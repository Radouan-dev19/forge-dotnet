# Explication

Le choix du flux tient en deux questions ; l'exercice le sait, et c'est pourquoi il déplace la
difficulté ailleurs — dans l'analyse d'un profil textuel qui peut mentir, se contredire ou se
taire. La fonction est ainsi coupée en deux moitiés inégales : une analyse défensive, puis une
décision triviale sur des axes prouvés valides — et cette architecture est la première leçon.

L'analyse traite le profil comme toute liste encodée : découpage sur la virgule, segments
rognés, vides ignorés, casse aplanie en minuscules invariantes — un profil est saisi par des
humains dans des configurations, et `User-Present` doit valoir `user-present`. Vient ensuite le
contrôle de complétude, et il est plus strict que l'intuition : chaque axe exige *exactement*
une étiquette. L'absence d'un axe ne se comble pas par un défaut — deviner « public » pour un
client qui ne s'est pas décrit, c'est choisir un flux de sécurité sur une hypothèse. La
contradiction — `user-present` et `machine-only` ensemble — ne s'arbitre pas par « le dernier
gagne », l'erreur classique des analyseurs écrits en boucle d'écrasement. Et l'étiquette
inconnue refuse tout le profil : un profil qui contient `implicit` vient d'un monde qui croit
encore aux flux morts, et le laisser passer en ignorant le mot inconnu masquerait le vrai
problème. Le verdict `invalid-profile` regroupe ces refus d'analyse, distinct des verdicts de
décision — on ne confond pas « je ne comprends pas la question » et « la réponse est non ».

La décision, elle, tient en deux gardes ordonnées, la transcription exacte de la leçon des
flux : un humain présent envoie vers le code d'autorisation avec preuve d'échange, *quelle que
soit* la confidentialité — la preuve remplace le secret pour le client public et ne coûte rien
au confidentiel ; sans humain, le client confidentiel présente ses identifiants ; et la
combinaison restante — machine sans secret — reçoit `refused`, car aucun flux légitime ne la
couvre : la réponse du monde réel est une identité gérée par la plateforme, pas un
contournement. L'ordre des gardes importe : tester la confidentialité d'abord enverrait les
applications serveur avec utilisateurs vers les identifiants client — un flux sans consentement
pour un cas qui en exige.

Les cas cachés éprouvent les deux moitiés : ordre et casse mélangés qui passent, contradiction,
axe manquant et étiquette inconnue qui rendent l'invalidité, et les trois verdicts de décision.

Le coût est linéaire dans la longueur du profil. La transposition est double : côté protocole,
c'est la grille de choix à savoir dérouler en entretien ; côté conception, c'est le patron
analyser-puis-décider — toute décision alimentée par du texte de configuration mérite une
analyse qui refuse l'ambigu, plutôt qu'une décision qui devine par-dessus.
