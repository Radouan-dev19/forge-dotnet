# Explication

Valider un sujet de commit : non vide, borné à soixante-douze caractères, sans point final. Le
prédicat est simple ; les trois règles qu'il encode viennent de l'*usage outillé* de
l'historique, et c'est cet usage qu'il faut comprendre pour les défendre en équipe.

La borne de longueur d'abord, et l'énoncé demande ce qu'un sujet trop long devient dans une
liste : une phrase coupée. Les vues d'historique — le journal en une ligne, les interfaces
web, les courriels d'intégration — affichent le sujet dans une largeur fixe, et tronquent
au-delà : « Corrige le calcul des frais de livraison pour les commandes internationales
avec... » — l'information décisive était dans la partie coupée. La convention des
soixante-douze caractères vient de là : un sujet est un *titre*, fait pour être balayé dans
une liste de cinquante, pas un paragraphe. Le détail va dans le corps du message, séparé par
une ligne vide, où la place est illimitée.

Le point final proscrit relève de la même logique typographique : un titre ne prend pas de
point, et sa présence dans une liste de sujets crée du bruit visuel sans porter d'information.
La règle est cosmétique en apparence ; son respect uniforme est ce qui rend un historique
*lisible comme un journal* — et un historique lisible est un outil de diagnostic : la
recherche de la modification fautive se fait en balayant des sujets, à deux heures du matin,
et chaque sujet clair économise une ouverture de diff.

Le prédicat applique les règles sur le sujet *rogné* : les blancs de bordure — artefacts de
copier-coller — ne comptent ni dans la longueur ni pour le point final, et le sujet tout blanc
tombe dans le refus initial. Les frontières sont celles que l'énoncé fait écrire :
soixante-douze exactement passe — la borne est incluse —, soixante-treize échoue, le point
final échoue, le blanc échoue. Le cas caché à la limite exacte départage `<=` de `<`, la
mécanique habituelle.

Ce que le prédicat ne vérifie pas mérite d'être dit : le *contenu* — le mode impératif, la
présence d'un contexte, la référence au ticket — relève du jugement humain ou de conventions
plus riches ; cette fonction est le socle mécanique qu'un crochet de dépôt peut appliquer sans
faux positifs.

Le coût est constant. La transposition est le principe des *conventions outillées* : une
convention d'équipe qui n'est vérifiée par personne s'érode ; la transformer en prédicat pur,
brancher le prédicat dans un crochet ou la chaîne d'intégration, et l'historique reste propre
sans qu'aucun humain ne joue au gendarme. La règle en une phrase : ce qui peut être vérifié
mécaniquement doit l'être — les humains gardent les vérifications qui demandent du jugement.
