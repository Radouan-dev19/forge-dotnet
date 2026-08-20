# Explication

Vérifier un défi PKCE, c'est jouer le rôle du guichet à l'instant décisif du flux : celui où le
détenteur du code prouve qu'il est aussi le demandeur de l'aller. La méthode assemble trois
briques déjà connues — validation d'entrée, condensat, encodage urlisé — et sa valeur est dans
la précision de chacune, car la moindre approximation rend toutes les vérifications fausses.

La validation du secret d'abord, et ses bornes ne sont pas décoratives. La norme impose de 43 à
128 caractères d'un alphabet restreint — lettres, chiffres et quatre signes — et la borne basse
porte le sens : 43 caractères de cet alphabet, c'est l'entropie minimale pour qu'une preuve de
possession résiste à l'énumération. Un guichet qui accepterait un secret de cinq caractères
accepterait une preuve devinable, et tout l'édifice — le code intercepté inutilisable —
s'effondrerait par le bas. Les bornes se testent aux frontières exactes, 43 et 128 inclus, avec
leurs voisines extérieures : la mécanique habituelle, appliquée à une exigence de norme.

Le calcul ensuite, en trois gestes dont l'ordre et les détails sont contractuels. Le condensat
se prend sur les octets *ASCII* du secret — l'alphabet restreint le garantit sans perte, et
c'est ce que la norme spécifie ; condenser une autre représentation produit d'autres octets,
donc une autre empreinte. L'encodage est le Base64 *urlisé sans remplissage* : la traduction des
deux caractères et le retrait des `=` finaux font partie du format — une empreinte à laquelle il
reste son remplissage ne correspondra jamais à celle que le client a envoyée, et le cas caché
qui en porte un le vérifie. Ce trio condensat-encodage-nettoyage est exactement celui des
segments de jetons de la semaine quatorze ; le retrouver ici montre que le vocabulaire est le
même d'un bout à l'autre du domaine.

La comparaison enfin, en temps constant sur les octets. L'argument est celui de toutes les
comparaisons de secrets : une égalité qui s'arrête à la première différence laisse fuir, par sa
durée, la longueur du préfixe correct. Ici s'ajoute une nuance de rôle : c'est le *guichet* qui
compare, face à un client potentiellement hostile qui peut mesurer — le réflexe n'est pas
académique.

Le régime d'erreur est le refus calme : un vérificateur de frontière trie ce que le réseau
apporte, il ne s'étonne pas. Toute anomalie — absence, bornes, alphabet, empreinte vide — rend
faux ; seul le chemin complet jusqu'à la comparaison peut rendre vrai.

Le coût est linéaire, dominé par le condensat. La transposition dépasse PKCE : toute preuve de
possession par empreinte — jetons d'inscription, liens signés, défis d'appairage — repose sur ce
même triptyque : entropie minimale imposée, calcul au format exact, comparaison en temps
constant. Savoir le dérouler une fois, c'est savoir le relire partout.
