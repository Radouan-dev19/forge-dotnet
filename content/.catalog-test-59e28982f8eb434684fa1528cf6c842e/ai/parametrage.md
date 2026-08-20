# Paramétrer son assistant : du réglage global au réglage par requête

Un assistant mal paramétré se corrige à chaque échange ; un assistant bien paramétré part juste.
Les réglages s'empilent en couches, de la plus durable à la plus ponctuelle — et la discipline
consiste à mettre chaque consigne dans la bonne couche.

## Couche 1 — Les consignes de dépôt, la plus rentable

La plupart des outils lisent au démarrage un fichier de consignes versionné à la racine du projet —
ce dépôt en a deux, `AGENTS.md` et les standards sous `docs/`, et c'est un exemple à imiter. Ce qui
y a sa place : l'architecture en une phrase par couche, les commandes de build et de test exactes,
les conventions non négociables (encodage, style, ce qu'on ne commite jamais), les pièges connus du
projet. Ce qui n'y a pas sa place : les demandes du jour. Trois vertus : chaque session part
informée sans que vous répétiez rien ; le fichier est stable donc servi depuis le cache de préfixe ;
et il est **relu en revue de code** comme le reste du dépôt — vos consignes d'IA deviennent un
artefact d'équipe, pas un savoir oral.

Écrivez-le comme une spécification, pas comme un vœu : « les tests s'exécutent avec telle commande,
un test rouge interdit de conclure » se vérifie ; « écris du code propre » ne pilote rien.

## Couche 2 — Le choix du modèle et de l'effort

Deux curseurs distincts. **Le modèle** : les familles proposent des tailles, du rapide-économique au
lent-profond. Routez par nature de tâche — mécanique répétitive vers le petit, conception et
diagnostic vers le grand — et changez en cours de session quand la nature change. **L'effort de
raisonnement** (ou le budget de réflexion, selon les outils) : combien le modèle délibère avant de
répondre. Montez-le pour un bogue retors ou un choix d'architecture ; baissez-le pour du formatage —
délibérer longuement pour renommer une variable coûte sans rien apporter. Quant à la température,
laissez la valeur par défaut pour le code : la « créativité » y produit surtout des variations non
demandées.

## Couche 3 — Les sorties structurées

Dès qu'un programme consomme la réponse — un script qui lit du JSON, un tableau à intégrer —
n'espérez pas un format, **imposez-le** : les outils sérieux acceptent un schéma de sortie et
refusent les réponses qui n'y entrent pas, ce qui remplace l'analyse fragile de texte libre par une
validation mécanique. Ce dépôt applique l'idée partout : ses manifestes de contenu sont validés par
schéma avant d'être servis, et ses agents de test rendent des objets typés. Faites pareil : un
format défini d'avance élimine toute une classe de « la réponse était bonne mais illisible ».

## Couche 4 — Les outils et leurs permissions

Un assistant moderne n'est pas qu'un générateur de texte : il lit des fichiers, exécute des
commandes, interroge des services via des connecteurs. Deux règles d'hygiène. **Le moindre
privilège** : n'accordez que les accès dont la tâche a besoin — un assistant qui relit du code n'a
pas à pousser sur le dépôt distant, et une commande destructrice mérite une confirmation manuelle,
toujours. **La méfiance envers le contenu externe** : tout texte qu'un outil rapporte — page web,
ticket, sortie de commande — peut contenir des instructions déguisées à destination du modèle ; un
assistant qui lit l'extérieur et détient des permissions d'écriture doit être traité comme une
surface d'attaque. Le bac à sable de ce dépôt — réseau coupé, utilisateur non-root, quotas — montre
la posture : on n'exécute pas du code non fiable avec des privilèges, on l'enferme.

## Couche 5 — La requête elle-même

Le dernier réglage est le prompt du moment, et une structure suffit : le **contexte** que la couche 1
ne porte pas (« la fonction en cours de migration est celle-ci »), la **tâche** en verbes précis,
les **contraintes** (« sans nouvelle dépendance », « en gardant la signature publique »), et le
**format de sortie**. Une demande floue prend son coût en allers-retours ; trois phrases précises le
paient une fois.

## L'erreur de couche, le défaut le plus courant

Répéter à chaque message une règle qui devrait vivre dans le fichier de dépôt ; graver dans le
fichier de dépôt une préférence d'un jour ; corriger la forme d'une réponse à la main au lieu
d'imposer un schéma ; monter l'effort de raisonnement en permanence « pour la qualité ». À chaque
friction récurrente, demandez-vous : dans quelle couche cette consigne aurait-elle dû vivre pour que
la friction n'existe pas ? C'est le geste de paramétrage fondamental — le reste n'est que syntaxe
d'outil.
