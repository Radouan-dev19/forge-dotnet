# Objectif observable

Détecter une écriture concurrente optimiste avec `rowversion` et `DbUpdateConcurrencyException`.

## Travail demandé

Deux contextes chargent la commande `1`. Le premier augmente son total et sauvegarde. Le second tente ensuite une autre augmentation depuis sa version périmée. Retourne `true` uniquement si le conflit est effectivement détecté.

Le test utilise deux connexions réelles. Une simple comparaison de valeurs en mémoire ne constitue pas une preuve. Après le scénario, `reset.sql` doit restaurer `120.50`.

## Limite volontaire

Ce scénario couvre la détection basique. Une stratégie métier de fusion ou de nouvelle tentative appartient à une tranche ultérieure ; écraser silencieusement la valeur est interdit.
