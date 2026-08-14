# Explication

Compter les champs d'une ligne CSV tient en un appel à `Split`, et pourtant deux décisions s'y
cachent, qui font la différence entre un compteur juste et un compteur qui ment sur les lignes
réelles.

La première concerne les champs vides, et c'est le cœur du contrat. Une ligne `a,,c` porte trois
champs, dont un vide — pas deux. `Split` avec `StringSplitOptions.None` conserve les segments
vides, et c'est exactement ce qu'il faut : dans un fichier tabulaire, un champ vide est une
donnée — la colonne existe, sa valeur est absente — et le supprimer décale toutes les colonnes
suivantes. L'option `RemoveEmptyEntries`, si commode pour découper des mots, est ici une
corruption silencieuse : les cas cachés placent des virgules adjacentes et des virgules en bord
de ligne pour la débusquer. Une ligne qui se termine par une virgule a un dernier champ vide ;
une ligne réduite à une virgule seule a deux champs vides. Le nombre de champs, c'est toujours
le nombre de séparateurs plus un — cette égalité est le test mental à faire sur chaque cas.

La deuxième décision est la borne d'entrée : la chaîne nulle ou vide rend zéro. On peut discuter
— une chaîne vide est-elle une ligne d'un champ vide, donc un ? — mais le contrat tranche pour
zéro, la convention du « rien du tout », et l'écrit dans l'énoncé. La leçon n'est pas la valeur
choisie, c'est qu'une valeur soit choisie et testée : les conventions de bord non écrites sont
la première source de désaccord entre un producteur et un consommateur de fichiers.

Enfin, l'énoncé assume une limite qui mérite d'être lue : ce micro-exercice ne gère pas les
guillemets du format complet. Dans le vrai format, une virgule entre guillemets ne sépare rien —
`"Dupont, Jean",Paris` porte deux champs — et le compteur par `Split` en verrait trois. Annoncer
cette frontière au lieu de la cacher est une pratique de contrat : un outil qui dit « je traite
le sous-ensemble sans guillemets » est utilisable en confiance dans son domaine, quand un outil
qui prétend tout traiter et se trompe est dangereux partout. Le jour où les guillemets entrent
dans le besoin, la réponse n'est pas d'améliorer le `Split`, c'est un automate à états — ou une
bibliothèque dédiée.

Le coût est linéaire, une allocation par segment ; pour *compter* sans allouer, un parcours qui
compte les virgules ferait mieux — optimisation à garder pour le jour où le profil la réclame.
