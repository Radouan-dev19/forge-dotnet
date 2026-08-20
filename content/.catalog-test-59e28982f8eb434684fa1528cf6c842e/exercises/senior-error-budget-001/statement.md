# Geler ou livrer selon le budget d'erreur

Implementez `Submission.BudgetDecision` avec la signature fournie. La fonction decide, a partir d'un
volume de requetes, du nombre d'echecs observes et d'un objectif de niveau de service, s'il faut
**geler** les livraisons ou continuer a **livrer**.

## Les entrees

- `totalRequests` : le nombre total de requetes sur la fenetre.
- `failedRequests` : le nombre de requetes en echec sur cette meme fenetre.
- `sloBasisPoints` : le taux de succes vise, exprime en points de base. `9990` signifie 99,90 pour
  cent de succes vise, donc 0,10 pour cent d'echecs tolere.

## La regle

Un objectif de service n'exige jamais zero panne : il fixe une part d'echecs acceptable, le **budget
d'erreur**. La tolerance, en points de base, vaut `10000 - sloBasisPoints`. Le nombre d'echecs
autorises est ce budget applique au volume, en division entiere :

```text
allowedFailures = totalRequests * (10000 - sloBasisPoints) / 10000
```

Le budget est **epuise** quand les echecs observes **depassent strictement** ce nombre : dans ce cas
la fonction rend `freeze`, pour arreter d'ajouter du risque tant que la fiabilite n'est pas retablie.
Sinon elle rend `ship`.

Validez d'abord les entrees, dans cet ordre : `totalRequests` inferieur a un, `failedRequests` hors
de l'intervalle `0..totalRequests`, ou `sloBasisPoints` hors de `0..10000` levent
`ArgumentOutOfRangeException`.

## Avant d'ecrire

Predisez la decision pour mille requetes avec cinq echecs a 99,90 pour cent, puis avec un seul echec,
et pour dix mille requetes avec exactement dix echecs. Dites ce que gele exactement un gel de
livraisons.
