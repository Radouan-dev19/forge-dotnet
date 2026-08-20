# Explication

Un plancher de couverture n'a de valeur que si la porte qui l'applique est étanche. L'exercice a l'air
d'une soustraction et d'une boucle ; sa vraie matière est le chemin de calcul entre une mesure brute —
deux comptes de branches — et une décision binaire — fusionner ou non. Chaque maillon de ce chemin
peut mentir d'une façon différente, et chacune a son cas caché.

**Pourquoi l'arithmétique entière et pas le pourcentage.** Le pourcentage est une présentation, pas
une donnée : le passage par la division flottante fabrique une valeur approchée, puis la comparaison
au plancher transforme cette approximation en verdict. Un module à 79,96 pour cent est en dessous du
plancher de quatre-vingts ; le même module, passé par un arrondi à un chiffre, se présente à 80,0 et
franchit la porte. L'inégalité en produits croisés — le couvert multiplié par cent contre le plancher
multiplié par le total — pose exactement la même question sans fabriquer un seul chiffre intermédiaire.
Elle a un prix : sur un dépôt à un milliard de branches, le produit dépasse la capacité d'un entier de
trente-deux bits, et le calcul doit s'élargir avant de multiplier. Le débordement rendrait la porte
aléatoire, ce qui est pire qu'une porte trouée : plus personne ne saurait dire dans quel sens elle se
trompe.

**Pourquoi le module sans branche passe.** La couverture est un quotient, et son dénominateur peut
être nul. Trois politiques existent : bloquer, passer, ou refuser la mesure. Bloquer punit les modules
qui ne contiennent aucune décision — des enregistrements, des constantes — et pousse mécaniquement à
écrire des branchements inutiles pour nourrir l'indicateur. Refuser paralyserait tout dépôt qui
contient un module trivial. Passer est la seule politique qui n'invente pas d'obligation : rien à
couvrir, rien à exiger.

**Pourquoi les mesures incohérentes sont refusées plutôt qu'écrêtées.** Un compte couvert supérieur au
total, ou négatif, ne décrit aucun état possible du dépôt : c'est le symptôme d'un rapport corrompu ou
d'une fusion de fichiers de mesure qui a mal tourné. Écrêter la valeur produirait une décision
plausible fondée sur une donnée fausse — précisément le genre d'erreur qu'on ne retrouve jamais. Le
refus remonte le problème à la source, là où il est réparable.

**Ce que la liste d'indices change par rapport à un booléen.** Rendre « bloqué ou non » suffirait à la
porte, mais la personne qui reçoit le refus a besoin de savoir où agir. Désigner les modules fautifs
transforme un verdict en plan de travail, et l'ordre croissant rend la sortie stable d'une exécution à
l'autre — une propriété que tout outil de chaîne d'intégration finit par exiger.

La transposition est immédiate : toute porte chiffrée — taille de lot, budget de latence, quota
d'avertissements — repose sur le même trio : comparaison exacte, cas dégénéré assumé, mesure
invraisemblable refusée.
