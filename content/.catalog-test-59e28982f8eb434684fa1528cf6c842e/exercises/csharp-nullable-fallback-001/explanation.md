# Explication

Une expression conditionnelle, un `Trim`, un libellé de repli : la solution semble ne rien
contenir, et c'est précisément parce que tout est dans la *doctrine* qu'elle applique — une
absence attendue n'est pas une erreur, et elle reçoit une valeur de repli explicite.

La distinction structure toute la gestion des données optionnelles. Un champ facultatif vide —
commentaire non rempli, référence externe absente — est un état *normal* du domaine : le
traiter par exception obligerait chaque lecture à un bloc d'interception pour un non-événement,
et le laisser filer en `null` disséminerait des bombes à retardement dans tout le code aval. Le
repli nommé — ici `n/a` — absorbe l'absence à la frontière et garantit à tout le reste du
programme une chaîne exploitable. À l'inverse, une donnée *obligatoire* absente est une erreur
métier, et elle mérite l'exception : le même mécanisme ne sert pas les deux cas, et confondre
les deux régimes est la source des `null` qui traversent trois couches avant d'exploser loin de
leur origine.

Le choix de `IsNullOrWhiteSpace` comme critère d'absence mérite sa phrase : `null`, la chaîne
vide et la chaîne d'espaces sont trois encodages du même fait — l'utilisateur n'a rien fourni —
et les regrouper donne un comportement uniforme quelle que soit la façon dont le vide est
arrivé : formulaire, base, fichier. La version qui ne testerait que `null` laisserait passer
`"   "` tel quel, et l'affichage montrerait du blanc là où le repli était attendu ; le cas caché
d'espaces purs la réfute.

Sur le chemin nominal, la valeur est rognée avant d'être rendue — `value.Trim()` — ce qui
normalise les bords sans toucher l'intérieur. L'ordre des deux moitiés de l'expression n'est
pas interchangeable : c'est le test d'absence qui protège le `Trim`, une valeur `null`
déréférencée lèverait. L'expression conditionnelle ternaire est ici l'outil juste — deux issues,
une par branche, pas d'effet de bord — là où un `if` complet n'ajouterait que du volume.

Un mot sur ce que le repli *n'est pas* : une valeur magique à tester ailleurs. Si du code aval
se met à comparer à `n/a` pour retrouver l'absence, le repli a échoué — il était fait pour
l'affichage, pas pour ressusciter l'information perdue. Quand l'aval a besoin de distinguer,
c'est le type qui doit porter l'option, pas la valeur.

Le coût est négligeable ; la transposition, quotidienne : chaque champ optionnel d'un système
mérite cette décision écrite — quel repli, à quelle frontière, et pour quel usage. Une ligne de
contrat qui épargne des années de vérifications défensives dispersées.
