# Piloter par jalons et par preuves

## Objectif observable

À la fin de cette leçon, vous saurez découper un projet en jalons dont l'achèvement se constate au lieu
de se déclarer, produire pour chacun une preuve vérifiable par quelqu'un d'autre, et refuser de
qualifier « terminé » ce qui ne l'est pas.

## Prérequis

- Avoir lu `final-project-architecture-001` et savoir cadrer par le parcours critique.
- Avoir lu `ci-deployment-gates-001` et savoir ce qu'une condition vérifiable exige.

## Intuition

« C'est presque fini » est la phrase la plus coûteuse d'un projet. Elle n'est ni vraie ni fausse :
elle n'est simplement pas vérifiable, et c'est ce qui la rend dangereuse.

Un jalon utile s'énonce par ce qu'on peut **constater** : un parcours qui s'exécute, une suite qui
passe, une réponse qu'on peut obtenir. La preuve n'est pas un supplément administratif — c'est ce qui
distingue un état connu d'une impression.

## Explication

**Un jalon est vertical, jamais horizontal.** « Le domaine est terminé » n'est pas un jalon : rien ne
s'exécute, rien ne se démontre, et l'intégration avec le reste reste entièrement devant vous. « Créer
une commande fonctionne de bout en bout, du point d'entrée à la base » en est un : il traverse toutes
les couches et prouve qu'elles se rejoignent.

Le découpage vertical déplace les mauvaises surprises au plus tôt, ce qui est le seul moment où elles
coûtent peu.

**Chaque jalon porte ses critères d'acceptation.** Trois au minimum, écrits **avant** de commencer, et
formulés de sorte qu'un tiers puisse les vérifier sans vous. « L'autorisation fonctionne » ne convient
pas ; « un appel à la ressource d'un autre utilisateur retourne un refus, et un test le prouve »
convient.

**La preuve est un artefact, pas une affirmation.** Un rapport de tests, une capture de la réponse HTTP
avec son statut, un journal montrant l'identifiant de corrélation, un lien vers le commit. Ce sont ces
objets qu'on relit trois mois plus tard, et ce sont eux qu'on présente en entretien — c'est le sujet de
`career-evidence-plan-001`.

**Trois conditions font qu'un jalon est révisable.** Les tests passent. Une revue de sécurité a eu
lieu sur ce qui a été ajouté. Un retour arrière est documenté. Les trois ensemble : des tests verts
sans revue de sécurité laissent passer exactement les défauts que `security-owasp-api-001` décrit, et
un jalon sans retour arrière documenté ne peut pas être déployé sereinement.

C'est cette conjonction que l'exercice de cette leçon fait écrire.

**Estimer par tranches, réviser à chaque jalon.** Une estimation à quatre semaines est fausse ; une
estimation à trois jours l'est beaucoup moins. Découper en jalons de quelques jours donne un rythme de
correction : à chaque achèvement, l'écart entre prévu et réel informe la suite.

**La réserve n'est pas de la lâcheté.** Un projet comporte des inconnues : une dépendance qui ne se
comporte pas comme annoncé, un environnement qui manque, une exigence mal comprise. Prévoir une marge
explicite est honnête ; ne pas en prévoir revient à promettre que rien d'imprévu n'arrivera.

**Ce qui est terminé ne se rouvre pas sans décision.** Un jalon accepté est figé. Y revenir est
possible, mais c'est une décision consignée avec son coût, pas une dérive silencieuse. Sans cette
règle, le projet avance et recule en même temps.

**La dette prise se déclare.** Un raccourci assumé — une validation partielle, un cas non traité — se
note avec sa raison et sa condition de résolution. C'est exactement ce que fait le registre de dette de
ce dépôt : ce qui est déclaré peut être suivi et ne peut pas empirer, ce qui est tu devient invisible.

## Exemple commenté

La condition de révision d'un jalon, comme conjonction :

```csharp
public static bool MilestoneReady(bool testsPass, bool securityReviewed, bool rollbackDocumented)
{
    // Les trois ensemble. Des tests verts sans revue de sécurité laissent passer
    // les défauts qu'aucun test fonctionnel ne cherche ; un jalon sans retour
    // arrière documenté ne peut pas être déployé sans pari.
    return testsPass && securityReviewed && rollbackDocumented;
}
```

Un découpage vertical, avec ses critères et ses preuves :

```text
Jalon 1 — Créer une commande de bout en bout                          (3 jours)

Critères d'acceptation
  1. POST /orders avec un corps valide retourne 201 et un en-tête de localisation.
  2. Un corps invalide retourne 400 au format d'erreur normalisé, champ fautif nommé.
  3. La commande est relue depuis un contexte neuf avec un identifiant strictement positif.

Preuves
  - rapport de la suite unitaire et de la suite HTTP (12 tests, 0 échec)
  - capture des deux réponses, statut et corps
  - commit 4f2a1c8, revue de sécurité consignée sur l'autorisation du point d'entrée
  - retour arrière : redéploiement de la version précédente, migration additive seule

Réel : 4 jours. Écart : la validation du corps a demandé un demi-jour de plus que prévu.
Report sur le jalon 2 : ramener sa marge de 1 jour à 0,5.
```

Et la déclaration d'une dette assumée :

```text
Dette 002 — pagination absente sur GET /orders

Raison       le jalon 2 vise le parcours de consultation ; la pagination n'était pas
             nécessaire pour le démontrer sur un jeu de 20 commandes.
Risque       une liste non bornée sature la mémoire dès quelques milliers de lignes.
Résolution   jalon 4, avant toute exposition à des données réelles.
Contrôle     un test qui échoue si la réponse dépasse 100 éléments, à ajouter au jalon 4.
```

## Contre-exemple et erreur fréquente

```text
Plan de projet — 4 semaines

Semaine 1 : le domaine
Semaine 2 : l'infrastructure
Semaine 3 : l'API
Semaine 4 : les tests et la documentation

Suivi hebdomadaire
  S1 : « domaine terminé »
  S2 : « infrastructure terminée »
  S3 : « API à 80 % »
  S4 : « API à 90 %, tests à faire »
  S5 : « on a découvert que la correspondance objet ne gère pas le cas des lignes »
```

Cinq défauts.

Le découpage est horizontal. Rien ne s'exécute avant la troisième semaine, donc aucune surprise ne
peut apparaître avant — et une surprise en semaine cinq coûte beaucoup plus qu'en semaine une.

Aucun jalon n'a de critère d'acceptation. « Domaine terminé » n'est vérifiable par personne, y compris
par celui qui l'écrit.

« À quatre-vingts pour cent » n'est pas une mesure : c'est une impression. Le passage de quatre-vingts
à quatre-vingt-dix en une semaine complète le montre — ces pourcentages ne convergent jamais.

Les tests sont placés en dernier. Ils ne serviront donc pas de filet pendant le développement, ce que
`quality-regression-refactoring-001` désigne comme la condition de toute modification sûre.

Enfin, aucune marge n'était prévue, et la découverte de la semaine cinq n'avait aucune place où
atterrir.

## Vérification de compréhension

Reformulez « la couche d'accès aux données est terminée » en un jalon vertical, avec trois critères
d'acceptation vérifiables par quelqu'un d'autre.

:::quiz
id=final-project-evidence-001-check
question=Pourquoi un jalon doit-il être vertical plutôt qu'horizontal ?
option=Parce qu'un jalon vertical demande moins de travail qu'un jalon par couche
option=Parce qu'il traverse toutes les couches et s'exécute : les incompatibilités apparaissent au plus tôt, quand elles coûtent encore peu
option=Parce que les couches ne peuvent pas être développées indépendamment les unes des autres
correct=1
success=Correct : un découpage horizontal repousse toutes les surprises à l'intégration, c'est-à-dire au moment où elles sont les plus chères.
retry=Relisez le passage sur le jalon vertical, et demandez-vous quand se manifeste une incompatibilité entre couches dans chaque découpage.
:::

## Exercice guidé

Ouvrez `azure-release-evidence-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que laisse manquer chaque combinaison à deux conditions sur trois.
2. Implémentez la règle comme une conjonction stricte, sans exception.
3. Vérifiez les trois cas où une seule condition manque.
4. Appliquez ensuite cette règle au premier jalon de votre projet final et notez ce qui manque
   réellement.

## Exercice autonome

Découpez votre projet final en cinq jalons.

Pour chacun : l'énoncé vertical, trois critères d'acceptation vérifiables par un tiers, les preuves
que vous produirez, la durée estimée et la marge, la façon de revenir en arrière. Ajoutez le registre
des dettes assumées, avec pour chacune sa raison, son risque, son jalon de résolution et le contrôle
qui la fermera.

## Débogage

Un ticket indique : « Le jalon a été déclaré terminé et deux défauts bloquants ont été trouvés le
lendemain. »

1. **Symptôme** : l'état déclaré ne correspond pas à l'état réel.
2. **Hypothèse** : les critères d'acceptation étaient absents, ou formulés de sorte que seul l'auteur
   pouvait les évaluer.
3. **Preuve** : relisez les critères du jalon et demandez-vous si un tiers pouvait les vérifier sans
   vous.
4. **Prévention** : trois critères vérifiables écrits avant de commencer, et une preuve produite pour
   chacun.

## Entretien

Question posée à voix haute : *comment suivez-vous l'avancement d'un projet ?*

Une réponse solide remplace les pourcentages par des jalons verticaux, décrit des critères
d'acceptation vérifiables par un tiers, cite les preuves produites, et sait dire que la dette assumée
se déclare plutôt qu'elle ne se tait.

## Résumé

- Un jalon est vertical : il s'exécute et se démontre.
- Trois critères d'acceptation, écrits avant, vérifiables sans vous.
- La preuve est un artefact relisible, pas une affirmation.
- Tests verts, revue de sécurité, retour arrière documenté : les trois ensemble.
- Une dette déclarée se suit ; une dette tue devient invisible.

## Cartes de révision

Question : que vaut un avancement exprimé en pourcentage ? Réponse attendue : rien de vérifiable — ces
chiffres ne convergent jamais.

Question : pourquoi prévoir une marge explicite ? Réponse attendue : ne pas en prévoir revient à
promettre que rien d'imprévu n'arrivera.

## Test de maîtrise

Sans relire, découpez un projet de quatre semaines en jalons : énoncé vertical de chacun, trois
critères d'acceptation vérifiables, preuves produites, estimation et marge, condition de révision,
règle appliquée à un jalon rouvert, et registre des dettes assumées avec leur contrôle de fermeture.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
