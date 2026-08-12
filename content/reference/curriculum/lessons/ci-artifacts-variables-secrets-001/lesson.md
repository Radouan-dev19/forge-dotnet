# Artefacts identifiables, variables et secrets

## Objectif observable

À la fin de cette leçon, vous saurez nommer un artefact de façon à retrouver le commit qui l'a produit,
distinguer une variable d'un secret dans une chaîne de livraison, et empêcher qu'un secret apparaisse
dans un journal de construction.

## Prérequis

- Avoir lu `ci-pipeline-build-test-001` et savoir ce qu'une chaîne produit.
- Avoir lu `api-configuration-secrets-errors-001` et savoir classer une valeur.

## Intuition

Un artefact est ce que la chaîne produit et ce qui sera déployé. Deux exigences suffisent : il doit
être **identifiable** — on doit pouvoir remonter du fichier déployé au commit exact — et **construit
une seule fois**, puis promu d'environnement en environnement.

Reconstruire pour chaque environnement, c'est déployer trois choses différentes en croyant en déployer
une.

## Explication

**Construire une fois, promouvoir ensuite.** L'artefact validé en recette est **exactement** celui qui
part en production. Le reconstruire à chaque étape réintroduit tout le risque qu'on cherchait à
supprimer : une dépendance résolue différemment, un outillage à jour, une variable d'environnement
absente.

**Le nom porte l'identité.** Nom logique, branche normalisée, numéro d'exécution — et l'empreinte du
commit dans les métadonnées. Un nom qui contient une barre oblique ou un caractère de chemin casse le
stockage ; la normalisation de la branche n'est donc pas cosmétique, c'est une condition de
fonctionnement.

Ce nom doit permettre, six mois plus tard et devant un incident, de dire quel code s'exécute. Sans lui,
le diagnostic commence par une enquête.

**Variable ou secret : le même test qu'ailleurs.** Si la valeur devenait publique, faudrait-il la
changer ? Une adresse de service, un nom d'environnement, un indicateur : variable. Un jeton de
publication, un mot de passe, une clé de signature : secret. C'est le critère de
`api-configuration-secrets-errors-001`, appliqué à la chaîne.

**Un secret se déclare hors du dépôt, et se lit juste avant l'usage.** Il vit dans le magasin de la
chaîne, il est injecté dans l'étape qui en a besoin, et nulle part ailleurs. Le donner à toutes les
étapes multiplie les occasions de fuite sans rien simplifier.

**Le journal de construction est public en pratique.** Il est lu par toute l'équipe, souvent conservé
des mois, parfois accessible plus largement qu'on ne le croit. Trois règles : ne jamais afficher une
valeur secrète, ne jamais activer le mode verbeux sur une étape qui manipule un secret, et se méfier
des commandes qui affichent leur ligne d'appel complète.

Le masquage automatique proposé par les plateformes est une **aide**, pas une garantie : il ne masque
que ce qu'il connaît, et pas une valeur dérivée — un secret encodé, découpé ou intégré dans une URL
passe au travers.

**Un secret exposé est compromis, définitivement.** La procédure n'est pas « supprimer la ligne du
journal » mais « changer le secret ». C'est la même règle que pour un secret commis dans l'historique,
vue dans `git-commits-history-001`.

**Les branches externes n'ont pas accès aux secrets.** Une contribution venue de l'extérieur exécute du
code que personne n'a encore relu. Lui donner les secrets de publication revient à les publier. Les
chaînes déclenchées par une contribution externe s'exécutent donc sans secret, et la publication n'a
lieu qu'après fusion.

**L'artefact est immuable.** Republier un contenu différent sous le même nom rend impossible de savoir
ce qui tourne. C'est l'exigence d'immuabilité de `git-pull-requests-versions-001`, appliquée au
binaire.

## Exemple commenté

Le nom d'artefact, normalisé et vérifiable :

```csharp
public static string ArtifactName(string? branch, int buildNumber)
{
    // Un numéro non positif ne désigne aucune exécution : la faute est à l'appelant.
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buildNumber);

    if (string.IsNullOrWhiteSpace(branch))
    {
        throw new ArgumentException("La branche est requise.", nameof(branch));
    }

    // La barre oblique d'une branche « feature/x » casserait le stockage :
    // elle serait interprétée comme un séparateur de chemin.
    string normalized = branch.Trim().Replace('/', '-').ToLowerInvariant();

    return $"tests-{normalized}-{buildNumber}";
}
```

La séparation entre ce qui est versionné et ce qui ne l'est pas :

```text
# Variables : versionnées, lisibles, sans conséquence si elles fuitent.
env:
  DOTNET_VERSION: "10.0.100"
  BUILD_CONFIGURATION: Release
  ARTIFACT_RETENTION_DAYS: "30"

# Secrets : référencés, jamais écrits ici, et injectés dans la seule étape qui les utilise.
- name: Publier le paquet
  env:
    PUBLISH_TOKEN: ${{ secrets.PUBLISH_TOKEN }}
  run: dotnet nuget push ./out/*.nupkg --api-key "$PUBLISH_TOKEN" --source $FEED_URL
```

Et la construction unique, promue ensuite :

```text
# Un seul travail produit l'artefact, avec l'empreinte du commit en métadonnée.
Construction  -> forge-api-main-418  (commit e7f0edf)
                 |
                 +-- déployé en recette      : forge-api-main-418
                 +-- promu en production      : forge-api-main-418  (le même fichier)

# Ce qu'il ne faut pas : trois constructions, trois contenus, un seul numéro.
```

## Contre-exemple et erreur fréquente

```text
- name: Construire et déployer
  env:
    # Tous les secrets à toutes les étapes : chaque commande peut les exposer.
    DB_PASSWORD: ${{ secrets.DB_PASSWORD }}
    PUBLISH_TOKEN: ${{ secrets.PUBLISH_TOKEN }}
    SIGNING_KEY: ${{ secrets.SIGNING_KEY }}
  run: |
    set -x                                  # trace : chaque commande est affichée avec ses arguments
    echo "Connexion avec $DB_PASSWORD"      # le secret part au journal, en clair
    curl "https://api.interne/deploy?token=$PUBLISH_TOKEN"   # et dans l'URL, donc partout

- name: Publier
  # Artefact reconstruit ici : ce n'est pas celui qui a été testé.
  run: dotnet publish -o ./out && ./upload.sh ./out latest
```

Cinq défauts, dont trois fuites.

Les trois secrets sont exposés à toutes les commandes de l'étape, y compris à celles qui n'en ont pas
besoin. Le moindre outil verbeux les affichera.

`set -x` trace chaque commande avec ses arguments développés. Toute valeur passée en paramètre atterrit
dans le journal, y compris celles que le masquage automatique ne reconnaît pas.

L'affichage direct du mot de passe est la fuite la plus évidente, et la moins rare.

Le jeton en paramètre d'URL est la plus large : il se retrouve dans le journal, dans les journaux du
serveur distant et dans ceux de tout serveur mandataire. Un jeton passe par un en-tête, comme le
rappelle `security-authentication-001`.

Enfin, l'artefact est reconstruit à l'étape de publication : ce qui part n'est pas ce qui a été testé,
et le nom `latest` empêche de savoir ce qui tourne.

## Vérification de compréhension

Un secret est apparu en clair dans un journal de construction conservé trente jours. Décrivez les
gestes à faire, dans l'ordre, et dites lequel n'est pas facultatif.

:::quiz
id=ci-artifacts-variables-secrets-001-check
question=Pourquoi construire l'artefact une seule fois puis le promouvoir plutôt que de le reconstruire par environnement ?
option=Parce que la reconstruction consomme du temps de calcul facturé
option=Parce que reconstruire produit un contenu potentiellement différent : l'artefact validé en recette n'est alors pas celui qui part en production
option=Parce que les environnements de production refusent les artefacts reconstruits
correct=1
success=Correct : dépendance résolue autrement, outillage mis à jour, variable absente — chaque reconstruction réintroduit le risque que la validation devait supprimer.
retry=Relisez le passage sur la promotion, et demandez-vous ce qui a réellement été testé si l'on reconstruit à chaque étape.
:::

## Exercice guidé

Ouvrez `ci-artifact-name-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce qui rend une entrée invalide : branche absente, blancs seuls, numéro
   nul ou négatif.
2. Implémentez la normalisation en traitant le séparateur de chemin et la casse.
3. Vérifiez une branche contenant plusieurs séparateurs, et le numéro exactement à un.
4. Ouvrez ensuite `content/labs/ci-delivery/` et repérez comment l'artefact y est nommé.

## Exercice autonome

Concevez la gestion des artefacts et des secrets d'une chaîne livrant un service et une bibliothèque.

Décidez avant d'écrire : la convention de nom, les métadonnées attachées, la durée de conservation,
la liste des valeurs et leur classement en variable ou secret, l'étape qui reçoit chacune, ce que vous
faites des contributions externes, et le contrôle qui empêche une reconstruction avant publication.

## Débogage

Un ticket indique : « Le déploiement en production se comporte différemment de la recette, avec le
même numéro de version. »

1. **Symptôme** : deux environnements divergent à identifiant identique.
2. **Hypothèse** : l'artefact a été reconstruit entre les deux étapes.
3. **Preuve** : comparez les empreintes des fichiers déployés dans les deux environnements.
4. **Prévention** : construire une fois, promouvoir le même fichier, et rendre les artefacts
   immuables.

## Entretien

Question posée à voix haute : *comment gérez-vous les secrets dans votre chaîne de livraison ?*

Une réponse solide applique le critère variable/secret, limite l'injection à l'étape qui en a besoin,
traite le journal comme une surface de fuite, sait que le masquage automatique n'est qu'une aide, et
énonce qu'un secret exposé se change.

## Résumé

- Construire une fois, promouvoir ensuite : c'est le même fichier partout.
- Le nom d'artefact permet de remonter au commit sans enquête.
- Un secret est injecté dans la seule étape qui l'utilise.
- Le masquage automatique aide ; il ne couvre pas les valeurs dérivées.
- Une contribution externe s'exécute sans secret.

## Cartes de révision

Question : que fait un mode de trace verbeux à un secret passé en argument ? Réponse attendue : il
l'écrit dans le journal, souvent hors de portée du masquage automatique.

Question : quelle est la seule réponse valable à un secret exposé ? Réponse attendue : le changer —
supprimer la trace ne suffit jamais.

## Test de maîtrise

Sans relire, décrivez la politique complète d'artefacts et de secrets d'une chaîne : convention de
nom, métadonnées, promotion entre environnements, immuabilité, classement de six valeurs en variable
ou secret, portée d'injection de chacune, traitement des contributions externes, et procédure en cas
d'exposition.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
