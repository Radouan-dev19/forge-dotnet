# Explication

Échantillonner, c'est accepter de ne pas tout savoir pour pouvoir savoir quelque chose. Un service
qui trace chaque requête paie sa télémétrie plus cher que son propre travail ; un service qui ne
trace rien est aveugle. La fraction est le compromis, et cet exercice en écrit la décision.

**La règle d'erreur passe avant le calcul, et ce n'est pas un détail de style.** Une trace ordinaire
sert à mesurer : mille traces valent autant que dix mille pour estimer une latence médiane. Une trace
en erreur sert à comprendre : elle est unique, et si l'échantillonnage la jette, l'incident devient
inexplicable. Placer ce test en tête n'est pas une optimisation, c'est l'énoncé de la priorité.

**Le reste d'une division entière prend le signe du dividende.** Une empreinte négative donne donc un
reste négatif, et un reste négatif est toujours inférieur au taux : sans correction, toutes les
traces d'empreinte négative seraient conservées. Le défaut ne se voit pas sur des jeux d'essai
positifs, il se voit en production, sous la forme d'un volume de traces bien supérieur au taux
demandé. Ramener le reste dans les valeurs positives coûte une addition et un second modulo.

**La comparaison est stricte.** Les seaux vont de zéro à quatre-vingt-dix-neuf : un taux de dix
retient les seaux zéro à neuf, soit exactement dix pour cent. Une borne large en retiendrait onze.
L'écart paraît négligeable et ne l'est pas quand le taux vaut un : on doublerait le volume.

**La décision est reproductible, et c'est ce qui la rend utile.** Tirer un nombre au hasard
donnerait le bon volume mais une trace incohérente : le service A garderait un segment que le service
B aurait jeté, et la trace distribuée serait trouée. En dérivant la décision de l'empreinte de
l'identifiant, tous les services traversés prennent la même, sans se parler.

La décision est en temps constant, et c'est une exigence : elle s'exécute sur le chemin de chaque
requête. Une décision d'échantillonnage coûteuse coûterait plus que la trace qu'elle évite.

**Ce que l'exercice ne traite pas, et qu'il faut savoir nommer.** L'échantillonnage écrit ici est
dit « de tête » : la décision est prise à l'entrée, avant de savoir comment la requête va finir. Elle
est donc bon marché — aucune trace n'est mise en mémoire tampon — mais elle décide en aveugle : une
requête lente, qui aurait mérité d'être conservée, sera jetée si son seau tombe hors de la fraction.
L'alternative, l'échantillonnage « de queue », attend la fin de la requête pour décider en fonction
de sa latence et de son issue. Elle conserve exactement ce qui intéresse, au prix d'un tampon qui
retient toutes les traces en cours et d'un point de collecte centralisé. Les deux approches
coexistent en production ; ce qui compte est de savoir laquelle on a déployée et ce qu'elle rend
aveugle.

**Un dernier piège, qui ne se voit qu'à l'échelle.** Le taux ne se règle pas une fois pour toutes. Un
service qui traite dix requêtes par seconde et un service qui en traite dix mille n'ont pas besoin de
la même fraction pour obtenir la même confiance statistique. Fixer le même pourcentage partout donne
soit trop peu de traces sur les services calmes, soit une facture de télémétrie disproportionnée sur
les services chargés. Le taux appartient donc à la configuration, jamais au code — ce que l'exercice
matérialise en le recevant en paramètre plutôt qu'en le lisant dans une constante.
