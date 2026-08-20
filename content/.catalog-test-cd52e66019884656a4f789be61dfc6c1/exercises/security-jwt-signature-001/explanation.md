# Explication

La vérification de signature est l'unique étape de toute la chaîne de validation qui distingue un
jeton authentique d'un jeton fabriqué. Tout ce qui précède — la forme — et tout ce qui suit — les
revendications — travaille sur du texte que n'importe qui peut produire. C'est pourquoi cet
exercice mérite d'être fait à la main une fois : il rend concret ce que le middleware exécute à
chaque requête, et ce qu'une mauvaise configuration désactiverait.

Le premier point dur est l'entrée exacte du calcul. Le condensat ne porte pas sur la charge utile
décodée, ni sur la charge utile seule, mais sur la concaténation des deux premiers segments *tels
qu'ils sont encodés*, point séparateur compris. Cette précision n'est pas un caprice de norme :
signer le texte encodé garantit que le moindre caractère modifié dans ce qui circule — y compris
dans l'en-tête — invalide la signature, sans qu'aucun décodage préalable ne soit nécessaire. Une
implémentation qui signe la charge utile seule laisse l'en-tête libre d'être réécrit, et on a vu
dans la leçon ce qu'un attaquant fait d'un en-tête libre.

Le deuxième point dur est le régime d'erreurs. Un vérificateur reçoit ce que le réseau lui apporte,
c'est-à-dire n'importe quoi : deux segments, une signature tronquée à la copie, des caractères hors
alphabet. Aucune de ces situations n'est exceptionnelle pour lui — elles sont son quotidien — et
toutes se traduisent par le même verdict : refus. Laisser remonter une exception de décodage
transformerait chaque jeton malformé en erreur serveur, offrant au passage un canal de sonde
gratuit. La règle est donc asymétrique et se retient bien : seul le chemin complet jusqu'à la
comparaison finale peut répondre vrai ; tous les autres chemins répondent faux, silencieusement.

Le troisième point dur est la comparaison elle-même. Deux tableaux d'octets se comparent
naturellement par une boucle qui s'arrête à la première différence — c'est ce que font `==` sur des
séquences et `SequenceEqual`. Or ce temps d'arrêt est mesurable : en soumettant des signatures
progressivement corrigées, un observateur patient déduit octet par octet le condensat attendu.
`CryptographicOperations.FixedTimeEquals` parcourt l'intégralité des deux tableaux quoi qu'il
arrive, précisément pour que la durée ne dise rien. Sur un HMAC complet l'attaque est peu
praticable, mais le réflexe doit être inconditionnel, car le même code servira un jour à comparer
quelque chose de plus court.

Reste l'algorithme. La méthode l'impose : HMAC-SHA256, écrit dans le code du vérificateur. Le jeton
annonce peut-être autre chose dans son en-tête — cela ne change rien au calcul effectué. Cette
surdité volontaire est la parade structurelle aux attaques par confusion d'algorithme, et elle
explique pourquoi la signature de cet exercice ne reçoit que deux paramètres : le jeton et la clé.
Il n'y a pas de place pour une négociation, et c'est exactement le but.
