# Incrément 02C — Lecteur

## 1. Statut

Validé le 25 juillet 2026.

## 2. Objectif

Afficher une leçon complète de référence avec navigation, quiz intégré, notes, signets, recherche et progression de lecture honnête.

## 3. Contexte à lire

`AGENTS.md`, `PRODUCT_SPEC.md`, `ARCHITECTURE.md`, `CONTENT_GUIDE.md`, `SECURITY.md`, `ROADMAP.md`, fiches `02A`/`02B`.

## 4. Prérequis

`02B` validé ; catalogue et recherche stables ; persistance 01C disponible pour notes/signets/progression.

## 5. Périmètre inclus

Liste module/leçon, rendu Markdown assaini, sommaire, blocs code, quiz de compréhension, notes/signets autosauvegardés, recherche UI, progression basée sur activités observables et une leçon complète de référence.

## 6. Périmètre explicitement exclu

Diagnostic, runner, indices/solutions, maîtrise, contenu de plusieurs semaines et examens.

## 7. Fichiers ou projets principalement concernés

Curriculum/Application, persistence Infrastructure, composants/pages Web, `content/curriculum` minimal et tests E2E.

## 8. Étapes d'implémentation

Créer projections publiques ; rendre Markdown sans HTML dangereux ; ajouter navigation/sommaire ; quiz ; notes/signets transactionnels ; progression non basée sur simple ouverture ; recherche ; leçon conforme aux 14 sections ; accessibilité.

## 9. Règles d'architecture

Web rend des projections Application ; contenu reste fichier ; progression utilisateur seule en SQLite ; aucune solution cachée dans le client.

## 10. Règles de sécurité

Assainir Markdown/liens, CSP, encodage, CSRF sur mutations, tailles bornées, autosave sans perte et jamais de HTML/script arbitraire.

## 11. Tests à écrire

Rendu sections, sanitization XSS, sommaire, réponse quiz, notes/signets persistants, autosave, recherche, progression non attribuée à une simple visite, contenu caché absent du HTML.

## 12. Tests manuels à effectuer

Parcourir clavier/mobile, lire leçon, utiliser sommaire/recherche, répondre quiz, écrire note, signet, recharger/redémarrer et vérifier persistance/focus.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## 14. Critères d'acceptation

Leçon autonome complète, UI accessible, notes/signets durables, recherche utile, progression honnête, tests E2E et XSS verts.

## 15. Conditions d'arrêt

Contenu non conforme, HTML brut non maîtrisé, solution/test caché exposé, simple page vue comptée comme maîtrise ou test E2E en échec.

## 16. Mise à jour attendue de la roadmap

Cocher `02C` seulement ; prochaine fiche `03A`.

## 17. Format obligatoire du rapport final

Routes/composants ; leçon ; persistance ; sécurité rendu ; tests/commandes ; test manuel ; accessibilité ; limites ; confirmation d'absence de diagnostic.
