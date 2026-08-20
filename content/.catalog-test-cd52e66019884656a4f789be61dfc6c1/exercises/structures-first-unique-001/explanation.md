# Explication

« Le premier caractère unique » assemble deux exigences qui tirent dans des directions
opposées : *unique* est une propriété globale — il faut avoir vu toute la chaîne pour l'affirmer
— tandis que *premier* est une propriété d'ordre local. La solution en deux passes est la
réponse canonique à cette tension, et c'est elle que l'exercice installe.

La première passe construit la connaissance globale : un dictionnaire de fréquences, caractère
vers compte, rempli par le motif de cumul habituel. À la fin de cette passe, la question
« unique ? » a une réponse en temps constant pour n'importe quel caractère. La seconde passe
apporte l'ordre : elle relit la chaîne *dans sa séquence d'origine* et s'arrête au premier
caractère dont le compte vaut un. L'ordre du verdict vient donc du texte lui-même, jamais du
dictionnaire — et c'est le point qui fait échouer les implémentations séduisantes : parcourir le
dictionnaire à la recherche d'un compte à un rend *un* caractère unique, mais l'ordre
d'énumération d'un dictionnaire n'est pas contractuel, et rien ne garantit que ce soit le
premier du texte. Le cas caché où plusieurs caractères sont uniques départage les deux
approches : seul le plus tôt placé dans la chaîne est la bonne réponse.

La tentation de tout faire en une passe mérite son paragraphe, parce qu'elle revient à chaque
entretien : mémoriser « le premier unique vu jusqu'ici » en avançant ne marche pas, car un
caractère cru unique peut être invalidé mille positions plus loin — l'information qui décide est
devant, pas derrière. Les variantes à une passe existent — liste chaînée des candidats entretenue
au fil de l'eau — et coûtent en complexité de code ce qu'elles économisent en lectures ; sur une
chaîne en mémoire, les deux passes simples gagnent à tous les coups. Savoir dire *pourquoi* une
passe ne suffit pas vaut mieux que de connaître la variante savante.

Le cas sans réponse est une convention du contrat : la chaîne vide — celle du « rien » — plutôt
qu'une exception ou un caractère sentinelle. Elle couvre aussi l'entrée vide, qui traverse les
deux boucles sans tour. Le retour est une chaîne d'un caractère, `c.ToString()`, le type du
contrat.

Le coût : deux parcours linéaires, un dictionnaire dont la taille est bornée par l'alphabet
réellement utilisé — sur du texte, quelques dizaines d'entrées. La transposition dépasse les
chaînes : premier client sans doublon de commande, première référence à occurrence unique dans
un journal — chaque fois que « premier » rencontre une propriété globale, la réponse est
connaissance d'abord, ordre ensuite : compter, puis relire dans l'ordre.
