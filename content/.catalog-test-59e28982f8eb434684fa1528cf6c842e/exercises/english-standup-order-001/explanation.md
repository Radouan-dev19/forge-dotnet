# Explication

L'exercice manipule des chaînes, mais ce qu'il enseigne est une règle de communication
professionnelle : **un point quotidien a un ordre, et cet ordre porte un sens**.

`done` d'abord, parce qu'il situe le travail : celui qui écoute sait où vous en êtes avant d'entendre
autre chose. `next` ensuite, parce qu'il annonce l'intention et permet à quelqu'un de dire « je fais
déjà ça ». `blocker` en dernier, parce que c'est la seule partie qui **appelle une réponse** — et
qu'une demande d'aide placée en tête, avant tout contexte, oblige l'équipe à reconstituer la
situation pour comprendre ce qu'on lui demande.

C'est pourquoi la fonction réordonne au lieu de refuser. En anglais professionnel, la structure
compte souvent plus que le vocabulaire : un point mal ordonné écrit dans un anglais parfait se fait
moins bien comprendre qu'un point bien ordonné écrit maladroitement.

**L'ordre d'arrivée est préservé à étiquette égale**, et c'est une décision, pas un oubli. Deux
lignes `done` racontent une chronologie — ce qui a été fait d'abord, puis ensuite. Les trier par
ordre alphabétique ou par longueur détruirait la seule information que leur ordre portait. La liste
par étiquette, alimentée par ajout en queue, suffit à garantir cette stabilité.

**Les lignes inexploitables sont écartées silencieusement.** Une étiquette inventée n'appartient pas
au format ; une étiquette sans texte occupe une ligne sans rien dire. Les écarter plutôt que les
refuser correspond à l'usage : un point quotidien n'est pas un formulaire à valider, c'est un message
dont on retient ce qui informe.

La normalisation — casse ignorée, blancs détourés, sortie en minuscules — rend deux points écrits par
deux personnes comparables. C'est ce qui permet, plus tard, d'agréger les blocages d'une équipe sans
les lire un par un.

Le coût est linéaire en nombre de lignes, ce qui est la borne évidente : chaque ligne est lue une
fois, écrite une fois.

**Pourquoi cet exercice appartient au domaine de l'anglais et non à celui des chaînes de
caractères.** Le découpage sur un deux-points est trivial ; ce qui ne l'est pas, c'est de savoir
qu'un point quotidien a une forme attendue, et laquelle. Un développeur francophone qui rejoint une
équipe anglophone passe rarement les premiers mois à chercher ses mots : il passe les premiers mois
à ne pas savoir *comment on dit les choses ici*. Combien de contexte donner. Où placer la demande.
Quand s'arrêter. Ces conventions ne s'apprennent pas dans un manuel de grammaire, et elles pèsent
plus lourd que le vocabulaire dans la perception de compétence.

**La forme rendue est délibérément pauvre**, et c'est un choix de conception à défendre. La fonction
ne reformule rien, ne corrige aucune faute, ne juge pas la longueur d'une ligne. Elle réordonne et
normalise, point. Une fonction qui prétendrait améliorer la langue introduirait un jugement qu'aucun
test ne peut vérifier, et se tromperait sur les cas qui comptent — un anglais approximatif mais
précis vaut mieux qu'un anglais fluide et vague. La seule chose qu'un programme peut garantir ici,
c'est la structure ; il s'y tient.

**Ce que la fonction rend possible plus tard.** Une fois les points normalisés, une équipe peut
extraire tous les blocages d'une semaine sans les relire un par un, repérer celui qui revient trois
jours de suite, et le traiter comme un problème d'organisation plutôt que comme une plainte
individuelle. C'est le bénéfice réel d'un format : il rend agrégeable ce qui n'était que
conversationnel.
