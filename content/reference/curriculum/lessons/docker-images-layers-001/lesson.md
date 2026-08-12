# Images, couches et cache de construction

## Objectif observable

À la fin de cette leçon, vous saurez ordonner les instructions d'une image pour que le cache serve
réellement, séparer la construction de l'exécution, et expliquer pourquoi une image épinglée par
empreinte est la seule qui soit reproductible.

## Prérequis

- Avoir lu `git-pull-requests-versions-001` et savoir ce qu'une version immuable garantit.
- Savoir construire et lancer un conteneur.

## Intuition

Une image est une **pile de couches**. Chaque instruction en produit une, et chaque couche est mise en
cache selon ce qui l'a produite. Modifier une instruction invalide sa couche et **toutes celles qui la
suivent**.

Toute l'optimisation d'une construction tient dans cette phrase : ce qui change rarement doit venir
avant ce qui change souvent. Copier le code source avant de restaurer les dépendances invalide la
restauration à chaque modification d'une ligne de code — et fait passer une construction de dix
secondes à trois minutes.

## Explication

**L'ordre décide du cache.** Le fichier de projet change rarement ; le code source change à chaque
commit. Copier d'abord le fichier de projet, restaurer les dépendances, puis copier le code : la
restauration reste en cache tant que les dépendances déclarées n'ont pas bougé.

**La construction en plusieurs étapes réduit ce qui est publié.** Une première étape contient le kit
de développement, compile et publie. Une seconde part d'une image d'exécution et ne reçoit que le
résultat. Le compilateur, les sources et les fichiers intermédiaires ne sortent jamais de la première
étape.

Le gain n'est pas seulement la taille. Une image d'exécution contient moins d'outils, donc moins de
surface exploitable : c'est aussi une décision de sécurité, prolongée dans
`docker-runtime-security-001`.

**L'étiquette n'identifie rien.** Une étiquette peut être déplacée : l'image derrière `10.0-alpine`
n'est pas la même d'un mois à l'autre. Une empreinte de contenu, elle, désigne exactement un contenu
et ne peut pas changer. C'est le même raisonnement que l'immuabilité des versions de
`git-pull-requests-versions-001`.

C'est ce que fait ce dépôt : l'image du bac à sable d'exécution est épinglée par empreinte, de sorte
qu'une construction refaite dans six mois produise le même environnement.

**Ce qui n'entre pas dans une image.** Aucun secret, jamais. Une valeur passée à la construction reste
dans l'historique des couches : quiconque récupère l'image peut la relire, même si une instruction
ultérieure l'a « supprimée ». Les secrets arrivent à l'exécution, par l'environnement ou un montage —
c'est la règle de `api-configuration-secrets-errors-001`.

N'entrent pas non plus : les fichiers de développement, les dossiers de sortie locaux, l'historique du
dépôt. Un fichier d'exclusion les écarte, ce qui réduit la taille du contexte envoyé et accélère
chaque construction.

**Une image reste petite par ce qu'on n'y met pas.** Choisir une image de base minimale, ne pas
installer d'outils de diagnostic « au cas où », nettoyer les caches de gestionnaire de paquets dans la
même instruction que l'installation — une suppression dans une instruction ultérieure ne réduit rien,
puisque la couche précédente existe toujours.

**Une image est étiquetée pour être identifiable.** Titre, description, source, version : ces
métadonnées permettent de relier une image en cours d'exécution au commit qui l'a produite. Sans
elles, diagnostiquer un incident commence par une enquête sur ce qui tourne réellement.

## Exemple commenté

L'ordre qui préserve le cache, et la séparation des étapes :

```text
# Étape de construction : contient le kit de développement, jamais publiée.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:d8ee39817ca03a AS build
WORKDIR /src

# Le fichier de projet d'abord : il change rarement, donc la restauration
# qui suit reste en cache tant que les dépendances déclarées ne bougent pas.
COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj

# Le code source ensuite : il change à chaque commit, et n'invalide
# que les couches situées après lui.
COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /out /p:UseAppHost=false
```

```text
# Étape finale : image d'exécution, sans compilateur ni sources.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:5f1c9a2be4770d
LABEL org.opencontainers.image.title="Forge API" \
      org.opencontainers.image.source="https://local.invalid/forge-dotnet" \
      org.opencontainers.image.version="1.4.8"

# Seul le résultat de la publication traverse : ni sources, ni fichiers intermédiaires.
COPY --from=build /out/ /opt/api/
ENTRYPOINT ["dotnet", "/opt/api/Api.dll"]
```

L'exclusion du contexte, qui réduit ce qui est envoyé au moteur :

```text
# .dockerignore — chaque ligne évite d'envoyer des fichiers qui n'entrent pas
# dans l'image, et évite surtout qu'un secret local y arrive par accident.
.git
bin/
obj/
**/appsettings.Development.json
**/*.user
.env
```

Et le contrôle qui refuse une image de base non épinglée, exécutable dans la chaîne de construction :

```csharp
public static bool IsPinnedByDigest(string? imageReference)
{
    if (string.IsNullOrWhiteSpace(imageReference))
    {
        return false;
    }

    // Une empreinte désigne un contenu et un seul. Une étiquette seule — y compris
    // une étiquette de version — peut être redirigée vers un autre contenu.
    int separator = imageReference.IndexOf("@sha256:", StringComparison.Ordinal);

    // Le nom doit précéder l'empreinte, et l'empreinte doit être complète :
    // soixante-quatre caractères hexadécimaux.
    return separator > 0 && imageReference.Length - separator - 8 == 64;
}
```

## Contre-exemple et erreur fréquente

```text
FROM mcr.microsoft.com/dotnet/sdk:latest

WORKDIR /app

# Tout le contexte d'abord : la moindre modification, même d'un fichier
# de documentation, invalide toutes les couches suivantes.
COPY . .

# Un secret passé à la construction : il reste lisible dans l'historique
# des couches, y compris après la « suppression » plus bas.
ARG NUGET_TOKEN
RUN dotnet restore --source https://flux.interne/index.json

RUN apt-get update && apt-get install -y curl vim net-tools
RUN rm -rf /var/lib/apt/lists/*

RUN dotnet publish -c Release -o /out

# Aucune seconde étape : le kit de développement, les sources et les
# fichiers intermédiaires sont publiés avec l'application.
ENTRYPOINT ["dotnet", "/out/Api.dll"]
```

Cinq défauts.

`latest` ne désigne aucun contenu stable. Deux constructions à deux semaines d'écart peuvent produire
deux environnements différents, et l'écart n'apparaîtra qu'à l'exécution.

`COPY . .` avant toute restauration détruit le cache : chaque modification, même d'un fichier de
documentation, relance la restauration complète des dépendances.

Le jeton passé en argument de construction est inscrit dans les métadonnées de la couche. Toute
personne qui récupère l'image peut le relire : le secret est compromis dès la publication.

L'installation d'outils de diagnostic ajoute de la surface exploitable sans nécessité. Et leur
suppression dans une instruction *suivante* ne réduit pas la taille : la couche qui les contient
existe toujours. Nettoyer doit se faire dans la même instruction que l'installation.

Enfin, l'absence de seconde étape publie le compilateur et les sources avec l'application.

## Vérification de compréhension

Vous modifiez une ligne dans un fichier de code source. Dites quelles couches sont réutilisées et
lesquelles sont reconstruites, pour l'ordre correct puis pour l'ordre du contre-exemple.

:::quiz
id=docker-images-layers-001-check
question=Pourquoi épingler une image de base par son empreinte de contenu plutôt que par une étiquette ?
option=Parce que l'empreinte se télécharge plus rapidement que l'étiquette
option=Parce qu'une étiquette peut être déplacée vers un autre contenu : seule l'empreinte garantit que deux constructions partent du même environnement
option=Parce que les étiquettes ne sont pas acceptées dans une construction en plusieurs étapes
correct=1
success=Correct : c'est la même exigence d'immuabilité que pour une version publiée — un identifiant qui désigne un contenu, et un seul.
retry=Relisez le passage sur l'étiquette, et demandez-vous ce que désigne la même étiquette à deux mois d'intervalle.
:::

## Exercice guidé

Cette leçon se pratique sur un artefact réel du dépôt plutôt que sur un exercice `/practice`.

1. Ouvrez `src/ForgeDotNet.CodeRunner/Container/Dockerfile` et repérez les deux étapes ainsi que
   l'épinglage par empreinte.
2. Écrivez, pour chaque instruction, ce qui invalide sa couche. Repérez celle dont l'invalidation coûte
   le plus cher.
3. Comparez avec `content/labs/container-delivery/Dockerfile` et relevez les différences d'ordre.
4. Proposez une modification qui améliorerait le cache, et dites ce qu'elle changerait au temps de
   construction.

## Exercice autonome

Écrivez l'image d'une application de votre choix.

Décidez avant d'écrire : l'image de base et la façon dont vous l'épinglez, le découpage en étapes,
l'ordre des instructions et sa justification par le cache, le contenu du fichier d'exclusion, les
métadonnées ajoutées, et la façon dont les secrets arrivent à l'exécution sans jamais entrer dans
l'image.

## Débogage

Un ticket indique : « La construction de l'image prend six minutes alors qu'on ne change qu'une ligne
de code. »

1. **Symptôme** : le temps de construction est indépendant de la taille du changement.
2. **Hypothèse** : une instruction coûteuse est placée après une copie qui change à chaque commit.
3. **Preuve** : relevez les instructions réellement réexécutées lors d'une construction. Une
   restauration de dépendances rejouée à chaque fois confirme.
4. **Prévention** : copier le fichier de projet et restaurer avant de copier les sources, et vérifier
   ce qu'un fichier d'exclusion écarte du contexte.

## Entretien

Question posée à voix haute : *comment réduisez-vous la taille et le temps de construction d'une image
applicative ?*

Une réponse solide explique le mécanisme des couches et l'invalidation en cascade, décrit la
construction en plusieurs étapes, mentionne l'épinglage par empreinte pour la reproductibilité, et
sait dire qu'un secret passé à la construction reste dans l'historique.

## Résumé

- Chaque instruction produit une couche ; en modifier une invalide toutes les suivantes.
- Ce qui change rarement vient avant ce qui change souvent.
- Une seconde étape ne publie que le résultat, pas le compilateur ni les sources.
- Seule une empreinte de contenu rend une construction reproductible.
- Un secret passé à la construction reste lisible dans l'historique des couches.

## Cartes de révision

Question : pourquoi supprimer un fichier dans une instruction ultérieure ne réduit-il pas la taille ?
Réponse attendue : la couche qui le contient existe toujours dans la pile.

Question : à quoi servent les métadonnées d'une image ? Réponse attendue : relier une image en cours
d'exécution au commit qui l'a produite, sans enquête.

## Test de maîtrise

Sans relire, écrivez l'image complète d'une application web : les étapes, l'épinglage des bases,
l'ordre des instructions avec la justification de chacune par le cache, le fichier d'exclusion, les
métadonnées, la stratégie de secrets, et ce que vous vérifiez avant de publier l'image.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
