# Explication

Convertir un numéro de page en nombre d'éléments à sauter : une multiplication et une
soustraction, et le hors-par-un le plus fréquent des API de listes. L'exercice fige la
convention et son arithmétique.

La convention d'abord : les pages se comptent *depuis un* — c'est le vocabulaire des humains et
des interfaces — tandis que le décalage se compte *depuis zéro* — c'est le vocabulaire des
requêtes. La formule `(page - 1) * pageSize` est le pont entre les deux, et le moins un est
exactement ce que l'énoncé demande de nommer : sans lui, la page un sauterait une page entière,
et les *premiers* éléments du jeu de résultats — les plus récents, les plus pertinents, ceux
que tout le monde regarde — disparaîtraient de toutes les listes, remplacés par la deuxième
tranche. Ce défaut a une signature reconnaissable en production : « il me manque toujours les
vingt premiers ». Le cas de l'exemple — page un, décalage zéro — est le verrou, et le cas caché
de la page deux vérifie la pente.

La validation reprend les invariants du domaine de pagination : un numéro de page nul ou
négatif ne désigne rien — la page zéro est l'ambiguïté classique entre les deux conventions de
comptage, et la refuser tranche net —, et la taille respecte les bornes publiques, de un au
plafond de cent, le même que l'exercice voisin de bornage applique. Valider ici *aussi*, alors
qu'un appelant bien élevé aurait déjà borné, est la défense en profondeur des frontières
internes : cette fonction produit un paramètre de requête, et un paramètre de requête faux
coûte cher en aval.

Le `checked` sur le produit ferme la dernière porte : page et taille valides pris séparément
peuvent produire un décalage qui déborde l'entier — une page de plusieurs dizaines de millions
suffit — et le débordement silencieux donnerait un décalage négatif, donc une requête qui
échoue ou, pire, qui repart du début. L'exception franche vaut mieux que la page fantôme.

Les cas suivent l'énoncé : première page à zéro, page intermédiaire au produit attendu, refus
du numéro nul et de la taille hors plage.

Le coût est constant. La transposition est la paire de conventions elle-même : chaque frontière
entre un monde compté-depuis-un et un monde compté-depuis-zéro — pages et décalages, numéros de
ligne et indices, rangs et positions — mérite une fonction de conversion *unique*, validée et
testée sur ses deux premiers points. Les hors-par-un ne se corrigent pas en relisant plus
fort ; ils se corrigent en donnant un domicile unique à la conversion.
