# Explication

Calculer un ETag, c'est fabriquer une empreinte qui obéit à deux exigences opposées en apparence :
stable — la même représentation donne toujours la même empreinte — et sensible — le moindre
changement de contenu la change. Un condensat cryptographique satisfait les deux par
construction, et l'exercice fait construire l'ETag fort qui en découle.

La stabilité et la sensibilité viennent gratuitement du SHA-256 : la même suite d'octets produit
toujours le même condensat, et deux suites différentes en produisent, en pratique, deux
différents. C'est exactement ce qu'exige un ETag — reconnaître l'identité d'un état et détecter
son changement — et c'est pourquoi le condensat est l'outil naturel. Le contrat pose une frontière
importante : la méthode condense les octets *reçus tels quels*. La canonisation — trier les clés,
normaliser les espaces pour que deux sérialisations d'un même objet coïncident — est de la
responsabilité de l'appelant, en amont. Mélanger les deux rôles rendrait la fonction imprévisible ;
la séparer garde chaque responsabilité testable isolément, et c'est la bonne conception : une
fonction qui condense fidèlement, une autre qui canonise.

Les trois gestes de la mise en forme portent chacun leur piège. Condenser les *octets UTF-8*, pas
la chaîne .NET : le condensat travaille sur des octets, et passer par l'encodage explicite évite
toute ambiguïté de représentation interne. Produire l'hexadécimal en *minuscules* : majuscules et
minuscules encodent le même condensat mais donnent deux textes différents, donc deux ETag qui ne
coïncideraient pas entre un serveur qui majuscule et un client qui minuscule — la stabilité exige
une casse fixée, et le cas caché qui l'éprouve compare deux calculs. Encadrer de *guillemets
doubles* : la syntaxe de l'ETag l'exige, et un ETag sans guillemets ou à guillemets simples est
malformé — un intermédiaire pourrait le rejeter. L'absence du préfixe de faiblesse marque enfin
qu'il s'agit d'un ETag fort, celui qui autorise les écritures conditionnelles.

Fort ou faible n'est pas un détail : le contrat demande un ETag *fort* parce qu'il servira aux
comparaisons `If-Match` d'écriture, où l'on veut l'identité octet pour octet, pas la simple
équivalence sémantique d'un ETag faible.

La chaîne vide a un condensat parfaitement défini — celui de zéro octet — donc un ETag valide :
aucune raison de la traiter à part, et un cas la couvre.

Le coût est linéaire dans la longueur de la représentation, dominé par le condensat. La
transposition dépasse l'ETag : chaque fois qu'on a besoin d'une empreinte d'identité de contenu —
déduplication, détection de modification, clé de cache —, un condensat sur une forme canonique
répond, avec la même vigilance sur la stabilité de la mise en forme. L'empreinte instable, qui
change sans que le contenu change, est le défaut récurrent de toute cette famille.
