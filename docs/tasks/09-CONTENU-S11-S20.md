# Incrément 09 — Contenu semaines 11 à 20

## 1. Statut

Validé le 5 août 2026.

## 2. Objectif

Écrire le contenu complet S11–S20 sur ASP.NET Core, HTTP, sécurité, tests, Git, Docker et CI/CD.

## 3. Contexte à lire

S11–S20 de `CURRICULUM.md`, `CONTENT_GUIDE.md`, `PRODUCT_SPEC.md`, `SECURITY.md`, `ROADMAP.md`, fiche `08`.

## 4. Prérequis

`08` validé ; catalogue et moteurs stables ; conventions .NET 10 confirmées.

## 5. Périmètre inclus

Leçons/exercices/projets S11–S20 : HTTP/REST, contrôleurs/DTO, validation/DI/config/secrets/erreurs, async/pagination/OpenAPI, authN/authZ/OWASP, xUnit/intégration/review, Git/PR, Docker/Compose et CI ; examens 5–6 et progression projets.

## 6. Périmètre explicitement exclu

Azure S21+, observabilité avancée, projet final, carrière, sujets distribués rares et changement de moteurs hors défaut séparé.

## 7. Fichiers ou projets principalement concernés

Contenu curriculum/exercises/interviews/projects S11–S20, starters API/CI conteneurisés et matrice de couverture.

## 8. Étapes d'implémentation

Planifier lots/seuils ; créer projets progressifs ; privilégier marché/pragmatisme ; inclure code review/diffs ; vérifier auth/secrets ; exécuter tous starters/tests/pipelines locaux ; revue sécurité et éditoriale.

## 9. Règles d'architecture

Exemples monolithes modulaires, règles métier hors contrôleurs, abstractions justifiées, pas de microservices ni surarchitecture pédagogique.

## 10. Règles de sécurité

Secrets factices/hors Git, exemples OWASP sûrs, auth sans contournement, images non-root, CI sans jeton réel, tests cachés/solutions protégés.

## 11. Tests à écrire

Validation contenu ; compilation API ; validation/erreurs/authN/authZ ; unit/integration ; Docker build/health ; pipeline syntaxe/build/test ; solutions et variantes ; liens/volumes.

## 12. Tests manuels à effectuer

Réaliser tranche API, PR/review, conflit Git en bac à sable, build Docker et CI locale ; vérifier clarté, durée, sécurité et défense orale.

## 13. Commandes de vérification

```powershell
dotnet test --no-build
docker compose config
# Construire/tester les starters API et Docker ; valider les workflows CI.
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## 14. Critères d'acceptation

S11–S20 complètes, projets progressifs, sécurité/test/livraison démontrés, tous contenus et pipelines verts, aucun thème S21+ anticipé.

## 15. Conditions d'arrêt

Secret réel, exemple vulnérable présenté comme correct, pipeline non exécutable, contenu exotique au détriment du socle, placeholder ou test rouge.

## 16. Mise à jour attendue de la roadmap

Cocher `09` seulement ; prochaine fiche `10`.

## 17. Format obligatoire du rapport final

Matrice/volumes S11–S20 ; projets ; validations API/Docker/CI ; tests/commandes ; sécurité ; revue manuelle ; écarts ; confirmation d'absence de S21+.
