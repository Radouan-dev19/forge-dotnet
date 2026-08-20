# Explication

La gestion des valeurs sensibles souffre de deux réflexes symétriques : tout mettre en configuration
— jusqu'au jour de la fuite — ou tout mettre au coffre — jusqu'au jour où plus personne ne sait ce
qui est réellement sensible. La cascade de cet exercice remplace les réflexes par trois questions
ordonnées, et chaque étage mérite son pourquoi.

**Pourquoi la sensibilité se juge en premier.** Le canal de secret a un coût — indirection, droits
d'accès, latence de résolution, procédures d'urgence — qui ne se justifie que pour ce dont la fuite
fait un incident. Y monter les adresses de services et les noms de files produit un double dégât : la
configuration ordinaire devient illisible, éparpillée entre deux mondes, et le canal de secret se
banalise — quand tout est secret, plus personne ne traite un accès au coffre comme un événement. La
première question de la cascade est donc un tri : ce qui peut vivre en clair vit en clair.

**Pourquoi l'identité attestée prime sur tout le reste, rotation comprise.** Le coffre central a une
faille conceptuelle bien connue : il protège des secrets, mais il faut un secret pour y accéder —
c'est le problème du premier secret, la régression sans fin des gardiens à garder. L'identité gérée
par la plateforme interrompt cette régression : la plateforme atteste l'identité de la charge, et il
n'existe plus d'identifiant stocké — donc plus rien à faire fuiter, plus rien à faire tourner, plus
rien à purger d'un dépôt. C'est pourquoi elle prime dès que le consommateur est hébergé : la rotation
devient un non-sujet quand le secret n'existe plus. Choisir le coffre « par prudence » pour une
charge hébergée, c'est réintroduire volontairement un maillon d'authentification que la plateforme
offrait de supprimer.

**Pourquoi le poste local se départage par la rotation.** Le développeur n'a pas d'identité de
plateforme, et deux besoins se présentent. La valeur statique — une clé de service de test — a
seulement besoin de rester hors du dépôt : le magasin utilisateur le fait sans infrastructure, et sa
limite connue — aucune synchronisation — est indolore pour ce qui ne change pas. La valeur tournante,
elle, rend cette limite fatale : le magasin local sert éternellement la valeur d'hier, et l'équipe
découvre la rotation par les échecs d'authentification du lundi matin. Seule une source centrale sert
la valeur courante après chaque rotation — d'où le coffre, précisément là où il apporte ce que rien
d'autre n'apporte.

**Pourquoi l'ordre est strict et les refus aussi.** Laisser la rotation décider avant l'hébergement
enverrait des charges hébergées vers le coffre avec identifiant stocké — la régression réintroduite.
Et deviner un attribut manquant produirait une recommandation de sécurité plausible et infondée, le
genre qui s'audite deux ans plus tard.

La transposition : la cascade se rejoue à chaque nouvelle valeur d'un projet, et sa première question
— est-ce vraiment un secret ? — est celle qui économise le plus, en argent comme en clarté.
