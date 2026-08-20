# Explication

Les sommes préfixes sont un pré-calcul : on paie une fois un parcours linéaire pour que toutes
les questions « quelle est la somme entre les positions i et j ? » deviennent ensuite une simple
soustraction de deux cases. Cet exercice n'en construit que la table, mais c'est la table qui
porte l'idée, et il faut la voir : `result[i]` contient la somme du préfixe qui se termine en
`i`, si bien que la somme d'une tranche s'obtient par `result[j] - result[i - 1]`. Un tableau
interrogé cent fois rembourse le pré-calcul dès la deuxième question ; c'est le même raisonnement
qui justifie un index de base de données ou un agrégat matérialisé, et c'est la transposition à
retenir.

La construction elle-même tient dans un accumulateur qui traverse la boucle. L'alternative —
recalculer chaque préfixe depuis le début, avec une boucle interne — rendrait le même tableau en
temps quadratique : sur mille éléments, un demi-million d'additions au lieu de mille. La
différence entre les deux versions n'est pas une optimisation de détail, c'est la présence ou
l'absence de l'idée d'accumulation, et les tailles des cas cachés suffisent à la sentir sans la
rendre bloquante.

Le choix le plus discutable de la solution est le mot clé `checked`, et il mérite sa défense. En
C#, l'addition d'entiers déborde silencieusement par défaut : la somme de valeurs toutes
positives peut devenir négative, et une table de sommes fausse ne prévient personne — elle
répondra faux à toutes les questions de tranche, longtemps après sa construction. `checked`
transforme ce mensonge différé en `OverflowException` immédiate, au moment et à l'endroit du
dépassement. Sur des cumuls — soldes, compteurs, tailles — c'est presque toujours le bon
réglage : une erreur franche vaut mieux qu'un résultat plausible et faux. Le coût d'exécution de
la vérification est négligeable devant l'addition elle-même.

Les bornes, enfin. Le tableau vide rend un tableau vide : l'allocation de taille zéro et la
boucle qui ne tourne pas produisent la bonne réponse sans garde spéciale. Les valeurs négatives
ne demandent rien non plus — l'accumulateur les absorbe, et une somme préfixe peut décroître.
Les cas cachés font varier tailles et signes, et réfutent la sortie recopiée de l'exemple ; le
contrat ajoute la non-mutation de l'entrée, que la solution respecte en écrivant uniquement dans
le tableau résultat, jamais dans `values`.

Ce qui doit rester après l'exercice : reconnaître les questions répétées sur des tranches, et
savoir répondre « pré-calcul linéaire, requêtes en temps constant » — avec le réflexe `checked`
partout où une somme grandit sans borne connue.
