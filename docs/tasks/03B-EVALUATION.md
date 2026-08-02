# Incrément 03B — Évaluation du diagnostic

## 1. Statut

Validé le 26 juillet 2026.

## 2. Objectif

Noter déterministement un diagnostic terminé, exprimer l'incertitude, produire carte de compétences, lacunes critiques et rapport prudent.

## 3. Contexte à lire

`PRODUCT_SPEC.md`, `ARCHITECTURE.md`, `CURRICULUM.md`, `ROADMAP.md`, fiche `03A` et ses contrats réels.

## 4. Prérequis

`03A` validé ; réponses figées et banque versionnée.

## 5. Périmètre inclus

Clés/rubriques de notation, pondérations par compétence/difficulté, score borné, intervalle/confiance qualitative, contrôles de fiabilité, niveau prudent, lacunes critiques et rapport explicable.

## 6. Périmètre explicitement exclu

Génération/acceptation du plan hebdomadaire, maîtrise globale, portes d'employabilité et personnalisation de contenu.

## 7. Fichiers ou projets principalement concernés

Diagnostic Domain/Application, projections Web, contenu de rubriques, tests Unit/Integration.

## 8. Étapes d'implémentation

Formaliser barème versionné ; calculer par compétence sans moyenne trompeuse ; traiter non-réponse/temps/diagnostic incomplet ; dériver incertitude ; identifier lacunes ; rendre explications et limites ; conserver snapshot du barème.

## 9. Règles d'architecture

Calcul pur dans Domain, orchestration Application, persistance des observations et version, UI en lecture ; ne pas réinterpréter silencieusement un ancien résultat.

## 10. Règles de sécurité

Réponses modèles non servies avant fin ; rapport n'expose pas questions cachées réutilisables ; aucune affirmation d'emploi/salaire ; données locales minimales.

## 11. Tests à écrire

Tout juste/tout faux, compétence absente, diagnostic incomplet, réponses faciles seules, pondérations, bornes, arrondis, incertitude, lacune critique, stabilité par version.

## 12. Tests manuels à effectuer

Comparer profils contrastés ; vérifier texte prudent, détails compréhensibles, aucune compensation d'une faiblesse critique et rapport incomplet signalé.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## 14. Critères d'acceptation

Notation déterministe/versionnée, carte honnête, incertitude visible, cas limites verts et aucune recommandation de plan anticipée.

## 15. Conditions d'arrêt

Barème non validé, score dépendant de l'ordre, faiblesse masquée, clé exposée ou nécessité de commencer `03C`.

## 16. Mise à jour attendue de la roadmap

Cocher `03B` seulement ; prochaine fiche `03C`.

## 17. Format obligatoire du rapport final

Formule/barème ; gestion incertitude ; exemples de rapports ; tests/cas limites ; sécurité ; commandes ; écarts ; confirmation d'absence de plan.
