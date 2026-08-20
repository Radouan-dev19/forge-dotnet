# Explication

Valider la *forme* d'un en-tête d'autorisation : le schéma attendu, suivi d'une preuve non
vide. La fonction rend un verdict booléen, et ce choix de sortie est la première décision de
sécurité de l'exercice — avant même la validation elle-même.

Pourquoi un verdict et jamais la valeur ? Parce qu'une preuve d'identité qui circule finit dans
un journal. Une fonction qui extrairait et retournerait le jeton « pour faciliter la suite »
le ferait transiter par toutes les couches appelantes, leurs traces de diagnostic, leurs
messages d'erreur — et un jeton journalisé est un jeton volé qui s'ignore. La discipline est
structurelle : la couche qui vérifie ne fait pas fuir ce qu'elle vérifie, et les couches qui
ont besoin de la valeur la lisent elles-mêmes, au plus près de l'usage. Le contrat de
l'exercice l'impose, et son harnais n'attend que vrai ou faux.

La validation, ensuite, tient en trois contrôles ordonnés. L'absence — en-tête nul, vide ou
blanc — répond faux calmement : un client anonyme est un cas normal du protocole, pas un
incident. Le schéma se compare *sans casse* — la norme des en-têtes d'autorisation déclare le
schéma insensible à la casse, et `bearer` minuscule est légitime : le cas caché posé dessus
réfute la comparaison stricte, l'erreur qui rejetterait des clients conformes. Enfin la
présence d'une preuve : le schéma seul, ou suivi de blancs, ne prouve rien — `Bearer ` n'est
pas une autorisation, c'est un début de phrase. Le contrôle de longueur puis de contenu
derrière le préfixe ferme ces deux trous, que les cas cachés éprouvent séparément.

Ce que la fonction ne fait *pas* est aussi son contrat : elle ne valide pas la preuve
elle-même — signature, expiration, émetteur relèvent de la chaîne cryptographique, traitée par
les exercices de jetons de la même semaine. Cette validation-ci est syntaxique, un premier
filtre bon marché avant les vérifications coûteuses — et confondre les deux niveaux, croire
qu'un en-tête bien formé est un client authentifié, est l'erreur conceptuelle que la dernière
erreur fréquente de l'exercice nomme.

Le coût est linéaire dans la longueur de l'en-tête. La transposition est le patron des
vérificateurs de frontière : un verdict qui ne transporte pas le secret, des refus calmes pour
les formes anormales, la tolérance de casse là où la norme la prescrit — et la conscience
nette de ce que ce filtre prouve et ne prouve pas, écrite dans son nom et sa documentation.
