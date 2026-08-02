# Incrément 00 — Conception

## 1. Statut

Validé. Fiche rétrospective ; ne pas réexécuter sans décision humaine de rouvrir la conception.

## 2. Objectif

Définir le produit, l'architecture, le curriculum, les formats de contenu, la sécurité et la roadmap avant toute réalisation massive.

## 3. Contexte à lire

`AGENTS.md`, la demande produit d'origine, puis l'ensemble de `docs/*.md` s'il s'agit d'une révision.

## 4. Prérequis

Dépôt Git initialisé ; besoin produit disponible ; aucun prérequis applicatif.

## 5. Périmètre inclus

`AGENTS.md`, `PRODUCT_SPEC.md`, `ARCHITECTURE.md`, `CURRICULUM.md`, `CONTENT_GUIDE.md`, `SECURITY.md`, `ROADMAP.md` ; diagrammes, données, règles de maîtrise, stratégies runner/SQL, risques et compromis.

## 6. Périmètre explicitement exclu

Solution .NET, code applicatif, packages, infrastructure Docker, contenu massif et données de démonstration.

## 7. Fichiers ou projets principalement concernés

`AGENTS.md` et `docs/*.md` uniquement.

## 8. Étapes d'implémentation

1. Inspecter le dépôt et reformuler le produit. 2. Établir hypothèses et non-objectifs. 3. Définir modules, dépendances et données. 4. Formaliser pédagogie, sécurité, contenu et roadmap. 5. Contrôler la cohérence croisée.

## 9. Règles d'architecture

Monolithe modulaire ; dépendances `Web → Application`, `Infrastructure → Application/Domain`, `Application → Domain`, `Domain → aucune` ; aucun microservice ni couche sans utilité.

## 10. Règles de sécurité

Inclure modèle de menace, séparation des runners, protection des tests cachés, confidentialité locale et absence de télémétrie externe par défaut.

## 11. Tests à écrire

Aucun test logiciel ; créer une checklist documentaire vérifiant présence, liens, contradictions, responsabilités et couverture des exigences.

## 12. Tests manuels à effectuer

Relire chaque parcours, seuil, diagramme, volume de contenu, risque et critère MVP ; vérifier qu'un agent peut comprendre la suite sans hypothèse implicite majeure.

## 13. Commandes de vérification

```powershell
git status --short
rg -n "TODO|TBD|lorem|placeholder" AGENTS.md docs
rg --files docs
```

## 14. Critères d'acceptation

Sept documents cohérents, risques/compromis explicites, roadmap complète, aucune fonctionnalité développée et aucune promesse d'emploi ou salaire.

## 15. Conditions d'arrêt

Besoin contradictoire, choix structurant sans information, document obligatoire manquant ou contenu applicatif commencé par erreur.

## 16. Mise à jour attendue de la roadmap

Cocher uniquement `00` après validation humaine ; désigner `01A` comme prochaine tâche.

## 17. Format obligatoire du rapport final

Documents créés/modifiés ; synthèse architecture/parcours ; hypothèses ; risques ; compromis à valider ; contrôles de cohérence ; confirmation d'absence de code.
