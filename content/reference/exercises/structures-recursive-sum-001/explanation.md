# Explication

Sommer un tableau récursivement n'a aucun intérêt pratique — la boucle fait mieux — et c'est
assumé : l'exercice enseigne la *forme* de la récursion sur séquence, celle qui devient
indispensable dès que la structure n'est plus plate, sur un cas assez simple pour qu'on voie
tous ses rouages.

Le premier rouage est la signature auxiliaire. La fonction publique reçoit un tableau ; la
récursion, elle, a besoin d'un état d'avancement — l'index courant — qui ne regarde pas
l'appelant. D'où la fonction locale `Sum(items, index)` : l'interface publique reste propre, et
l'état de progression vit dans les paramètres de l'auxiliaire. Ce découpage
façade-plus-auxiliaire-indexé est le gabarit de presque toutes les récursions sur séquences et
chaînes, et le mot clé `static` sur la fonction locale ajoute une garantie de discipline : elle
ne capture rien de son environnement, tout ce qu'elle utilise passe par ses paramètres — la
récursion est pure, lisible dans sa seule signature.

Le deuxième rouage est le choix du cas de base : `index == items.Length` rend zéro. La somme du
suffixe vide est le neutre de l'addition — la même logique que le produit vide de la
factorielle voisine — et ce choix fait que le tableau vide ne demande aucune garde : le premier
appel *est* déjà le cas de base. Placer l'ancre sur « plus rien à traiter » plutôt que sur
« dernier élément » évite un cas spécial et une exception sur le vide ; c'est un critère de
conception que l'énoncé impose en fixant l'ancre à la longueur.

Le troisième rouage est la progression : `index + 1` à chaque appel, strictement croissante et
bornée par la longueur — la terminaison se lit. Et le calcul remonte au *retour* : chaque niveau
additionne son élément au résultat du suffixe, en `checked` pour que le cumul qui déborderait
lève au lieu de mentir.

L'honnêteté oblige à nommer le coût caché : chaque élément consomme un cadre de pile, et un
tableau de très grande taille provoquerait un débordement de pile là où la boucle
travaillerait en espace constant. C'est *la* limite structurelle de la récursion sur séquence
en C# — pas d'élimination d'appel terminal garantie — et la raison pour laquelle la forme
récursive se réserve aux structures dont la profondeur est naturellement bornée : arbres
équilibrés, expressions imbriquées, dossiers. Savoir dire « je sais l'écrire, et voici pourquoi
je ne le ferais pas ici » est exactement la réponse attendue en entretien.

Les cas cachés couvrent le vide, l'élément unique, les négatifs mêlés — la somme n'est pas un
maximum, tout compte — et une disposition qui réfute le résultat figé. La transposition :
récursion pour les structures récursives, boucle pour les plates — et toujours une ancre sur le
vide, une progression stricte, un état porté par les paramètres.
