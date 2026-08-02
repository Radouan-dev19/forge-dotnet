# Incrément 04B — Runner local

## 1. Statut

Validé le 27 juillet 2026.

## 2. Objectif

Définir le contrat de compilation/test et orchestrer les résultats avec un double sûr, sans exécuter de code utilisateur ni appeler Docker.

## 3. Contexte à lire

`ARCHITECTURE.md`, `SECURITY.md`, `CONTENT_GUIDE.md`, `ROADMAP.md`, fiche `04A`, projet `ForgeDotNet.CodeRunner`.

## 4. Prérequis

`04A` validé ; format exercice/test stabilisé.

## 5. Périmètre inclus

`ICodeRunner`, RunRequest/RunResult, diagnostics compilation/tests séparés, statuts timeout/annulation/indisponible, orchestration Practice et double déterministe pour tests/UI.

## 6. Périmètre explicitement exclu

Processus local réel, `dotnet` lancé sur soumission, Docker, limites OS effectives, mode manuel zip et contenu initial.

## 7. Fichiers ou projets principalement concernés

Contrats Application, modèles Domain si nécessaires, `ForgeDotNet.CodeRunner`, Practice Web et tests avec doubles.

## 8. Étapes d'implémentation

Définir contrat minimal ; limiter fichiers/tailles au contrat ; mapper résultats ; intégrer annulation/idempotence ; double configurable ; UI compilation vs tests ; état « indisponible » honnête ; documenter frontière de confiance.

## 9. Règles d'architecture

Web appelle un cas d'usage ; Application dépend d'une abstraction ; CodeRunner implémente ; aucun appel shell dans Web/Application ; pas de référence inverse.

## 10. Règles de sécurité

Aucun code exécuté ; données sensibles exclues du contrat ; sorties bornées conceptuellement ; commandes jamais acceptées depuis l'utilisateur ; tests cachés non inclus dans réponse.

## 11. Tests à écrire

Succès compilation/test, erreur compilation empêchant tests, échec visible/caché redacted, timeout, annulation, runner indisponible, output tronqué, double appel idempotent.

## 12. Tests manuels à effectuer

Utiliser le double pour chaque statut, vérifier messages, annulation, historique et absence de formulation « code validé » en mode indisponible.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## 14. Critères d'acceptation

Contrat stable, orchestration testée, UI différencie compilation/tests, aucun code utilisateur exécuté et double remplaçable par Docker.

## 15. Conditions d'arrêt

Besoin de shell réel, secret/code caché dans résultat, contrat couplé à Docker, ambiguïté de statuts ou test en échec.

## 16. Mise à jour attendue de la roadmap

Cocher `04B` seulement ; prochaine fiche `04C`.

## 17. Format obligatoire du rapport final

Contrats/statuts ; dépendances ; intégration ; tests ; sorties UI ; sécurité ; commandes ; limites ; confirmation qu'aucune exécution réelle/Docker n'existe.
