# Explication

Le cache de construction d'images est un mécanisme simple qui produit des factures compliquées. Chaque
instruction devient une couche ; une couche se réutilise tant que rien de ce qui la détermine n'a
changé ; et dès qu'une couche est invalidée, tout ce qui est bâti dessus se refait. La conséquence
pratique — dix minutes de reconstruction pour un commentaire modifié — paraît absurde tant qu'on n'a
pas fait le calcul que cet exercice mécanise.

**Pourquoi seule la copie regarde le dépôt.** Une instruction d'exécution est déterminée par son texte
et par l'état laissé par les couches précédentes ; une instruction de répertoire de travail, par son
texte seul. Aucune des deux ne lit le dépôt au moment de la construction. La copie, elle, embarque le
contenu des fichiers qu'elle vise : son empreinte change quand ce contenu change. C'est le seul canal
par lequel une modification du dépôt entre dans l'image — et c'est pour cela que la position des
copies décide de tout le comportement du cache.

**Pourquoi la première correspondance suffit.** Le cache s'évalue dans l'ordre du fichier, et la
première couche invalidée entraîne mécaniquement toutes les suivantes — y compris d'autres copies qui
n'auraient rien vu du changement. Chercher toutes les correspondances serait donc du travail perdu :
l'indice de la première fixe le coût. Ce raisonnement en cascade est le même que pour toute chaîne de
dérivation — étapes de compilation, vues matérialisées — et il explique pourquoi le comptage rend
« l'invalidée et tout ce qui la suit » plutôt que « les couches qui dépendent du fichier ».

**Pourquoi la convention de la barre oblique est stricte.** Un détail de copie peut viser un fichier
ou un répertoire, et la différence ne se voit que dans la forme du chemin. Traiter tout détail comme
un préfixe créerait des collisions muettes : un répertoire nommé comme le début d'un autre — les
sources et les sources-annexes — s'invaliderait mutuellement, et le coût des constructions deviendrait
inexplicable. La convention retenue — barre oblique finale pour un répertoire, égalité exacte sinon —
rend chaque portée décidable à la lecture.

**Ce que la règle d'or devient une fois chiffrée.** Copier le manifeste de dépendances tôt et les
sources tard n'est pas une élégance : c'est l'arbitrage entre deux fréquences. Le manifeste change
quelques fois par mois et invalide presque tout ; les sources changent à chaque validation et
n'invalident que la fin. Inverser l'ordre fait payer le prix fort à chaque validation — la
restauration des dépendances se rejoue pour un commentaire. L'exercice permet de chiffrer cet
arbitrage au lieu de le réciter : deux couches contre quatre sur l'exemple, des minutes contre des
secondes sur un vrai projet.

Le zéro final mérite un mot : un fichier jamais copié — documentation, scripts d'exploitation — ne
coûte aucune couche, et c'est un argument d'architecture. Sortir du contexte de construction tout ce
qui n'entre pas dans l'image est la première optimisation, avant même l'ordre des instructions.
