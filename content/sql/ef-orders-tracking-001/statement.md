# Objectif observable

Expliquer et mesurer la différence entre une requête suivie et `AsNoTracking`.

## Travail demandé

Charge deux fois la commande `1` avec le même `DbContext`, puis effectue une troisième lecture explicitement destinée à l’affichage. Retourne trois observations : identité des deux instances suivies, état de l’entité en lecture seule et nombre d’entrées suivies.

Le résultat attendu est `true`, `true`, `1`. Aucun appel à `SaveChanges` n’est nécessaire. Le starter relit seulement une entité suivie et prétend qu’elle est détachée : le test négatif doit le révéler.

## Point d’attention

`AsNoTracking` réduit le coût du suivi pour une lecture pure, mais il ne doit pas être appliqué mécaniquement lorsqu’une modification suivie est prévue. La preuve porte sur `ChangeTracker`, pas sur une intuition de performance.
