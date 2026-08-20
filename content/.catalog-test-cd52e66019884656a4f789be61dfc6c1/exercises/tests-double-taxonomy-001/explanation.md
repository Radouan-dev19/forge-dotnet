# Explication

Les doubles de test souffrent d'un vocabulaire flou — tout s'appelle « mock » dans la conversation —
et ce flou a un coût mesurable : des suites qui vérifient trop, cassent au moindre remaniement, et
finissent contournées. La taxonomie de cet exercice remplace le choix par nom — « une base, donc un
fake » — par un choix par structure, sur deux axes qui suffisent à tout classer.

**Le premier axe est le sens du flux, et il change la question posée.** Une dépendance entrante
fournit des données : la question du test est « que fait mon code avec ces données ? », et la
dépendance n'a qu'à les servir. Une dépendance sortante reçoit des effets : la question devient
« mon code a-t-il produit le bon effet ? », et c'est l'effet lui-même qu'il faut rendre observable.
Confondre les deux axes produit les deux pathologies classiques : espionner une lecture — le test
vérifie alors combien de fois le code lit, un détail que tout remaniement honnête a le droit de
changer — ou servir une réponse toute faite à une écriture, ce qui ne vérifie littéralement rien.
Les deux croisements interdits de l'exercice sont exactement ces deux pathologies, et les refuser
avec une exception plutôt que de les « interpréter » est le choix pédagogique central : un outil qui
attribue un double plausible à un descriptif incohérent aide à écrire un mauvais test plus vite.

**Le second axe est la nature du contrat, et il sépare les deux emplois du fake.** La réponse toute
faite suffit quand le test ne dépend d'aucune cohérence — l'heure, une configuration, un tirage. Dès
que le test enchaîne écriture puis lecture, le contrat devient comportemental : servir des réponses
indépendantes laisserait passer un code qui écrit sans relire, et la simulation légère — le dépôt en
mémoire — est l'outil qui honore ce contrat sans infrastructure. Côté sortant, la même simulation
sert l'effet relisible — le courrier accumulé qu'on relit — tandis que l'espion sert le protocole,
quand l'ordre et le contenu des appels **sont** le contrat, comme une séquence d'annulation ou de
compensation. Le même mot « fake » couvre donc deux cases pour une bonne raison : dans les deux, on
remplace la dépendance par une implémentation qui se comporte, et le test observe des états, pas des
appels.

**Pourquoi l'attribution se fait dépendance par dépendance, en liste.** Un test réel a plusieurs
dépendances, et le mélange des doubles dans un même test est normal — horloge bouchonnée, dépôt
simulé, bus espionné. L'inventaire en une passe, dans l'ordre du descriptif, produit un document de
conception : la ligne de sortie se colle dans la revue de conception du test, et chaque désaccord
porte sur une dépendance nommée plutôt que sur une doctrine générale.

La transposition tient en une pratique : avant d'écrire un test à dépendances, écrire son descriptif
flux-contrat. La moitié des débats sur « mock ou pas mock » disparaissent quand la question devient
« quel flux, quel contrat » — et l'autre moitié devient un vrai débat de conception sur ce que le
produit devrait exposer.
