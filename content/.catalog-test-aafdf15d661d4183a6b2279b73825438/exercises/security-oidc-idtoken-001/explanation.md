# Explication

Ces trois contrôles sont ce qui sépare « un jeton valide » de « un jeton valide *pour moi, ici,
maintenant* » — et c'est exactement l'écart qu'exploitent les attaques par substitution et par
rejeu. La chaîne de la semaine quatorze a prouvé l'authenticité ; celle-ci prouve la
*destination*, et chaque revendication lie le jeton à un objet différent.

Le `nonce` le lie à *la demande*. Le client l'a tiré au sort à l'aller, le guichet l'a gravé
dans le jeton signé : un jeton d'identité volé dans un autre flux porte le nonce d'une autre
demande et échoue ici, quelle que soit sa fraîcheur. L'`azp` le lie au *client* : la partie
autorisée nomme l'application pour laquelle le jeton a été émis, et un jeton d'identité destiné
à une autre application du même guichet — signature identique, émetteur identique — échoue là.
L'`at_hash` le lie au *jeton d'accès reçu ensemble* : sans lui, un intermédiaire pourrait
assembler l'identité d'un flux et l'accès d'un autre ; l'empreinte scelle le couple.

Le calcul de cette empreinte est le seul fragment cryptographique, et son détail piège :
c'est la *moitié gauche* du condensat — seize octets sur trente-deux — encodée en Base64Url
sans remplissage. La troncature vient de la norme, qui proportionne l'empreinte à l'algorithme
de signature du jeton d'identité ; l'oublier produit une empreinte de quarante-trois caractères
là où vingt-deux sont attendus, et le cas caché construit précisément ce jeton-là — plausible,
signé, faux d'un seul détail de longueur. Le condensat se prend sur les octets ASCII du jeton
d'accès *encodé*, tel qu'il circule : c'est la chaîne elle-même qui est scellée, pas son
contenu.

L'ordre des contrôles est contractuel, comme dans la chaîne de la semaine quatorze : le verdict
nomme le premier échec, et un jeton doublement faux — nonce et partie autorisée divergents —
rend le verdict du nonce, ce qu'un cas caché fige. Le verdict de forme précède tout : une
charge utile indécodable n'a pas de revendications à comparer. Les revendications absentes
rendent le verdict de leur contrôle — un jeton d'accès présenté à la place d'un jeton
d'identité échoue typiquement dès le nonce, qu'il ne porte pas, et c'est exactement le refus
que le laboratoire de la semaine met en scène.

Les comparaisons sont ordinales et sensibles à la casse : nonce et identifiants de client sont
des valeurs exactes tirées ou enregistrées par des machines, et toute tolérance élargirait
l'espace atteignable par un forgeur.

Le coût est linéaire, dominé par le décodage et le condensat. La transposition est la question
à poser devant toute preuve signée : au-delà de « est-elle authentique ? », demander « est-elle
*pour moi*, en réponse à *ma* demande, et liée à *ce qui l'accompagne* ? » — trois liens, trois
revendications, et l'ordre d'échec écrit dans le contrat.
