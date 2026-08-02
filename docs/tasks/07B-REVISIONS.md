# Incrément 07B — Révisions

## 1. Statut

Validé le 29 juillet 2026.

## 2. Objectif

Créer cartes et planification espacée transparente à partir des erreurs, bugs, questions ratées et solutions consultées.

## 3. Contexte à lire

`PRODUCT_SPEC.md`, règles de planification d'`ARCHITECTURE.md`, `CONTENT_GUIDE.md`, `ROADMAP.md`, fiche `07A`.

## 4. Prérequis

`07A` validé ; événements de maîtrise fiables ; horloge injectable.

## 5. Périmètre inclus

ReviewItem, sources, intervalles J+1/J+3/J+7/J+14/J+30, J+1/J+7 après solution/échec, adaptation réussite/échec, file du jour, cartes manuelles/personnelles et transparence du prochain intervalle.

## 6. Périmètre explicitement exclu

Examens sans aide, tirage chronométré, dashboard complet et contenu final des cartes.

## 7. Fichiers ou projets principalement concernés

Mastery/Reviews Domain/Application/Infrastructure/Web, migrations et tests à horloge simulée.

## 8. Étapes d'implémentation

Modéliser source/échéance/état ; générer idempotemment ; appliquer politique versionnée ; répondre et replanifier ; éviter doublons ; traiter absence prolongée sans culpabiliser ; afficher dûes et règle.

## 9. Règles d'architecture

Planification Domain avec horloge abstraite ; source immuable ; persistance Infrastructure ; UI ne choisit pas arbitrairement le score.

## 10. Règles de sécurité

Cartes personnelles locales, pas de contenu caché prématuré, pas de manipulation addictive, entrées assainies et aucune télémétrie externe.

## 11. Tests à écrire

Tous intervalles, réussite/échec, solution consultée, doublon, fuseau/changement de jour, retard de deux semaines, réponse concurrente, source supprimée/versionnée, horloge déterministe.

## 12. Tests manuels à effectuer

Simuler jours/retards, répondre cartes, vérifier échéances/explications, erreurs personnelles et comportement après absence.

## 13. Commandes de vérification

```powershell
dotnet test --no-build --filter "Category=ReviewScheduling"
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## 14. Critères d'acceptation

Calendrier transparent/déterministe, génération idempotente, retards traités honnêtement, tests horloge verts et aucun examen anticipé.

## 15. Conditions d'arrêt

Date dépendante de l'horloge système non injectable, carte cachée exposée, doublons, comportement culpabilisant ou test temporel flaky.

## 16. Mise à jour attendue de la roadmap

Cocher `07B` seulement ; prochaine fiche `07C`.

## 17. Format obligatoire du rapport final

Politique ; modèle ; sources ; cas temporels ; tests/commandes ; parcours manuel ; limites ; confirmation d'absence d'examens/dashboard complet.
