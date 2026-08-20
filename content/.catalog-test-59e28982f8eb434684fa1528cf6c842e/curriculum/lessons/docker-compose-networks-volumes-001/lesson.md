# Composition, réseaux et volumes déclarés

## Objectif observable

À la fin de cette leçon, vous saurez décrire une pile de plusieurs services dans un fichier
reproductible, distinguer ce qui doit survivre à un conteneur de ce qui ne le doit pas, et calculer la
fenêtre d'un contrôle de santé pour qu'un démarrage lent ne soit pas pris pour une panne.

## Prérequis

- Avoir lu `docker-runtime-security-001` et savoir borner une exécution.
- Avoir lu `api-configuration-secrets-errors-001` et savoir d'où vient un secret.

## Intuition

Une application réelle n'est jamais seule : une interface, une base, parfois un cache. Décrire cet
ensemble dans un fichier versionné remplace une page de documentation que personne ne tient à jour par
quelque chose d'exécutable.

Le fichier répond à trois questions : *qui parle à qui*, *qu'est-ce qui doit survivre*, et *comment
sait-on que c'est prêt*.

## Explication

**Un réseau déclaré définit qui peut joindre qui.** Les services d'un même réseau se joignent par leur
nom ; ceux qui n'y sont pas ne se joignent pas du tout. Deux réseaux — un pour la façade, un pour les
données — permettent d'exposer l'interface sans que la base soit joignable depuis l'extérieur.

C'est le moindre privilège de `security-authorization-roles-policies-001`, appliqué à la topologie :
un service ne joint que ce dont il a besoin.

**Publier un port est une décision, pas un réglage par défaut.** Un service accessible sur le réseau
d'application n'a aucune raison de publier son port sur la machine hôte. Publier le port d'une base de
données la rend joignable depuis l'extérieur, souvent sans que personne ne l'ait voulu.

**Volume nommé et montage de répertoire ne servent pas au même usage.** Un *volume nommé* est géré par
le moteur : c'est ce qu'il faut pour des données qui doivent survivre au conteneur — le contenu d'une
base. Un *montage de répertoire* relie un chemin de la machine au conteneur : pratique en
développement pour voir ses modifications immédiatement, à éviter en exécution réelle, où il crée une
dépendance à la disposition des fichiers de l'hôte.

Et ce qui ne doit **pas** survivre ne reçoit aucun volume : un espace de travail temporaire, un cache
reconstructible. Leur donner de la persistance transforme un incident en état durable.

**Le contrôle de santé dit « prêt », pas « démarré ».** Un conteneur en cours d'exécution n'est pas
forcément capable de répondre : la base peut encore appliquer ses migrations. Le contrôle interroge un
point d'entrée qui vérifie réellement les dépendances essentielles, et il détermine quand les services
dépendants peuvent démarrer.

Trois paramètres le règlent : l'intervalle entre deux vérifications, le nombre d'essais avant de
déclarer l'échec, et le délai de grâce initial. La fenêtre totale — intervalle multiplié par essais —
doit dépasser le temps de démarrage réel, sinon un service lent est déclaré en panne alors qu'il
allait répondre. C'est ce que l'exercice de cette leçon fait calculer.

**L'ordre de démarrage ne suffit pas.** Déclarer qu'un service dépend d'un autre garantit l'ordre de
lancement, pas la disponibilité : le second démarre dès que le premier est *lancé*, pas dès qu'il est
*prêt*. La dépendance doit porter sur l'état de santé, et l'application doit de toute façon savoir
réessayer — une dépendance peut redémarrer en cours de vie.

**Les secrets ne sont pas dans le fichier.** Le fichier de composition est versionné : il ne contient
que des références à des variables, jamais leurs valeurs. Un fichier d'exemple versionné liste les
clés attendues avec des valeurs manifestement fausses, et le fichier réel reste hors du dépôt — c'est
exactement la règle de `api-configuration-secrets-errors-001`.

**Le durcissement s'applique ici aussi.** Identité non privilégiée, racine en lecture seule,
interdiction d'élévation et bornes de ressources se déclarent service par service. Un fichier de
composition qui les oublie annule le travail de `docker-runtime-security-001`.

## Exemple commenté

La fenêtre du contrôle de santé, calculée avant d'être écrite :

```csharp
public static bool FitsHealthBudget(int intervalSeconds, int retries, int budgetSeconds)
{
    // Des valeurs non strictement positives ne décrivent aucune fenêtre :
    // c'est une configuration fautive, pas un cas limite à absorber.
    if (intervalSeconds <= 0 || retries <= 0 || budgetSeconds <= 0)
    {
        return false;
    }

    // La fenêtre avant déclaration d'échec est le produit des deux. Elle doit
    // tenir dans le budget, sinon un démarrage lent est pris pour une panne.
    return intervalSeconds * retries <= budgetSeconds;
}
```

La pile décrite, avec ses deux réseaux et son volume nommé :

```text
services:
  api:
    image: forge-api@sha256:5f1c9a2be4770d
    # Seule la façade publie un port : la base reste injoignable de l'extérieur.
    ports: ["8080:8080"]
    networks: [frontal, donnees]
    environment:
      # Référence, jamais valeur : le fichier est versionné.
      ConnectionStrings__Orders: ${ORDERS_CONNECTION}
    depends_on:
      db:
        # Sur l'état de santé, pas sur le simple lancement.
        condition: service_healthy
    read_only: true
    user: "1654:1654"
    security_opt: ["no-new-privileges:true"]
    tmpfs: ["/tmp:size=64m"]
    deploy:
      resources:
        limits: { memory: 512M, cpus: "0.50" }

  db:
    image: postgres:17-alpine@sha256:9b1e6a4c07f2ad
    # Aucun port publié : seuls les services du réseau « donnees » y accèdent.
    networks: [donnees]
    environment:
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      # Volume nommé : ces données doivent survivre au conteneur.
      - donnees-db:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      retries: 6
      start_period: 10s

networks:
  frontal:
  donnees:
    # Réseau interne : aucun accès sortant depuis ce segment.
    internal: true

volumes:
  donnees-db:
```

L'intervalle de cinq secondes et six essais donnent une fenêtre de trente secondes, à laquelle
s'ajoute le délai de grâce initial : c'est ce qui sépare un démarrage lent d'une panne réelle.

## Contre-exemple et erreur fréquente

```text
services:
  api:
    build: .
    ports: ["8080:8080"]
    environment:
      # Le secret est écrit en clair dans un fichier versionné :
      # il est compromis dès le premier commit, et le reste dans l'historique.
      ConnectionStrings__Orders: "Server=db;User Id=sa;Password=Motdepasse123!"
    depends_on: [db]
    volumes:
      # Montage du code source : pratique en développement,
      # dépendance à la disposition des fichiers de l'hôte en exécution réelle.
      - .:/app

  db:
    image: postgres:latest
    # Port de base publié : la base est joignable depuis l'extérieur de la machine.
    ports: ["5432:5432"]
    environment:
      POSTGRES_PASSWORD: "Motdepasse123!"
    # Aucun volume : les données disparaissent à chaque recréation du conteneur.
```

Six défauts.

Le mot de passe en clair dans un fichier versionné est compromis dès le commit, et il reste dans
l'historique après suppression — c'est ce qu'explique `git-commits-history-001`.

`depends_on` sans condition de santé garantit seulement l'ordre de lancement. L'application démarrera
avant que la base soit prête, échouera à se connecter, et le symptôme sera intermittent selon la
charge de la machine.

Le port de base publié la rend joignable depuis l'extérieur. Aucun service applicatif n'en avait
besoin : c'est une surface d'attaque ajoutée par défaut.

L'absence de volume sur la base fait disparaître les données à chaque recréation du conteneur. Le
défaut ne se voit pas en développement, où l'on recrée volontiers, et se voit très bien la première
fois qu'on redéploie.

`latest` ne désigne aucun contenu stable, et aucun réglage de durcissement ni aucune borne de
ressources n'est déclaré : tout le travail de `docker-runtime-security-001` est annulé.

## Vérification de compréhension

Un service applicatif démarre avant que sa base soit prête et échoue une fois sur trois. Dites ce que
vous ajoutez au fichier, et pourquoi cela ne dispense pas l'application de savoir réessayer.

:::quiz
id=docker-compose-networks-volumes-001-check
question=Quelle différence pratique entre un volume nommé et un montage de répertoire de la machine ?
option=Le volume nommé est chiffré, le montage de répertoire ne l'est pas
option=Le volume nommé est géré par le moteur et convient aux données qui doivent survivre au conteneur ; le montage lie un chemin de l'hôte et crée une dépendance à sa disposition
option=Le volume nommé n'est accessible que depuis un seul service à la fois
correct=1
success=Correct : le montage de répertoire est commode en développement, et devient une dépendance fragile en exécution réelle.
retry=Relisez le passage sur les volumes, et demandez-vous ce qui se passe si la machine cible n'a pas la même arborescence.
:::

## Exercice guidé

Ouvrez `docker-health-window-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que doit produire chaque valeur non strictement positive.
2. Implémentez la validation des trois valeurs avant tout calcul.
3. Vérifiez la frontière exacte : une fenêtre égale au budget, puis supérieure d'une seconde.
4. Ouvrez ensuite `content/labs/container-delivery/compose.yaml` et vérifiez que sa fenêtre de santé
   dépasse bien le temps de démarrage observé.

## Exercice autonome

Décrivez la pile complète d'une application à trois services : interface, service applicatif, base.

Décidez avant d'écrire : les réseaux et qui appartient à chacun, les ports réellement publiés et leur
justification, ce qui reçoit un volume nommé et ce qui n'en reçoit aucun, les paramètres du contrôle
de santé avec le calcul de la fenêtre, les réglages de durcissement par service, et la façon dont les
secrets parviennent aux conteneurs.

## Débogage

Un ticket indique : « Après un redémarrage de la machine, toutes les données de test ont disparu. »

1. **Symptôme** : la persistance n'a pas survécu à la recréation des conteneurs.
2. **Hypothèse** : le répertoire de données du service n'est associé à aucun volume nommé.
3. **Preuve** : relevez les volumes déclarés et comparez-les au chemin réellement utilisé par le
   service pour stocker ses données.
4. **Prévention** : déclarer un volume nommé pour ce qui doit survivre, et vérifier après un cycle
   complet d'arrêt et de recréation.

## Entretien

Question posée à voix haute : *comment décrivez-vous un environnement applicatif à plusieurs
services ?*

Une réponse solide part du fichier versionné comme documentation exécutable, sépare les réseaux selon
qui doit joindre qui, distingue volume nommé et montage de répertoire, explique pourquoi la dépendance
doit porter sur la santé, et sait dire que le fichier ne contient jamais de secret.

## Résumé

- Le réseau déclare qui peut joindre qui ; publier un port est une décision.
- Volume nommé pour ce qui survit, aucun volume pour ce qui ne doit pas survivre.
- Le contrôle de santé dit « prêt », pas « démarré ».
- La fenêtre de santé doit dépasser le temps de démarrage réel.
- L'ordre de lancement ne remplace ni la condition de santé, ni la reprise applicative.

## Cartes de révision

Question : que garantit exactement une dépendance sans condition de santé ? Réponse attendue : l'ordre
de lancement, jamais la disponibilité.

Question : que contient le fichier de composition à la place d'un secret ? Réponse attendue : une
référence à une variable, dont la valeur vit hors du dépôt.

## Test de maîtrise

Sans relire, écrivez la composition complète d'une pile applicative : services, réseaux et
appartenances, ports publiés avec justification, volumes et leur nécessité, contrôles de santé avec le
calcul de leur fenêtre, réglages de durcissement, transport des secrets, et le contrôle qui prouve que
la persistance survit à une recréation.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
