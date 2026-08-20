# Explication

Une quantité de commande à valider, trois verdicts possibles : accepté, absent, hors plage.
L'exercice paraît redire les classements par gardes — et sa nuance propre est ailleurs, dans la
*taxonomie des refus* : deux façons d'être invalide qui ne se corrigent pas de la même manière,
donc deux verdicts distincts.

L'énoncé demande de nommer ce qu'un message fondu ferait perdre : le geste de correction. Un
client qui reçoit « quantité invalide » ne sait pas s'il a *oublié* le champ ou s'il a demandé
*trop*. Le verdict `required` dit « le champ manque » — la correction est de le renseigner ;
le verdict `range` dit « la valeur sort des bornes » — la correction est de la ramener entre
un et cent. Dans une réponse d'erreur normalisée, ces deux verdicts deviennent deux codes que
le client affiche différemment — champ surligné contre message de plage — et fondre les deux
appauvrit chaque formulaire construit sur l'API. La granularité des erreurs de validation est
un service rendu, pas un luxe.

La convention d'absence mérite son paragraphe critique : zéro *représente* « non renseigné »
dans ce contrat — une sentinelle dans le domaine des entiers, choisie parce qu'une quantité
nulle n'a pas de sens commercial dans cette commande. C'est défendable ici et l'énoncé
l'assume ; la version plus riche utiliserait un type optionnel pour distinguer « absent » de
« zéro saisi », et le jour où zéro devient une valeur légitime — ligne annulée — la sentinelle
explose. Reconnaître une sentinelle, savoir ce qu'elle interdit, et la remplacer par un
optionnel quand le domaine grandit : la trajectoire est la même que pour le nœud absent des
structures.

L'ordre des gardes découle de la taxonomie : l'absence d'abord — zéro est happé avant le test
de plage, sinon il tomberait dans `range` par la borne basse, verdict faux —, la plage
ensuite, l'acceptation en reste. Les bornes sont un et cent inclus : les cas cachés se posent
sur les quatre points sensibles — un et cent qui passent, zéro qui rend `required`, le négatif
et le cent-un qui rendent `range` — exactement la liste que l'énoncé fait écrire avant le
code.

Le coût est constant. La transposition est la charpente de toute validation de champ : pour
chaque règle, un code de verdict distinct — absent, plage, format, référence inconnue — et
l'ordre des tests choisi pour que les sentinelles ne fuient pas dans les mauvaises catégories.
L'exercice d'agrégation voisin assemble ces verdicts par champ ; celui-ci apprend à ne pas les
fondre au niveau du champ lui-même.
