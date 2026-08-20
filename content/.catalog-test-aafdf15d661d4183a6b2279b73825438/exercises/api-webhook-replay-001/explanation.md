# Explication

Ce contrôle est le second verrou d'un point de réception de webhooks, et il protège contre une
menace que la signature laisse entière : le rejeu. Un envoi authentique — signature parfaite,
corps intact — capturé dans un journal ou par un intermédiaire, peut être renvoyé tel quel et
resterait valide indéfiniment si la seule signature comptait. La signature dit *qui* et garantit
l'intégrité ; elle ne dit pas *quand*. L'horodatage, signé donc infalsifiable, comble ce trou.

Le mécanisme est un écart comparé à une tolérance, mais deux subtilités le rendent correct. La
première est la *symétrie*. On pourrait croire qu'il suffit de rejeter le passé — un envoi trop
vieux. C'est oublier la dérive d'horloge : l'émetteur peut avoir une horloge légèrement en avance,
et son envoi porter un horodatage dans un futur proche du point de vue du récepteur. Le rejeter
comme « impossible » casserait des envois parfaitement légitimes. La fenêtre couvre donc les deux
sens autour du présent — d'où la valeur absolue de l'écart —, exactement comme la tolérance
d'horloge des jetons acceptait un écart de part et d'autre. Le cas caché de l'envoi en avance
vérifie cette symétrie.

La seconde subtilité est le dimensionnement, implicite mais décisif : la fenêtre doit être
*étroite*. Trop large, elle rouvre le rejeu qu'elle devait fermer — un attaquant a tout le temps
de capturer et renvoyer. Trop étroite, elle rejette des envois légitimes que le réseau a retardés.
Quelques minutes est le compromis usuel : assez pour absorber la latence et la dérive, trop peu
pour laisser le temps d'un rejeu à froid. La méthode reçoit la tolérance en paramètre justement
pour que ce compromis reste une décision de déploiement, pas une constante enfouie.

La borne est inclusive — à un écart exactement égal à la tolérance, l'envoi passe — et la
tolérance nulle est valide : elle n'accepte que l'instant exact, réglage extrême mais cohérent.
Seule une tolérance *négative* est une faute d'appel, car elle ne décrirait aucune fenêtre. Le
cas de la tolérance nulle et celui de la borne exacte fixent ensemble le comportement au bord.

L'arithmétique en 64 bits est la précaution des calculs d'instants : la différence de deux
horodatages éloignés peut sortir du domaine d'un entier de 32 bits, et un écart enroulé fausserait
la comparaison.

Le coût est constant. La transposition est le principe « signature plus fraîcheur » : une preuve
d'origine ne suffit jamais contre le rejeu, il faut y adjoindre une borne temporelle — jeton avec
échéance, webhook avec horodatage, requête signée avec fenêtre. Et le renfort ultime, hors de
cette fonction pure, est l'identifiant d'envoi unique mémorisé le temps de la fenêtre, qui ferme
même le rejeu immédiat.
