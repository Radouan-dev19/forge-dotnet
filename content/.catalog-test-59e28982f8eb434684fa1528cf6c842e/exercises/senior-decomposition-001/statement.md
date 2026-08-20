# Decouper ou non : la decision argumentee

Implementez `Submission.DecompositionAdvice` avec la signature fournie. La fonction recoit trois
mesures d'un systeme et rend la decision de conception : extraire un service, ou garder le monolithe.

## Les entrees

- `teams` : le nombre d'equipes qui possedent le code concerne.
- `deploysCoupled` : le nombre de deploiements qui doivent sortir ensemble avec ce code.
- `sharedTables` : le nombre de tables de base de donnees partagees avec le reste du systeme.

## La regle

La position par defaut est **garder le monolithe**. L'extraction d'un service coute cher : un
deployable de plus, un appel reseau la ou il y avait un appel de methode, une panne partielle
nouvelle. On ne la paie que si une frontiere reelle la justifie.

Appliquez les regles dans cet ordre :

1. Si `teams` est inferieur a un, ou si `deploysCoupled` ou `sharedTables` est negatif, levez
   `ArgumentOutOfRangeException` : les compteurs sont incoherents.
2. Si `teams` vaut au plus un, rendez `keep-monolith` : une seule equipe ne gagne rien a se distribuer.
3. Si `sharedTables` est strictement positif, rendez `keep-monolith` : des donnees partagees trahissent
   une frontiere mal placee, et extraire creerait un couplage cache pire que le couplage visible.
4. Si `deploysCoupled` vaut zero, rendez `extract-service` : plusieurs equipes et un deploiement
   independant designent une vraie frontiere.
5. Sinon, rendez `keep-monolith` : tant que les deploiements restent couples, extraire ne fait que
   deplacer le probleme.

## Avant d'ecrire

Predisez la decision pour une equipe seule, pour trois equipes sans couplage, et pour trois equipes
partageant deux tables. Formulez la phrase qui refuse le decoupage a un interlocuteur presse.
