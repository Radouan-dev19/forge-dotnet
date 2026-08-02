# Incrément 07C — Examens et dashboard

## 1. Statut

Validé le 29 juillet 2026.

## 2. Objectif

Livrer examen sans aide, tirage/minuterie/verrouillage/rapport et tableau de bord honnête alimenté par données réelles.

## 3. Contexte à lire

`PRODUCT_SPEC.md`, examens/mesures de `CURRICULUM.md`, `SECURITY.md`, `ARCHITECTURE.md`, `ROADMAP.md`, fiches `07A`/`07B`.

## 4. Prérequis

`07B` validé ; Practice, runners, Mastery et Reviews fonctionnels.

## 5. Périmètre inclus

Exam/ExamAttempt, banque compatible, tirage aléatoire auditable, durée serveur, aucun indice/solution, tests cachés, fin/abandon, rapport après fin, replanification ; dashboard temps actif, forces/faiblesses, dues, objectif, portes, examens, aides et réussite avant solution.

## 6. Périmètre explicitement exclu

Contenu complet des 8 examens, proctoring intrusif, promesse d'employabilité, désactivation infaillible du copier-coller.

## 7. Fichiers ou projets principalement concernés

Exam/Mastery/Analytics Domain/Application/Infrastructure/Web, migrations, tests E2E et sécurité.

## 8. Étapes d'implémentation

Créer session figée/tirage ; minuterie serveur ; contexte sans aide ; empêcher endpoints d'indice/solution ; différer détail ; finaliser atomiquement ; alimenter maîtrise/révisions ; projections dashboard factuelles ; temps actif avec seuil inactivité.

## 9. Règles d'architecture

États/temps Domain/Application ; contenu fichier ; tentatives SQLite ; runner inchangé ; dashboard projection lecture, jamais source de score.

## 10. Règles de sécurité

Tests/solutions serveur, IDs opaques, anti-CSRF, fin atomique, logs sans réponses, copier-coller présenté comme friction seulement, aucune surveillance caméra.

## 11. Tests à écrire

Tirage/seed, durée, reprise interdite/autorisée selon règle, indice/solution bloqués, tests cachés, double fin, abandon, rapport différé, échec→révision, métriques sans fausse donnée, inactivité et porte critique.

## 12. Tests manuels à effectuer

Passer/abandonner un examen, tenter URL d'indice/solution, dépasser temps, vérifier rapport/dashboard, clavier/mobile et absence de métriques non disponibles.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## 14. Critères d'acceptation

Examen réellement sans aide, temps/tirage/rapport fiables, maîtrise/révision intégrées, dashboard uniquement factuel et E2E vert.

## 15. Conditions d'arrêt

Indice/solution accessible, timer client seul, rapport avant fin, métrique inventée, score critique compensé ou E2E en échec.

## 16. Mise à jour attendue de la roadmap

Cocher `07C` seulement ; prochaine fiche `08`.

## 17. Format obligatoire du rapport final

États/tirage/temps ; protections ; dashboard/métriques ; tests/commandes ; parcours adversarial ; limites ; confirmation d'absence de contenu massif.
