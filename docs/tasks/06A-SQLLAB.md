# Incrément 06A — SqlLab

## 1. Statut

Validé le 28 juillet 2026.

## 2. Objectif

Fournir SQL Server de laboratoire isolé, base jetable/réinitialisable, éditeur, exécution bornée et validation structurée des résultats.

## 3. Contexte à lire

Sections SqlLab de `SECURITY.md` et `ARCHITECTURE.md`, section SQL de `CONTENT_GUIDE.md`, `ROADMAP.md`, fiche `05`.

## 4. Prérequis

`05` validé ; Docker/Compose opérationnels ; schéma SQL v1 disponible ; menace SQL revue.

## 5. Périmètre inclus

SQL Server conteneurisé interne, dataset minimal, base par session/reset vérifié, login least privilege, éditeur/exécution, timeout/annulation/quota, schéma visible, validation colonnes/valeurs/ordre/effets, erreurs utiles.

## 6. Périmètre explicitement exclu

12 scénarios pédagogiques, exercices EF Core, maîtrise, accès à SQLite progression, publication SQL sur réseau public et procédures OS.

## 7. Fichiers ou projets principalement concernés

SqlLab Domain/Application/Infrastructure/Web, Compose/réseau/seed SQL, tests Integration/Security/EndToEnd, runbook.

## 8. Étapes d'implémentation

Créer réseau interne ; configurer instance sans port public par défaut ; créer login minimal ; provisionner DB jetable ; exécuter sans concaténation de commandes système ; imposer timeout/quota/annulation ; valider résultat ; reset avec preuve ; health ; documenter destruction.

## 9. Règles d'architecture

SqlLab séparé de SQLite progression ; Application porte contrats ; Infrastructure pilote SQL ; Web ne reçoit ni secret ni accès direct ; contenu décrit dataset/attendu.

## 10. Règles de sécurité

Contrôles renforcés obligatoires : aucune permission serveur/inter-base, `xp_cmdshell`/external scripts interdits, réseau interne, identifiants serveur secrets, base jetable, transaction de protection, lignes/octets/temps bornés, validation serveur ; une blacklist seule est interdite.

## 11. Tests à écrire

SELECT normal, timeout, résultat massif, annulation, reset, deux sessions isolées, tentative inter-base, DDL serveur, procédure OS, lecture SQLite impossible, login sans privilèges, secret absent client/log, validation ordonnée/non ordonnée.

## 12. Tests manuels à effectuer

Inspecter droits/réseau ; exécuter requêtes sûres et hostiles ; reset ; comparer sessions ; inspecter ports, logs, connexions, volumes et absence d'effet sur progression.

## 13. Commandes de vérification

```powershell
docker compose config
docker compose up -d <service-sql-lab>
docker compose ps
dotnet test --no-build --filter "Category=SqlLabSecurity"
dotnet test --no-build
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
docker compose down
```

## 14. Critères d'acceptation

Isolation session/progression prouvée, privilèges minimaux, attaques refusées, timeout/quota/reset fiables, validation exacte et aucun secret navigateur.

## 15. Conditions d'arrêt

Port public involontaire, rôle serveur, accès inter-base/OS, reset non fiable, secret exposé, test d'isolation en échec ou besoin d'écrire les 12 scénarios.

## 16. Mise à jour attendue de la roadmap

Cocher `06A` seulement après revue sécurité SQL ; prochaine fiche `06B`.

## 17. Format obligatoire du rapport final

Topologie/ports ; droits ; cycle DB ; validation ; matrice d'attaques ; tests/commandes ; résultats manuels ; risques résiduels ; confirmation d'absence de contenu 06B.
