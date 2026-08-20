# Explication

Compter les lignes d'erreur d'un journal est souvent le premier chiffre d'un incident — combien
de fois, depuis quand, à quel rythme — et cet exercice en isole le geste de base : chercher un
marqueur exact, encore et encore, sans en rater ni en inventer.

Le marqueur est cherché tel quel, en comparaison ordinale et en majuscules : `ERROR` est un
*niveau de gravité*, un mot technique écrit par la bibliothèque de journalisation, pas du texte
humain. La recherche insensible à la casse compterait aussi le mot « error » apparaissant dans
un message — « no error found », ironiquement — et gonflerait le chiffre avec des faux
positifs. La précision inverse mérite d'être dite aussi : la recherche par sous-chaîne compte
*toutes* les apparitions du marqueur, y compris au milieu d'un message qui citerait le mot en
majuscules. Le compteur par `IndexOf` est donc un estimateur — excellent sur des journaux au
format discipliné, approximatif sur du texte libre — et le fiabiliser passerait par une analyse
par lignes avec position du niveau. Connaître la marge d'erreur de son instrument fait partie
du diagnostic.

La boucle est le balayage standard : chercher à partir du point courant, compter, avancer. Le
point de reprise saute la longueur entière du marqueur — cinq caractères — ce qui rend les
occurrences disjointes ; pour ce marqueur sans motif répétitif interne, le pas de un donnerait
d'ailleurs le même compte, mais le saut complet est la forme correcte du motif et celle qui
survivrait à un marqueur auto-chevauchant. L'affectation dans la condition du `while` est
l'idiome consacré de ce balayage — compacte, elle demande une lecture attentive une fois, puis
se reconnaît partout.

Les `ERROR` adjacents — collés ou séparés d'un espace comme dans l'exemple — comptent chacun :
le cas caché aux occurrences serrées vérifie que l'avancement ne saute pas par-dessus une
occurrence voisine. Le journal vide ou absent rend zéro, convention de comptage cohérente avec
un outil qui doit digérer une rotation de fichiers sans se plaindre.

Le coût est linéaire dans la taille du journal — chaque caractère est visité au plus une fois
par la recherche — et l'espace constant : pas de découpage en lignes, pas d'allocation, ce qui
compte quand le fichier pèse des centaines de mégaoctets.

La transposition est l'instrumentation de fortune qui sauve les astreintes : compter des
marqueurs de niveau, des codes HTTP, des identifiants d'exception dans un flux de texte — avec,
à chaque fois, les deux questions de cet exercice : mon marqueur est-il assez exact pour ne pas
compter du bruit, et mon point de reprise avance-t-il sans rater ni recompter ?
