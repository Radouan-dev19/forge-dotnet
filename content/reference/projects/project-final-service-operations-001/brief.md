# Projet final — service métier exploitable

## Mission

Concevez et réalisez vous-même un service métier local en .NET sous forme de monolithe modulaire. Choisissez un domaine différent des mini-projets déjà livrés, un acteur principal et un parcours critique mesurable. Le produit doit pouvoir être construit, testé et démontré hors ligne.

Forge.NET fournit uniquement le brief, les jalons, les critères et les questions de revue. Aucun squelette métier, code de remise, modèle de données final ou solution complète nʼest fourni. Une consultation de documentation ne remplace pas votre justification.

## Contraintes non négociables

- un seul déployable applicatif et des modules cohésifs ;
- règles métier dans le domaine, orchestration dans lʼapplication, adaptateurs dans lʼinfrastructure et UI sans règle importante ;
- persistance locale reproductible, données de démonstration factices et migrations rejouables ;
- succès, frontières, refus, autorisation, erreurs et non-régressions couverts par des preuves utiles ;
- aucune donnée personnelle réelle, aucune valeur sensible et aucune dépendance réseau obligatoire ;
- déploiement Azure facultatif : le mode simulé satisfait le projet ;
- aucun échec applicable masqué.

## Dossier de preuve à produire

Créez vos propres fichiers de décision, matrice de risques, journal dʼincident, résultats de commandes et support de défense. Pour chaque affirmation, indiquez lʼartefact ou la commande qui la soutient. Les captures ne remplacent pas un test reproductible.

## Questions de revue contradictoire

1. Quel comportement incorrect plausible reste vert avec vos tests ?
2. Quelle donnée ou quel droit traverse une frontière sans validation ?
3. Comment repartir de zéro et comment revenir à lʼartefact précédent ?
4. Quel signal dʼincident est actionnable et lequel ajoute seulement du bruit ?
5. Quelle décision changerait si la charge, le coût ou lʼéquipe changeait ?

## Défense

Présentez le parcours critique, un refus de sécurité, un incident simulé résolu et une décision dʼarchitecture. Terminez par un résumé de deux minutes en anglais : problem, constraint, decision, evidence, limitation, next experiment.
