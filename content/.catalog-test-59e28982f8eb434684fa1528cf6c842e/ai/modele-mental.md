# Le modèle mental : contexte, tokens et coût

Avant d'optimiser quoi que ce soit, il faut savoir ce qu'on optimise. Un assistant de code n'est ni
un moteur de recherche ni un compilateur : c'est un modèle qui lit une fenêtre de texte — le
contexte — et produit la suite la plus plausible, token par token. Tout le reste du chapitre découle
de trois propriétés de ce mécanisme.

## Trois propriétés qui gouvernent tout

**Le contexte est une ressource finie et payante.** Tout ce que le modèle « sait » de votre tâche
tient dans sa fenêtre : vos instructions, les fichiers montrés, l'historique de la conversation, ses
propres réponses précédentes. Chaque élément coûte des tokens — grossièrement, un token vaut trois
quarts d'un mot français, et du code en consomme davantage que de la prose à cause de la
ponctuation. La fenêtre se remplit vite : un fichier de mille lignes pèse plusieurs milliers de
tokens, une sortie de test verbeuse aussi. Quand elle sature, l'outil résume ou tronque, et le
modèle « oublie » — pas par caprice, par arithmétique.

**Le contexte n'est pas une mémoire fiable.** Ce qui est au milieu d'une très longue fenêtre pèse
moins que ce qui est au début ou à la fin ; une instruction donnée il y a quarante échanges peut
être diluée par tout ce qui a suivi. D'où deux réflexes : répéter les contraintes importantes au
moment où elles s'appliquent, et préférer une session neuve à une session interminable quand le
sujet change.

**La sortie est plausible, pas garantie.** Le modèle produit ce qui ressemble le plus à une bonne
réponse — ce qui inclut des noms d'API inventés, des versions confondues et des certitudes mal
placées. La conséquence pratique n'est pas « ne pas l'utiliser », c'est **ne jamais accepter une
sortie sans vérification exécutable** : compilation, test, lecture. Ce chapitre entier repose sur ce
principe, et Forge.NET vous a déjà entraîné à l'appliquer — c'est exactement la différence entre une
déclaration et une preuve.

## Où un assistant excelle, où il déçoit

Il excelle là où le coût de vérification est faible devant le coût de production : générer du code
répétitif dont les tests existent, traduire un patron connu d'un langage à un autre, expliquer un
code que vous avez sous les yeux, écrire la première version d'un test, chercher dans une grande
base ce que vous sauriez reconnaître mais pas localiser, produire des variantes pour comparer.

Il déçoit là où la vérification coûte plus cher que le travail : décisions d'architecture aux
conséquences lointaines, code de sécurité dont l'erreur est silencieuse, algorithmes subtils dont
les cas limites demandent une vraie analyse, tout domaine où vous ne sauriez pas reconnaître une
erreur. La règle de délégation est la même qu'avec un stagiaire brillant mais sans mémoire : donnez
le travail dont vous pouvez juger le résultat, gardez celui dont vous ne le pouvez pas — c'est lui
qui vous fait progresser.

## Le coût, en argent et en attention

Les fournisseurs facturent au token, entrée et sortie séparées, avec un tarif qui varie d'un facteur
dix et plus selon le modèle. Mais le coût dominant pour un développeur n'est pas la facture : c'est
**votre attention**. Une réponse de trois cents lignes non demandées se relit plus longtemps qu'elle
ne s'écrit ; un contexte pollué produit des réponses qui dérivent, que vous corrigez, ce qui pollue
davantage. Optimiser les tokens — l'objet du guide suivant — optimise d'abord votre temps de
lecture et la qualité des réponses, la facture vient ensuite.

## La règle Forge.NET, posée une fois pour toutes

Ce parcours mesure votre autonomie, et son contrat d'apprentissage engage à des séances sans IA.
Concrètement : **aucun assistant sur les exercices comptés, les examens, les réflexions préalables,
les explications personnelles et les journaux de débogage** — y faire écrire l'IA fabriquerait un
faux signal de maîtrise, précisément ce que toute la plateforme refuse. L'assistant est une
compétence de métier qui s'apprend **à côté** : sur les laboratoires, sur vos projets personnels,
sur du code qui n'est pas une preuve. Un développeur employable en 2026 sait les deux : coder sans
assistant, et multiplier sa production avec — dans cet ordre d'apprentissage.

## Ce qu'il faut retenir

Le contexte est un budget ; tout ce que vous y mettez doit y gagner sa place. La sortie est une
proposition ; seule l'exécution la transforme en fait. Et la frontière entre « l'IA travaille » et
« je prouve que je sais » n'est pas morale mais mécanique : d'un côté un outil de production, de
l'autre un instrument de mesure — les mélanger casse l'instrument.
