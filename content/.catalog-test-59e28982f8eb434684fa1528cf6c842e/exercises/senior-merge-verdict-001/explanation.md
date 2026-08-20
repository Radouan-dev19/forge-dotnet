# Explication

Le verdict de fusion est une politique d'équipe rendue exécutable, et ses trois règles — l'opposition
qui ne se vote pas, l'approbation périmée qui ne compte pas, l'absence de revue qui attend — corrigent
chacune un contournement observé partout où la revue est obligatoire.

**Pourquoi l'opposition ne se vote pas.** La demande de changements n'est pas un vote négatif à
compenser : c'est un veto motivé — la personne a vu quelque chose qu'elle juge devoir être corrigé
avant la fusion. Laisser trois approbations l'écraser transformerait la revue en scrutin, où il
suffirait de solliciter assez d'approbateurs bienveillants pour noyer l'objection — exactement le
contournement que la règle existe pour empêcher. Seule la personne qui a posé l'opposition peut la
lever, après discussion ou correction ; le verdict la **nomme** — la première dans l'ordre du
relevé — parce que la prochaine action de l'auteur est d'aller lui parler, pas de chercher un
quatrième approbateur.

**Pourquoi l'approbation périmée vaut zéro.** Une approbation signe un état précis du code. Quand la
demande change substantiellement après — le remaniement complet, le correctif de dernière minute —
la signature reste affichée mais ne couvre plus rien : la personne a validé un autre code. Compter
ces signatures fusionnerait du code que personne n'a relu, avec l'apparence parfaite de la
conformité — le pire des deux mondes, celui où le processus rassure sans protéger. C'est le trou que
les plateformes ferment avec l'invalidation des approbations à chaque nouveau commit ; le verdict de
cet exercice en est la sémantique, indépendante de l'outil.

**Pourquoi le relevé vide attend au lieu d'échouer.** Une demande fraîche n'a rien d'anormal : zéro
revue, zéro approbation, verdict « bloqué, zéro sur deux ». Refuser le relevé vide confondrait « pas
encore relu » avec « données corrompues », et le chiffrage de l'écart — obtenues sur exigées — fait
du verdict un affichage prêt à l'emploi : l'auteur sait exactement combien de relectures il lui
manque.

**Pourquoi le relecteur en double se refuse.** Deux états pour la même personne signifient un relevé
mal consolidé — la plateforme garde la dernière position de chacun, et un export qui en montre deux a
fusionné deux instantanés. Choisir l'un des deux ferait dépendre la fusion de l'ordre d'un fichier ;
le refus renvoie à la consolidation.

En entretien, ce sujet s'appelle la politique de protection de branche — approbations exigées,
rejet bloquant, invalidation des approbations périmées — et la question type est le second cas de
l'énoncé : « quatre approbations, une opposition, que fait-on ? ». La bonne réponse assume la
frustration des quatre : la revue n'est pas un scrutin.
