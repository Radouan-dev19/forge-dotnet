# Incrément 04A — Pratique

## 1. Statut

Validé le 26 juillet 2026.

## 2. Objectif

Implémenter réflexion préalable, tentatives, indices progressifs, déverrouillage des solutions et historique, sans exécuter encore de code.

## 3. Contexte à lire

`PRODUCT_SPEC.md`, `ARCHITECTURE.md`, `CONTENT_GUIDE.md`, `SECURITY.md`, `ROADMAP.md`, fiche `03C`.

## 4. Prérequis

`03C` validé ; schémas exercice/catalogue/persistance disponibles.

## 5. Périmètre inclus

Reflection, Attempt, HintUsage ; six champs préalables ; tentative sérieuse ; quatre niveaux d'indice ; deux tentatives + délai ; solution vue/non maîtrisée ; explication personnelle ; variante ; historique/comparaison textuelle.

## 6. Périmètre explicitement exclu

Compilation/tests, runner local/Docker, scoring de maîtrise, révisions planifiées et contenu de 10 exercices.

## 7. Fichiers ou projets principalement concernés

Practice Domain/Application/Infrastructure/Web, migrations, contenu exercice minimal, tests Unit/Integration/EndToEnd.

## 8. Étapes d'implémentation

Modéliser états/transitions ; imposer réflexion ; détecter doublon substantiel sans prétendre détecter toute triche ; enregistrer aide ; contrôler délais avec horloge injectable ; verrouiller solution ; marquer non maîtrisé et demander explication/variante ; afficher historique.

## 9. Règles d'architecture

Transitions pures Domain ; cas d'usage Application ; horloge/persistence Infrastructure ; Web sans mutation directe ; aucune dépendance à un runner.

## 10. Règles de sécurité

Solution/tests cachés restent serveur ; autorisation par état ; CSRF ; tailles d'entrée bornées ; ne pas loguer réponse/solution ; IDs non prédictibles si exposés.

## 11. Tests à écrire

Réflexion incomplète, doublon, tentative sérieuse, ordre/plafond indices, délai, moins de deux tentatives, solution consultée, explication requise, variante, accès direct interdit, concurrence double clic.

## 12. Tests manuels à effectuer

Tenter indice sans réflexion, progresser H1→H4, simuler délai, consulter solution après conditions, vérifier statut et historique ; navigation clavier/mobile.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## 14. Critères d'acceptation

Protocole anti-dépendance appliqué côté serveur, solutions protégées, historique exact, tests de contournement verts et aucune exécution prétendue.

## 15. Conditions d'arrêt

Solution récupérable hors conditions, aide non tracée, mode manuel présenté comme automatique, horloge non testable ou besoin de runner.

## 16. Mise à jour attendue de la roadmap

Cocher `04A` seulement ; prochaine fiche `04B`.

## 17. Format obligatoire du rapport final

Machine d'états ; cas d'usage ; persistence/routes ; tests anti-contournement ; commandes ; parcours manuel ; sécurité ; limites ; confirmation d'absence de runner.
