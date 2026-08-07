# Incrément 10 — Contenu semaines 21 à 24

## 1. Statut

Validé le 6 août 2026.

Le blocage Docker a été corrigé sans étendre les limites 04C : les deux examens EF préparent leur petit dataset SQLite directement en mémoire et le scénario `IQueryable` capture la commande EF réellement exécutée au lieu de traduire deux fois la requête. Le contrôle exhaustif des 135 exercices utilise la concurrence documentée par défaut de deux conteneurs et rapporte désormais lʼidentifiant complet en cas dʼéchec. Deux passes ciblées EF, puis les 270 exécutions solution/starter, sont vertes.

La passe finale de `scripts/verify.ps1` réussit : build sans avertissement, 124/124 tests unitaires, 155/155 tests dʼintégration, 28/28 tests E2E, fixtures valides acceptées, fixtures invalides refusées, 40 scénarios SQL/EF et 481 documents de référence valides, catalogues chargeables et formatage propre. `docker compose config --quiet`, `git diff --check`, le mode Azure simulé, la résolution dʼincident et lʼexport carrière local réussissent également. La recherche de secrets ne contient que des règles, exemples factices, identifiants pédagogiques et chemins hors Git ; aucun secret réel nʼa été détecté. Aucune ressource Azure ni remise de projet final nʼa été créée.

## 2. Objectif

Achever S21–S24 : Azure utile, observabilité/performance/sécurité, projet final guidé, entretiens, anglais et carrière.

## 3. Contexte à lire

S21–S24 de `CURRICULUM.md`, `PRODUCT_SPEC.md`, `CONTENT_GUIDE.md`, `SECURITY.md`, `ROADMAP.md`, fiche `09`.

## 4. Prérequis

`09` validé ; volumes cumulés connus ; parcours/projets antérieurs fonctionnels.

## 5. Périmètre inclus

Azure App Service/Container Apps, Azure SQL/Storage/Key Vault/Managed Identity simple, App Insights/logs/métriques/coûts, performance/sécurité ; projet final avec jalons/grille ; examens 7–8 ; 190 questions selon répartition ; 50 cartes anglais ; carrière/CV/STAR/candidatures/négociation ; plan post-embauche.

## 6. Périmètre explicitement exclu

Génération complète de la remise finale, promesse salariale/emploi, cloud obligatoire/payé, systèmes distribués avancés et parcours post-embauche implémenté.

## 7. Fichiers ou projets principalement concernés

Contenu curriculum/interviews/english/projects/career S21–S24, exemples de déploiement sans secrets et matrice finale des volumes.

## 8. Étapes d'implémentation

Planifier lots ; utiliser émulateur/mode manuel quand cloud absent ; documenter coûts ; écrire observabilité/incidents ; définir jalons et rubric projet sans solution avant remise ; atteindre répartitions entretien/anglais ; créer outils carrière honnêtes ; audit des volumes.

## 9. Règles d'architecture

Le cloud reste un sujet/exemple, pas une dépendance de Forge.NET ; projet final suit monolithe pragmatique ; aucune remise générée à la place de l'apprenant.

## 10. Règles de sécurité

Aucun identifiant Azure/PII, secrets via mécanismes adaptés, coûts/ressources supprimables, données CV averties sensibles, réponses entretien/solutions verrouillées.

## 11. Tests à écrire

Validation/volumes exacts, exemples IaC/config sans secret, starters buildables, exercices observabilité/performance/sécurité, rubriques projet/anglais/entretien, examens 7–8 et liens.

## 12. Tests manuels à effectuer

Suivre un déploiement documenté ou mode simulé, résoudre incident, présenter architecture en anglais, parcourir jalons projet, exporter CV/preuves et vérifier avertissements coût/confidentialité.

## 13. Commandes de vérification

```powershell
dotnet test --no-build
# Valider tous contenus, volumes, starters et configurations S21-S24.
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
rg -n "secret|password|token|connection string" content docs
```

## 14. Critères d'acceptation

24 semaines couvertes, volumes obligatoires atteints sans remplissage, projet final guidé non fourni, ressources cloud optionnelles, carrière honnête et tous tests verts.

## 15. Conditions d'arrêt

Secret/coût non maîtrisé, solution finale livrée avant remise, promesse d'emploi/salaire, volume artificiel, contenu non autonome ou validation en échec.

## 16. Mise à jour attendue de la roadmap

Cocher `10` seulement ; prochaine fiche `11`.

## 17. Format obligatoire du rapport final

Matrice finale volumes ; lots ; Azure/coûts ; projet/examens ; entretiens/anglais/carrière ; tests/commandes ; risques ; confirmation de non-génération du projet final.
