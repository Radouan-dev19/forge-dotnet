# Durcissement d'exécution d'un conteneur

## Objectif observable

À la fin de cette leçon, vous saurez exécuter un conteneur sans privilège superflu, borner ce qu'il
peut consommer, et énoncer les trois réglages qui doivent tenir **simultanément** pour qu'une
exécution soit considérée comme durcie.

## Prérequis

- Avoir lu `docker-images-layers-001` et savoir ce que contient une image.
- Avoir lu `security-owasp-api-001` et savoir qu'un contrôle sans borne n'en est pas un.

## Intuition

Un conteneur isole des processus, il ne les enferme pas. Par défaut, le processus s'exécute avec des
privilèges élevés, peut écrire partout dans son système de fichiers, consommer toute la mémoire de la
machine, et acquérir de nouveaux droits en cours d'exécution.

Le durcissement consiste à retirer chacune de ces possibilités, et à le faire **ensemble** : deux
réglages sur trois laissent le chemin ouvert.

## Explication

**Ne pas s'exécuter avec des privilèges élevés.** Le processus tourne sous une identité dédiée, sans
droit particulier. La conséquence est immédiate : une exécution de code arbitraire dans le conteneur
n'obtient pas les droits qui permettraient d'en sortir ou d'altérer le système sous-jacent.

L'identité se déclare dans l'image, et se vérifie à l'exécution — c'est ce que fait le bac à sable de
ce dépôt, dont l'image finale bascule sur une identité numérique dédiée avant le point d'entrée.

**Système de fichiers en lecture seule.** Un processus qui n'a pas besoin d'écrire ne doit pas pouvoir
écrire. Ce qui doit l'être — un répertoire temporaire, un espace de travail — est monté explicitement,
avec une taille bornée. Une écriture inattendue devient alors une erreur immédiate plutôt qu'une
modification durable.

**Interdire l'acquisition de nouveaux privilèges.** C'est le réglage le moins connu et le plus
important des trois. Sans lui, un exécutable portant un bit d'élévation peut faire remonter les droits
du processus **pendant** l'exécution, y compris s'il a démarré sans privilège. Le premier réglage
devient alors contournable.

Les trois doivent tenir ensemble : identité non privilégiée, racine en lecture seule, et interdiction
d'élévation. C'est exactement ce que l'exercice de cette leçon fait exprimer sous forme de règle.

**Borner les ressources.** Mémoire, temps processeur, nombre de processus, taille de sortie, durée. Un
conteneur sans borne peut consommer toute la machine et faire tomber ce qui tourne à côté. Ces bornes
ne sont pas des optimisations : ce sont des contrôles de disponibilité, au même titre que les bornes
d'entrée de `api-pagination-filtering-sorting-001`.

Une borne de mémoire raisonnable a un défaut sûr et un intervalle admissible : trop basse, le
processus est arrêté sans raison légitime ; trop haute, elle ne protège plus rien.

**Couper le réseau quand il n'est pas nécessaire.** Un conteneur qui ne doit joindre personne n'a pas
besoin d'interface réseau. C'est le contrôle le plus efficace contre l'exfiltration : sans réseau, du
code hostile exécuté à l'intérieur ne peut rien envoyer.

**Le conteneur est éphémère et son nettoyage est garanti.** Il est créé pour une exécution, détruit
après, y compris en cas d'échec. Sans garantie de nettoyage, les conteneurs et volumes orphelins
s'accumulent jusqu'à saturation du disque — c'est la même exigence que la base de test jetable de
`tests-integration-database-001`.

**Aucun secret dans les variables d'un conteneur inspectable.** L'inspection d'un conteneur en cours
d'exécution révèle son environnement. Un secret y est lisible par quiconque a accès au moteur. Les
secrets passent par un montage dédié dont le contenu ne persiste pas.

## Exemple commenté

La règle de durcissement, exprimée comme une conjonction :

```csharp
public static bool IsHardened(bool runsAsNonRoot, bool readOnlyRootFilesystem, bool noNewPrivileges)
{
    // Les trois conditions ensemble, sans exception. Deux sur trois laissent
    // un chemin ouvert : sans l'interdiction d'élévation, l'identité non
    // privilégiée peut être contournée pendant l'exécution.
    return runsAsNonRoot && readOnlyRootFilesystem && noNewPrivileges;
}
```

La borne de mémoire, avec défaut sûr et intervalle admissible :

```csharp
public static int ClampMemoryMb(int? requestedMb)
{
    const int defaultMb = 256;
    const int minimumMb = 128;
    const int maximumMb = 1_024;

    // Absence de demande : un défaut prudent plutôt qu'aucune limite.
    if (requestedMb is null)
    {
        return defaultMb;
    }

    // Une demande explicite est bornée des deux côtés : trop basse, le processus
    // est arrêté sans raison légitime ; trop haute, la borne ne protège plus rien.
    return Math.Clamp(requestedMb.Value, minimumMb, maximumMb);
}
```

Et l'exécution durcie, telle qu'elle est lancée :

```text
docker run --rm \
  --user 1654:1654 \
  --read-only \
  --security-opt no-new-privileges \
  --network none \
  --memory 256m --memory-swap 256m \
  --cpus 0.5 \
  --pids-limit 64 \
  --tmpfs /workspace:rw,size=64m,mode=1777 \
  forge-runner@sha256:d8ee39817ca03a
```

Chaque option ferme une menace nommée : identité dédiée contre l'élévation, racine en lecture seule
contre la persistance, interdiction d'élévation contre le contournement de la première, absence de
réseau contre l'exfiltration, bornes de mémoire et de processus contre l'épuisement de la machine,
espace de travail temporaire et borné pour ce qui doit malgré tout s'écrire, suppression automatique
pour le nettoyage.

## Contre-exemple et erreur fréquente

```text
docker run -d \
  --privileged \
  -v /:/host \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e DB_PASSWORD=motdepasse-reel \
  --network host \
  mon-image:latest
```

Cinq décisions, chacune suffisant à annuler l'isolation.

`--privileged` retire l'essentiel des restrictions. Le processus obtient des capacités proches de
celles de la machine hôte : le conteneur n'isole plus rien.

Le montage de la racine de l'hôte donne accès en écriture à l'ensemble du système de fichiers. Une
faille dans l'application devient une compromission de la machine.

Le montage de la prise du moteur est équivalent à un accès administrateur : qui peut parler au moteur
peut démarrer un conteneur privilégié montant la racine. C'est le raccourci le plus fréquent et le
plus grave.

Le mot de passe passé en variable est lisible par inspection du conteneur, et se retrouve dans les
journaux du moteur.

`--network host` supprime l'isolation réseau : le conteneur voit et peut joindre tout ce que voit
l'hôte, y compris les services censés n'être accessibles qu'en local.

Enfin, `latest` ne désigne aucun contenu stable, et aucune borne de mémoire ni de processus n'est
posée.

## Vérification de compréhension

Un conteneur s'exécute sous une identité non privilégiée et avec une racine en lecture seule, mais
sans interdiction d'élévation. Dites en quoi cette configuration reste vulnérable.

:::quiz
id=docker-runtime-security-001-check
question=Pourquoi monter la prise du moteur de conteneurs dans un conteneur est-il particulièrement grave ?
option=Parce que ce montage ralentit fortement les entrées et sorties du conteneur
option=Parce que qui peut parler au moteur peut démarrer un conteneur privilégié montant la racine de l'hôte : c'est équivalent à un accès administrateur sur la machine
option=Parce que la prise n'est pas compatible avec un système de fichiers en lecture seule
correct=1
success=Correct : toutes les autres restrictions deviennent décoratives dès lors que le conteneur peut demander au moteur d'en créer un autre sans restriction.
retry=Relisez le contre-exemple, et demandez-vous ce qu'un processus peut faire s'il peut piloter le moteur de conteneurs.
:::

## Exercice guidé

Ouvrez `docker-hardening-policy-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, pourquoi les trois conditions doivent tenir ensemble et ce que laisse
   ouvert chaque combinaison à deux.
2. Implémentez la règle comme une conjonction stricte, sans exception.
3. Vérifiez les trois cas où une seule condition manque.
4. Enchaînez avec `docker-memory-limit-001`, qui borne la consommation.

## Exercice autonome

Écrivez la commande d'exécution durcie d'un service qui lit une base et n'expose qu'un point d'entrée
HTTP.

Décidez avant d'écrire : l'identité d'exécution, ce qui doit rester inscriptible et sous quelle forme,
les bornes de ressources et leur justification, la politique réseau, la façon dont les secrets
parviennent au processus, et la garantie de nettoyage. Pour chaque option, nommez la menace fermée.

## Débogage

Un ticket indique : « Un conteneur applicatif a réussi à modifier un fichier de la machine hôte. »

1. **Symptôme** : une écriture est sortie de l'isolation.
2. **Hypothèse** : le conteneur est privilégié, ou un répertoire de l'hôte est monté en écriture, ou la
   racine est inscriptible.
3. **Preuve** : inspectez les options d'exécution du conteneur : privilèges, montages, identité,
   interdiction d'élévation.
4. **Prévention** : les trois réglages de durcissement ensemble, aucun montage de l'hôte en écriture,
   et un contrôle automatique qui refuse une exécution non conforme.

## Entretien

Question posée à voix haute : *comment exécutez-vous du code non fiable dans un conteneur ?*

Une réponse solide nomme les trois réglages et explique pourquoi ils sont indissociables, ajoute les
bornes de ressources et l'absence de réseau, mentionne le nettoyage garanti, et sait dire que
l'isolation d'un conteneur n'est pas celle d'une machine virtuelle.

## Résumé

- Identité non privilégiée, racine en lecture seule, interdiction d'élévation : les trois ensemble.
- Les bornes de ressources sont des contrôles de disponibilité, pas des optimisations.
- Sans réseau, du code hostile ne peut rien exfiltrer.
- Le conteneur est éphémère et son nettoyage est garanti, même en cas d'échec.
- Monter la prise du moteur revient à donner un accès administrateur.

## Cartes de révision

Question : que permet l'absence d'interdiction d'élévation ? Réponse attendue : un exécutable portant
un bit d'élévation peut faire remonter les droits pendant l'exécution.

Question : pourquoi un secret ne passe-t-il pas par une variable d'environnement de conteneur ? Réponse
attendue : l'inspection du conteneur le révèle, et il se retrouve dans les journaux du moteur.

## Test de maîtrise

Sans relire, écrivez la configuration d'exécution durcie d'un service exécutant du code non fiable :
identité, système de fichiers, élévation, réseau, bornes de mémoire, de processeur, de processus et de
durée, transport des secrets, garantie de nettoyage. Pour chaque réglage, nommez la menace qu'il ferme
et ce qui reste ouvert s'il manque.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
