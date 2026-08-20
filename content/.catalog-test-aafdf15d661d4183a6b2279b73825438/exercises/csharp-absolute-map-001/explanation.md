# Explication

Cet exercice est la forme la plus pure de la *projection* : produire une collection neuve en
appliquant la même transformation à chaque élément, sans toucher la source. Le vocabulaire
importe, car il structure ensuite tout LINQ — `Select` est exactement cela — et une partie du SQL.
Reconnaître qu'un problème est une projection, c'est déjà savoir que la sortie a la même longueur
que l'entrée, que chaque case se calcule indépendamment des autres, et qu'aucun état ne traverse
la boucle. Trois propriétés qui rendent le code trivial à tester et à paralléliser.

Le choix d'outillage mérite d'être défendu. `Array.ConvertAll` fait précisément le travail :
il alloue le tableau de sortie à la bonne taille et applique la fonction case par case. Une
boucle `for` écrite à la main serait équivalente et acceptable ; la version LINQ
`values.Select(Math.Abs).ToArray()` aussi, avec une allocation intermédiaire de plus. Ce que
toutes les bonnes versions partagent, c'est le tableau *neuf*. La version fautive — écrire
`values[i] = Math.Abs(values[i])` puis retourner `values` — rend la bonne valeur et viole le
contrat : l'appelant qui relit son tableau après l'appel le trouve modifié. Le harnais compare
les arguments avant et après sur des cas dédiés, si bien que cette mutation, invisible dans la
valeur de retour, échoue quand même. C'est la leçon durable de l'exercice : « ne pas muter »
n'est pas un conseil de style, c'est une clause testable du contrat, et les fonctions qui la
respectent se composent sans surprise.

Sur les bornes, la valeur absolue cache un piège d'une profondeur inattendue : le minimum d'un
entier signé n'a pas d'opposé représentable, et `Math.Abs(int.MinValue)` lève une
`OverflowException` plutôt que de rendre un nombre négatif absurde. Les cas de cet exercice
restent dans le domaine sûr, mais savoir que la fonction la plus innocente de la bibliothèque
peut lever change la façon dont on lit du code de calcul. Zéro, lui, est son propre absolu — le
cas caché qui le contient vérifie qu'aucune condition maladroite ne le traite comme un négatif.

Le coût est linéaire en temps et en espace, incompressible pour une projection qui doit produire
toutes les cases. La transposition est immédiate et quotidienne : normaliser des montants,
convertir des unités, masquer des champs — chaque fois, la même question ouvre l'analyse :
« est-ce une projection pure, ou un état circule-t-il entre les éléments ? ». Si la réponse est
« pure », le code s'écrit en une ligne et se teste par table ; sinon, c'est une réduction ou une
machine à états, et d'autres exercices s'en occupent.
