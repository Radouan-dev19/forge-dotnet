# Carnet STAR

Les questions comportementales — « racontez-moi une fois où… » — se préparent comme un exercice de
code : avec une méthode, des exemples travaillés et des reprises à blanc. La méthode STAR structure
chaque récit en quatre temps, auxquels ce carnet ajoute un cinquième que les recruteurs seniors
attendent : la réflexion.

## La grille

- **Situation** : le contexte en deux phrases, sans nom privé ni détail inutile.
- **Task** : votre responsabilité réelle et bornée — ce qui dépendait de vous, pas de l'équipe.
- **Action** : vos décisions, au singulier (« j'ai isolé », « j'ai choisi »), dans l'ordre réel.
- **Result** : une observation vérifiable. Quand rien n'a été mesuré, dites « non mesuré » — cette
  franchise crédibilise tout le reste du récit.
- **Réflexion** : l'erreur commise ou le feedback reçu, et ce que vous feriez autrement.

Règle d'or héritée du produit : une réponse modèle consultée n'est pas une préparation. Rédigez
d'abord la vôtre, puis comparez.

## Exemple 1 — un DebugLab (diagnostic méthodique)

Issu du scénario `debug-null-reference-001`, « Tracer une NullReferenceException ».

- **Situation** : un code de normalisation de texte plantait sur certaines entrées, avec une trace
  d'exception mais sans cause évidente ; je ne connaissais pas ce code.
- **Task** : reproduire le plantage, trouver la cause racine et livrer un correctif accompagné d'un
  test qui empêche le retour du défaut.
- **Action** : j'ai reproduit avec l'entrée minimale, posé un point d'arrêt sur la ligne incriminée,
  inspecté les variables locales, formulé une hypothèse écrite avant de toucher au code, puis
  corrigé le seul chemin fautif.
- **Result** : correctif validé par la suite de tests du scénario, journal de diagnostic exporté ;
  temps de résolution non mesuré.
- **Réflexion** : ma première hypothèse accusait la mauvaise couche ; noter l'hypothèse par écrit
  m'a évité de corriger un symptôme au lieu de la cause.

## Exemple 2 — un projet console (livraison bornée)

Issu de `project-log-analyzer-001`, « Analyseur de journaux ».

- **Situation** : construire un outil console qui agrège des journaux applicatifs, avec un contrat
  d'acceptation imposé et des cas de test cachés.
- **Task** : livrer une soumission unique qui passe toutes les suites, y compris les cas que je ne
  voyais pas.
- **Action** : j'ai écrit d'abord la validation des entrées, traité les lignes malformées comme des
  cas normaux plutôt que comme des exceptions, et gardé une fonction par règle d'agrégation pour
  pouvoir tester chacune isolément.
- **Result** : projet livré — toutes les suites vertes sur la même soumission, en environnement
  isolé et reproductible.
- **Réflexion** : j'avais sous-estimé les cas limites d'encodage ; depuis, je liste les bornes avant
  d'écrire la première ligne.

## Exemple 3 — un incident simulé (comportement sous contrainte)

Issu de `project-incident-drill-001`, « Exercice d'incident simulé ».

- **Situation** : un service présentait des symptômes dégradés — erreurs et lenteurs — dans un
  exercice chronométré reproduisant une panne.
- **Task** : rétablir un comportement correct et produire un compte rendu exploitable par un tiers.
- **Action** : j'ai d'abord stabilisé — limiter l'impact avant de chercher l'élégance —, consigné
  chaque observation horodatée, puis remonté la chaîne des causes une fois le service redevenu sain.
- **Result** : incident clos avec les vérifications attendues au vert et un compte rendu structuré ;
  il s'agissait d'une simulation locale, ce que je précise systématiquement.
- **Réflexion** : j'ai perdu du temps à chercher la cause avant de limiter l'impact ; l'ordre
  inverse est désormais mon réflexe.

## Constituer votre banque

Visez huit à dix récits couvrant : un bogue difficile, un choix technique défendu, une erreur
assumée, un désaccord résolu, un apprentissage rapide, une contrainte de temps, un travail relu par
un tiers, un moment où vous avez demandé de l'aide. Votre parcours antérieur à la reconversion est
une source légitime : les employeurs valorisent un conflit client bien géré autant qu'un correctif.

## Protocole de répétition

1. Rédigez le récit complet, puis réduisez-le à cinq puces mémorisables.
2. Racontez-le à voix haute en moins de deux minutes, sans lire.
3. Répondez à une variante (« et si le délai avait été divisé par deux ? »).
4. Reprenez-le à blanc une semaine plus tard : ce qui a disparu n'était pas acquis.

Ce carnet ne produit aucune preuve de maîtrise : un récit se juge en face d'un humain.
