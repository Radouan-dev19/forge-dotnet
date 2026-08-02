# Incrément 03C — Plan personnalisé

## 1. Statut

Validé le 26 juillet 2026.

## 2. Objectif

Transformer le diagnostic évalué en recommandations et plan hebdomadaire explicable, modifiable puis accepté par l'utilisateur.

## 3. Contexte à lire

`PRODUCT_SPEC.md`, `CURRICULUM.md`, `ARCHITECTURE.md`, `ROADMAP.md`, fiches `03A`/`03B`.

## 4. Prérequis

`03B` validé ; carte/lacunes fiables ; profil et heures disponibles persistés.

## 5. Périmètre inclus

Règles de recommandation, priorités critiques, charge 10–15 h adaptée aux disponibilités, remédiations, plan de semaines, justification, ajustement utilisateur, acceptation et version.

## 6. Périmètre explicitement exclu

Exercices interactifs, runner, score de maîtrise, révisions automatiques et contenu massif.

## 7. Fichiers ou projets principalement concernés

WeeklyPlan Domain/Application/Infrastructure, projections/pages Web, tests Integration/EndToEnd.

## 8. Étapes d'implémentation

Définir contraintes ; classer lacunes/prérequis ; générer plan sans supprimer tests de maîtrise ; borner charge ; expliquer chaque recommandation ; permettre ajustements sûrs ; accepter/versionner ; marquer provisoire si diagnostic incomplet.

## 9. Règles d'architecture

Plan calculé dans Domain/Application ; curriculum fichier reste source ; décisions utilisateur persistées ; aucune logique de recommandation dans Razor.

## 10. Règles de sécurité

Pas de manipulation culpabilisante, de promesse d'embauche ou de données externes ; validation serveur des ajustements ; journaliser seulement métadonnées utiles.

## 11. Tests à écrire

Lacune critique prioritaire, compétence forte raccourcie mais test conservé, heures faibles/élevées, plan provisoire, prérequis, absence de cycle, ajustement invalide, acceptation/version.

## 12. Tests manuels à effectuer

Générer depuis plusieurs diagnostics, lire justifications, modifier charge, accepter, recharger et vérifier absence de contenu impossible ou promesse.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## 14. Critères d'acceptation

Plan explicable, faisable, persistant, respectant prérequis et lacunes ; test d'intégration diagnostic→plan accepté vert.

## 15. Conditions d'arrêt

Plan contourne une compétence critique, dépasse charge sans avertissement, curriculum insuffisant ou besoin de commencer Practice.

## 16. Mise à jour attendue de la roadmap

Cocher `03C` uniquement ; prochaine fiche `04A`.

## 17. Format obligatoire du rapport final

Règles ; données ; exemples ; routes ; tests/commandes ; parcours manuel ; limites ; confirmation d'absence de Practice/Runner.
