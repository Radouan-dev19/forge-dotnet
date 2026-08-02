# Objectif observable

Écrire un prédicat sargable et justifier l’index utilisé par une propriété stable du plan.

Compte les commandes du client `777` créées à partir du `2026-01-14`. Le résultat attendu est `10`. Le dataset contient 20 000 lignes et un index composite `(CustomerId, CreatedAtUtc)` incluant `Total`.

Le test contrôle le résultat, l’existence et la définition de l’index, puis recherche son nom dans le plan XML. Il ne compare ni coût estimé, ni durée, ni numéro de nœud. Ces valeurs varient selon version et machine.

Conserve la colonne indexée nue dans le prédicat. Une fonction appliquée à `CreatedAtUtc` pourrait empêcher une recherche efficace.
