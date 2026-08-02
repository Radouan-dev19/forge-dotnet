# Incrément 03A — Diagnostic

## 1. Statut

Validé le 25 juillet 2026.

## 2. Objectif

Créer la session de diagnostic, la banque, l'échantillonnage stratifié, la minuterie, la reprise et la collecte des réponses, sans calcul final de niveau.

## 3. Contexte à lire

`PRODUCT_SPEC.md`, flux diagnostic d'`ARCHITECTURE.md`, `CURRICULUM.md`, `CONTENT_GUIDE.md`, `SECURITY.md`, `ROADMAP.md`, fiche `02C`.

## 4. Prérequis

`02C` validé ; persistance et moteur de contenu opérationnels ; compétences/IDs définis.

## 5. Périmètre inclus

Banque couvrant logique, C#, lecture, debug, SQL, HTTP, Git, tests et anglais ; session figée ; échantillonnage ; sections chronométrées ; sauvegarde/reprise ; diagnostic réduit de test et initial exploitable.

## 6. Périmètre explicitement exclu

Score final, incertitude, carte, recommandation, plan personnalisé, runner de code et maîtrise.

## 7. Fichiers ou projets principalement concernés

Module Diagnostic Domain/Application/Infrastructure/Web, contenu diagnostic, migrations nécessaires, tests Integration/EndToEnd.

## 8. Étapes d'implémentation

Définir session/réponse/état ; versionner banque ; figer sélection et ordre ; implémenter minuterie serveur ; autosauvegarder ; reprendre sans regénérer ; terminer/abandonner explicitement ; UI accessible ; diagnostic réduit déterministe.

## 9. Règles d'architecture

Temps et transitions dans Domain/Application ; horloge injectable ; contenu en fichiers ; réponses/session en SQLite ; aucune notation dans Web.

## 10. Règles de sécurité

Questions/réponses attendues séparées, protection CSRF, minuterie non fiable uniquement côté client, pas de surveillance intrusive ni détail sensible dans logs.

## 11. Tests à écrire

Échantillonnage couvrant chaque compétence, stabilité session, reprise, expiration section, double soumission, interruption, session incomplète, diagnostic réduit E2E.

## 12. Tests manuels à effectuer

Démarrer, répondre, actualiser, reprendre, laisser expirer, abandonner et terminer ; vérifier clavier, avertissements temporels et état « incomplet » honnête.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## 14. Critères d'acceptation

Session reproductible, couverture des neuf domaines, minuterie/reprise fiables, diagnostic réduit E2E vert, aucun score final anticipé.

## 15. Conditions d'arrêt

Banque insuffisante, sélection non reproductible, minuterie contournable par simple client, perte de réponses ou besoin d'implémenter 03B.

## 16. Mise à jour attendue de la roadmap

Cocher `03A` uniquement ; prochaine fiche `03B`.

## 17. Format obligatoire du rapport final

Modèle/états ; banque/couverture ; algorithme sélection ; routes ; persistance ; tests/commandes ; parcours manuel ; limites ; confirmation d'absence d'évaluation finale.
