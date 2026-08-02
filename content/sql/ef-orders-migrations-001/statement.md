# Objectif observable

Appliquer une migration EF Core versionnée sur une base vide et prouver qu’elle est idempotente.

## Contexte

Le mini‑ERP doit créer `Customers`, `Orders`, leur relation et les index utiles. Le compte fourni est limité à cette base jetable et possède uniquement les droits DDL nécessaires au scénario.

## Travail demandé

1. Configure `MiniErpContext` sans chaîne de connexion codée en dur.
2. Applique les migrations avec l’API EF Core dédiée.
3. Réexécute l’opération : aucune table ni migration ne doit être dupliquée.
4. Vérifie l’historique et les contraintes, pas seulement l’existence d’une table.

Le starter utilise `EnsureCreated`, qui crée éventuellement le modèle mais ne démontre pas un historique de migrations. La solution doit rester annulable par `reset.sql`.

## Critères visibles

- une migration `202607290001_InitialMiniErp` dans l’historique ;
- deux tables métier, une clé étrangère et les deux index attendus ;
- aucun secret ou nom de serveur dans le code de contenu.
