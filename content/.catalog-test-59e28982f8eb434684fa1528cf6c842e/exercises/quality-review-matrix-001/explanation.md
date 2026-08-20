# Explication

La sévérité d'un constat de revue est le point où les équipes s'usent le plus : chacun a raison depuis
son expérience, et la discussion recommence à chaque demande de fusion. Un barème écrit ne rend pas la
question objective — les pondérations restent un choix — mais il déplace le désaccord au bon endroit :
on débat une fois des poids, en réunion, au calme, plutôt que de débattre de chaque constat sous la
pression d'une livraison. Le code de cet exercice est l'exécuteur fidèle de ce contrat d'équipe, et
chacune de ses décisions découle de ce rôle.

**Pourquoi un barème additif plutôt qu'un arbre de règles.** Trois attributs à trois ou quatre valeurs
donnent trente-six combinaisons ; un arbre de cas particuliers serait illisible et pousserait chaque
discussion vers l'ajout d'une branche. L'addition a une propriété précieuse : elle rend les poids
comparables et donc discutables. Dire « la nature pèse deux fois le rayon » est une phrase qu'une
équipe peut contester avec des exemples ; une forêt de conditions ne s'audite pas. La contrainte
d'interdire les règles spéciales dans le code n'est pas un purisme : la première exception codée en
dur — « la sécurité bloque toujours » — réintroduit exactement l'arbitraire que le barème devait
éliminer, et le fait en silence, là où personne ne relit.

**Pourquoi la sécurité ne bloque pas automatiquement.** L'intuition proteste : un constat de sécurité
laissé passer, c'est grave. Mais le barème pondère aussi l'atteignabilité et le rayon, et c'est sa
force : un défaut de sécurité dans du code mort mérite d'être traité — trois points, la tranche s'en
souvient — sans bloquer une livraison qui ne l'expose pas. L'automatisme inverse, tout-sécurité-bloque,
apprend aux équipes à requalifier les constats en « robustesse » pour passer, ce qui est le pire des
deux mondes : la catégorie ment et le barème aussi.

**Pourquoi les refus sont stricts.** Un attribut manquant complété par défaut fabriquerait une
sévérité que personne n'a déclarée ; une clé répétée dont la seconde valeur écrase la première ferait
dépendre le verdict de l'ordre d'écriture ; une valeur hors vocabulaire acceptée « au plus proche »
inventerait une catégorie. Dans les trois cas, le chiffre rendu aurait l'air calculé alors qu'il
serait deviné — et un barème qui devine perd la seule chose qui le justifie, la confiance.

**Pourquoi l'ordre des attributs est libre.** Le constat est écrit par des humains et des outils
variés ; exiger un ordre fixe transformerait des refus de forme en faux désaccords de fond. Analyser
en paires clé-valeur coûte trois lignes et évite cette friction.

La transposition dépasse la revue : toute politique d'équipe chiffrée — priorité d'incidents, tri de
dette — gagne à être un barème déclaré, exécuté sans exception, et amendé en réunion plutôt qu'au
détour d'un correctif.
