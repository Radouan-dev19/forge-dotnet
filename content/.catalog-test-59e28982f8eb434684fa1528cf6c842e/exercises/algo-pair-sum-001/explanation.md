# Explication

La version naïve de ce problème est une double boucle : pour chaque valeur, parcourir le reste du
tableau à la recherche du complément. Elle est correcte, quadratique, et surtout elle rate
l'occasion d'apprendre le geste central de l'exercice : transformer une question sur des *paires*
en une question sur un *élément* — « ai-je déjà vu le complément de la valeur courante ? » — que
l'ensemble `seen` répond en temps constant. Un seul parcours suffit alors, et le coût tombe à
linéaire, au prix d'un ensemble qui grandit avec l'entrée. Cet échange espace-contre-temps est
l'un des plus rentables du métier, et le mémoriser sous cette forme minimale le rend disponible
partout : détection de doublons, jointures en mémoire, caches de calculs.

L'ordre des deux opérations dans la boucle n'est pas une élégance, c'est la correction même. On
interroge `seen` *avant* d'y ajouter la valeur courante ; ainsi, le complément trouvé provient
nécessairement d'une position antérieure, donc distincte. Inverser les deux lignes fait accepter
une paire fantôme : pour une cible de dix-huit et une seule case valant neuf, la valeur se
trouverait elle-même comme complément. C'est exactement le genre de faute qu'un exemple nominal
ne montre jamais, et le cas caché qui place une moitié de la cible en un seul exemplaire est là
pour la réfuter. Notez que deux exemplaires de neuf, eux, doivent répondre vrai — deux positions
distinctes existent alors, et l'insertion après l'interrogation rend les deux verdicts corrects
sans aucun code spécial pour les doublons.

Que rend la fonction, et que ne rend-elle pas ? Un booléen d'existence, pas les positions. C'est
un choix de contrat qui simplifie tout : dès qu'il faut les indices, l'ensemble devient un
dictionnaire valeur-vers-position, même squelette, même ordre d'opérations. Savoir énoncer cette
gradation — existence, puis position, puis toutes les positions — évite de sur-construire.

Sur les bornes : un tableau vide ou d'un seul élément ne peut porter aucune paire et rend faux
par simple épuisement de la boucle, sans garde dédiée — le code le plus court qui soit correct
est celui qui n'ajoute pas de cas spéciaux inutiles. Les compléments négatifs fonctionnent sans
rien faire de plus, la soustraction `target - value` étant définie sur tout le domaine.

La transposition à retenir dépasse les tableaux : chaque fois qu'une double boucle compare tout à
tout, demandez-vous quelle information un premier passage pourrait mémoriser pour que le second
devienne une simple interrogation. La réponse tient souvent, comme ici, en un ensemble.
