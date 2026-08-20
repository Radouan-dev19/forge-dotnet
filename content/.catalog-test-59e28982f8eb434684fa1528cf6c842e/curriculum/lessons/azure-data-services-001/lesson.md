# Services de données et moindre privilège

## Objectif observable

À la fin de cette leçon, vous saurez choisir un service de stockage à partir de la forme des données et
des accès, accorder à une application le droit minimal dont elle a besoin, et énoncer ce qu'une
sauvegarde ne garantit pas tant qu'elle n'a pas été restaurée.

## Prérequis

- Avoir lu `azure-hosting-choice-001` et savoir partir des contraintes.
- Avoir lu `sql-relational-constraints-001` et savoir ce qu'une contrainte garantit.

## Intuition

Le choix d'un stockage se décide par deux questions : *quelle forme ont les données* et *comment sont-
elles interrogées*. Des entités reliées, interrogées par jointures et devant respecter des invariants
appellent une base relationnelle. Des documents lus par identifiant appellent autre chose. Des
fichiers volumineux appellent un stockage d'objets.

Le second principe est plus important que le premier : quel que soit le service, l'application reçoit
**le droit minimal** dont elle a besoin, sur les seules données qui la concernent.

## Explication

**La forme des données décide en premier.** Des entités avec des relations et des invariants
transversaux — un total qui doit correspondre à ses lignes, une référence qui doit exister — sont
mieux servies par une base relationnelle, où ces règles se déclarent et se font respecter par le
moteur.

Des documents autonomes, lus le plus souvent par leur identifiant, et dont la structure varie d'un
enregistrement à l'autre, supportent mal ce modèle. Des fichiers — images, exports, sauvegardes — n'ont
rien à faire dans une base : ils vont dans un stockage d'objets, et la base ne conserve que leur
référence.

**Le profil d'accès décide ensuite.** Beaucoup d'écritures ponctuelles, beaucoup de lectures par clé,
des agrégations lourdes sur l'historique : ce sont trois profils différents. Mélanger le traitement
opérationnel et l'analyse sur la même base fait payer chaque requête d'analyse par un ralentissement du
service.

**Le moindre privilège commence par le compte d'accès.** Une application qui ne fait que lire ne doit
pas pouvoir écrire. Une application qui écrit dans deux tables ne doit pas pouvoir en supprimer une
troisième. Un compte administrateur utilisé par l'application transforme la moindre injection en
compromission totale — c'est le prolongement direct de `security-owasp-api-001`.

En pratique : un compte par application, des droits attribués au niveau le plus fin disponible, et
aucun compte partagé entre plusieurs services.

**Le réseau est le second niveau.** Une base joignable depuis n'importe où dépend entièrement de son
mot de passe. Restreindre les origines autorisées, ou mieux, la rendre inaccessible en dehors du
réseau applicatif, ajoute un contrôle indépendant du secret. C'est la même logique que les réseaux
déclarés de `docker-compose-networks-volumes-001`.

**Chiffrement au repos et en transit.** Le chiffrement au repos est en général actif par défaut et
protège contre l'accès physique au support. Le chiffrement en transit protège contre l'écoute réseau,
et se vérifie : une chaîne de connexion qui désactive la validation du certificat annule ce contrôle,
souvent pour faire disparaître une erreur de configuration.

**Une sauvegarde non restaurée n'est pas une sauvegarde.** C'est la phrase à retenir. Tant qu'une
restauration n'a pas été effectuée sur un environnement séparé et vérifiée, on ignore si la sauvegarde
est complète, lisible et exploitable. La restauration se répète à intervalle régulier, et son délai
réel est mesuré — c'est lui qui figure dans la procédure de reprise, pas une estimation.

**Les données personnelles ajoutent des obligations.** Minimiser ce qui est collecté, limiter la durée
de conservation, savoir répondre à une demande de suppression, et ne jamais les copier en clair dans un
environnement de test. Une base de recette peuplée avec des données de production est une fuite en
attente — c'est aussi pourquoi `tests-integration-database-001` exige une base jetable.

## Exemple commenté

Le choix, ramené à la forme des données et au profil d'accès :

```text
Donnée                          Forme                 Accès dominant        Service
------------------------------  --------------------  --------------------  ------------------
Commandes et lignes             relations, invariants jointures, agrégats   base relationnelle
Préférences utilisateur         document autonome     lecture par clé       stockage clé-valeur
Factures au format portable     fichier binaire       lecture par référence stockage d'objets
Historique de facturation       relations, volumineux agrégats lourds       copie analytique
```

La dernière ligne est celle qu'on oublie : séparer l'analyse de l'opérationnel évite que le rapport
mensuel ralentisse la prise de commande.

Le moindre privilège, exprimé sur le compte applicatif :

```sql
-- Un compte par application, et rien de plus que ce dont elle a besoin.
CREATE USER app_commandes WITHOUT LOGIN;

-- Lecture et écriture sur les seules tables du domaine « commandes ».
GRANT SELECT, INSERT, UPDATE ON SCHEMA::commandes TO app_commandes;

-- Aucune suppression, aucun droit sur le schéma de facturation,
-- aucune permission de modifier la structure : une injection réussie
-- reste bornée à ce que ce compte peut faire.
DENY DELETE ON SCHEMA::commandes TO app_commandes;
DENY SELECT ON SCHEMA::facturation TO app_commandes;
```

Et la vérification de sauvegarde, telle qu'elle doit être conduite :

```text
Répétition de restauration — trimestrielle

1. Restaurer la sauvegarde de la veille sur une instance jetable, isolée du réseau applicatif.
2. Vérifier le nombre de lignes des cinq tables principales par rapport à la source.
3. Exécuter la suite d'intégration contre l'instance restaurée.
4. Mesurer le délai réel entre le début de la restauration et le premier service rendu.
5. Consigner ce délai : c'est lui qui figure dans la procédure de reprise, pas une estimation.
6. Détruire l'instance.

Sans les étapes 2 à 4, on a copié un fichier, pas vérifié une sauvegarde.
```

## Contre-exemple et erreur fréquente

```text
Chaîne de connexion utilisée par l'application :
  Server=srv-prod;Database=Facturation;User Id=sa;Password=...;Encrypt=false;

Configuration réseau :
  Origines autorisées : 0.0.0.0 - 255.255.255.255

Sauvegardes :
  Automatiques, quotidiennes, conservées 35 jours. Jamais restaurées.

Environnement de recette :
  Copie de la base de production, données clients comprises, restaurée chaque lundi.
```

Cinq défauts, dont deux critiques.

Le compte administrateur utilisé par l'application supprime toute limite : une injection réussie ne
donne pas accès à une table, elle donne accès à l'instance entière, y compris à la possibilité de
supprimer le schéma.

`Encrypt=false` désactive le chiffrement en transit. Le mot de passe et les données circulent en clair,
et cette option est presque toujours ajoutée pour faire taire une erreur de certificat.

L'ouverture réseau totale fait reposer toute la sécurité sur ce seul mot de passe, qui vient d'être
transmis en clair.

Les sauvegardes jamais restaurées ne garantissent rien. Le jour où elles serviront, on découvrira soit
qu'elles sont incomplètes, soit que la restauration prend beaucoup plus longtemps que la procédure ne
l'annonce.

Enfin, la recette peuplée avec des données clients réelles étend la surface d'exposition à un
environnement moins protégé, accessible à plus de monde, et souvent sans les mêmes contrôles.

## Vérification de compréhension

Une application n'a besoin que de lire trois tables et d'écrire dans une quatrième. Décrivez les droits
que vous accordez, ce que vous refusez explicitement, et ce que cela change en cas d'injection
réussie.

:::quiz
id=azure-data-services-001-check
question=Pourquoi une sauvegarde jamais restaurée ne constitue-t-elle pas une garantie ?
option=Parce que les sauvegardes non utilisées sont automatiquement purgées
option=Parce que rien n'établit qu'elle est complète, lisible et exploitable, ni combien de temps la restauration prend réellement
option=Parce qu'une sauvegarde doit être restaurée pour rester valide techniquement
correct=1
success=Correct : la répétition de restauration mesure aussi le délai réel, qui est ce qui figure dans la procédure de reprise.
retry=Relisez le passage sur les sauvegardes, et demandez-vous ce qu'on découvre le jour où l'on en a besoin.
:::

## Exercice guidé

Cette leçon se pratique sur le laboratoire `content/labs/azure-operations/` plutôt que sur un exercice
`/practice`.

1. Relevez, pour chaque donnée manipulée, sa forme et son profil d'accès dominant.
2. Écrivez le service de stockage que vous retenez pour chacune, avec le critère qui décide.
3. Rédigez les droits du compte applicatif : ce qui est accordé, ce qui est explicitement refusé.
4. Écrivez la procédure de répétition de restauration, avec les étapes de vérification.

## Exercice autonome

Concevez le stockage d'une application de facturation : clients, factures, lignes, documents portables
émis, et historique analytique.

Décidez avant d'écrire : le service retenu pour chaque donnée et son critère, les comptes d'accès et
leurs droits exacts, la configuration réseau, le chiffrement en transit, la politique de sauvegarde et
sa vérification, la durée de conservation des données personnelles, et la façon dont vous peuplez un
environnement de recette sans exposer de données réelles.

## Débogage

Un ticket indique : « Une injection sur un point d'entrée secondaire a permis de lire la table des
salaires. »

1. **Symptôme** : la portée de l'incident dépasse largement le point d'entrée touché.
2. **Hypothèse** : l'application utilise un compte disposant de droits sur l'ensemble de l'instance.
3. **Preuve** : relevez les droits effectifs du compte de connexion applicatif.
4. **Prévention** : un compte par application, des droits au niveau le plus fin, et des refus
   explicites sur les schémas étrangers.

## Entretien

Question posée à voix haute : *comment protégez-vous la base de données d'une application ?*

Une réponse solide cite le moindre privilège sur le compte applicatif, ajoute la restriction réseau
comme second contrôle indépendant du secret, mentionne le chiffrement en transit et sa désactivation
fréquente, et pose qu'une sauvegarde non restaurée ne garantit rien.

## Résumé

- La forme des données décide du service ; le profil d'accès affine.
- Un compte par application, avec le droit minimal et des refus explicites.
- La restriction réseau est un contrôle indépendant du mot de passe.
- Une désactivation de chiffrement en transit masque presque toujours autre chose.
- Une sauvegarde non restaurée est un fichier, pas une garantie.

## Cartes de révision

Question : pourquoi séparer l'analyse de l'opérationnel ? Réponse attendue : sinon chaque requête
d'agrégation ralentit le service qui rend la production.

Question : que risque une recette peuplée de données de production ? Réponse attendue : étendre
l'exposition à un environnement moins protégé et plus largement accessible.

## Test de maîtrise

Sans relire, décrivez l'architecture de données complète d'un service : le stockage retenu par type de
donnée avec son critère, les comptes et leurs droits exacts, la configuration réseau, le chiffrement,
la politique de sauvegarde avec sa procédure de vérification et le délai mesuré, le traitement des
données personnelles, et la façon de peupler un environnement de test.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
