# Explication

« Les trois plus grands » est la question de palmarès dans sa version minimale, et sa réponse
LINQ — ordonner en descendant, borner, figer — vaut surtout par les deux clauses de contrat
qu'elle doit respecter sans qu'on les lui rappelle.

La première concerne les doublons : ils *comptent*. Un tableau `[9, 9, 5, 1]` a pour podium
`[9, 9, 5]` — deux concurrents peuvent avoir le même score, et le palmarès reflète les
occurrences, pas les valeurs distinctes. L'erreur classique glisse un `Distinct()` dans la
chaîne « pour faire propre » et transforme la question en une autre : « les trois plus grandes
valeurs différentes ». Les deux questions sont légitimes ; le contrat en pose une seule, et le
cas caché aux doublons de tête départage les implémentations. La leçon dépasse LINQ : chaque
opérateur ajouté à une chaîne change la *question posée*, pas seulement la forme de la réponse.

La deuxième clause est le comportement en pénurie : un tableau de deux éléments rend un podium
de deux, un tableau vide rend un podium vide. `Take(3)` a exactement cette sémantique — il rend
*au plus* trois éléments et s'arrête sans bruit quand la source s'épuise — là où un accès par
indices `[0..3]` lèverait ou exigerait des gardes. Choisir l'opérateur dont le comportement de
bord est déjà celui du contrat, c'est faire disparaître du code de garde, et c'est une heuristique
de conception qui se réutilise partout.

Sur le coût, l'honnêteté s'impose : `OrderByDescending` trie toute la source — n log n — pour
n'en garder que trois. Un parcours qui maintient les trois meilleurs au fil de l'eau ferait le
travail en linéaire, et c'est la bonne réponse quand n devient grand ou que la question revient
souvent. Ici, la clarté de la chaîne l'emporte sur une optimisation sans enjeu à ces tailles ;
savoir *énoncer* l'alternative et son seuil de rentabilité est ce qu'un entretien attend, plus
que de l'écrire d'office. Notons aussi que le tri LINQ ne touche pas la source — il travaille
sur une copie interne — ce qui tient la clause de non-mutation que le harnais vérifie.

Le régime d'erreur ne change pas : `null` levé en tête, nommément. Et la sortie est figée par
`ToArray` en fin de chaîne, une matérialisation unique et justifiée puisque la signature promet
un tableau.

La transposition est immédiate — meilleures ventes, pires latences, derniers connectés — et les
deux clauses reviennent à chaque fois : que font les ex æquo, que rend la pénurie ? Deux phrases
dans le contrat, deux cas dans les tests, et le palmarès cesse d'être une source de surprises.
