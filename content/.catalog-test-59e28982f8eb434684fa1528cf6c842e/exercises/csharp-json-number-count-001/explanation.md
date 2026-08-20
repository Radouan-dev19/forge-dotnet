# Explication

Compter les nombres d'un tableau JSON force à travailler avec du contenu *typé par le document*,
et c'est ce qui distingue l'exercice d'un simple parcours : le type d'un élément JSON n'est pas
celui d'une variable C#, il se découvre à l'exécution, élément par élément.

L'outil central est `JsonDocument.Parse`, et son choix face à l'alternative se justifie. La
tentation artisanale — chercher des chiffres dans la chaîne — échoue immédiatement : `"12"`
entre guillemets est une *chaîne* JSON qui contient des chiffres, pas un nombre, et un vrai
nombre peut s'écrire `-3.5e2`. Seul un analyseur du format fait cette distinction, et
`ValueKind == JsonValueKind.Number` la lit ensuite sans ambiguïté : le nombre JSON est une
catégorie syntaxique, pas une apparence. Le cas caché qui mélange `1` et `"1"` dans le même
tableau départage définitivement l'analyseur du bricolage.

`JsonDocument` plutôt que la désérialisation vers un type C# : ici, le document est hétérogène
par nature — nombres, chaînes, booléens mêlés — et aucun type cible ne le décrit. Le modèle
document expose chaque élément avec son genre et laisse le code décider ; la désérialisation
typée reprendra l'avantage quand le format sera connu et stable. Savoir choisir entre les deux
modèles est une compétence d'API réutilisée bien au-delà de cet exercice. Le `using` sur le
document n'est pas décoratif : `JsonDocument` loue de la mémoire en pool et doit être libéré —
l'oublier fonctionne en apparence et dégrade le service sous charge.

Les décisions de bord structurent le reste. L'entrée absente ou blanche rend zéro — convention
de comptage, cohérente avec « rien à compter ». Un document valide dont la racine n'est pas un
tableau rend zéro aussi : le contrat compte *les éléments numériques d'un tableau racine*, et un
objet racine n'en a pas ; on aurait pu lever, le contrat a choisi la tolérance, et l'important
est que le choix soit écrit. En revanche, un JSON *malformé* lève — `Parse` échoue — et c'est
voulu : la différence entre « document valide qui ne me concerne pas » et « document illisible »
mérite deux comportements, car l'appelant ne corrige pas les deux de la même façon.

Sur le tableau lui-même, `EnumerateArray` ne parcourt que le premier niveau : un tableau imbriqué
est un élément de genre tableau, pas une série de nombres à aplatir. Le comptage est donc
strictement de surface, ce que les cas cachés vérifient avec une imbrication.

Le coût est linéaire dans la taille du document — l'analyse domine — et la transposition est
directe : valider aux frontières avec un analyseur du format, décider explicitement du sort des
documents valides-mais-inattendus, et laisser lever ce qui est illisible.
