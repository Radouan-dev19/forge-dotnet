# Explication

Une revue de code utile tient dans un signal net : ce qui doit etre corrige avant de fusionner, et ce
qui reste un avis. Ce noyau entraine precisement cette discipline de classement, parce que c'est elle,
et non la capacite a repérer un defaut, que la revue en equipe mobilise chaque jour. Reperer une
faute ne sert a rien si le relecteur ne sait pas dire si elle bloque la fusion ou non.

Le classement se fait sur deux axes. La gravite d'abord : un constat est `blocking` ou `minor`. La
categorie ensuite : `correctness` pour un comportement faux, `security` pour une faille, `concurrency`
pour un acces partage non protege, `style` pour une preference de forme. Le couple `severite:categorie`
porte les deux informations sans les confondre ; les fusionner en une seule etiquette perdrait la
distinction qui compte au moment de decider d'un merge.

La regle de fond est asymetrique et c'est tout l'enjeu. Trois familles bloquent : une faute de
correction casse le resultat, une faille de securite expose l'utilisateur, un acces concurrent non
synchronise produit un bug non deterministe qui echappera aux tests. Le style ne bloque jamais. Une
preference de nommage ou un commentaire manquant sont des avis que l'auteur peut suivre ou non ; les
transformer en veto est un faux positif. Ce faux positif a un cout reel et mesurable : il retarde des
fusions saines, il noie les vrais bloquants sous le bruit, et il erode la confiance dans le relecteur,
au point que ses remarques serieuses finissent ignorees. C'est pourquoi les cas de test comptent une
faute de style presentee comme un candidat au blocage : la bonne reponse la classe `minor:style`, et
une solution qui la bloquerait echoue.

Les cas caches couvrent chaque famille et les deux bornes du domaine. La concurrence est representee
parce qu'elle est la plus facile a sous-estimer en revue : le code compile, les tests passent, et le
defaut ne se manifeste que sous charge. Un identifiant inconnu rend `unknown` plutot qu'une etiquette
inventee : un relecteur honnete admet qu'il ne sait pas classer un constat qu'il ne reconnait pas,
au lieu de trancher au hasard. Un identifiant nul, lui, n'est pas un constat mais une faute d'appel,
et merite une exception d'argument.

La transposition est directe vers la pratique et vers l'entretien. Savoir classer un constat par
gravite, et surtout savoir ne pas bloquer sur du style, distingue un relecteur qui fait avancer une
equipe d'un relecteur qui la freine. Le projet de revue de la meme semaine prolonge ce noyau sur des
diffs entiers ; ici, on isole la decision de classement pour la rendre reflexe.
