# Explication

Normaliser un segment de route, c'est fabriquer la forme canonique sous laquelle deux écritures
du même chemin deviennent comparables : ` /Orders/ ` et `orders` désignent la même collection,
et tout ce qui compare des routes — cache, autorisation, statistiques — a besoin qu'elles se
ressemblent enfin. La fonction enchaîne trois nettoyages, et la précision de chacun est le
sujet.

Le premier rogne les blancs de bord — l'artefact de saisie et de configuration habituel. Le
deuxième rogne les *séparateurs* de bord : `Trim('/')` retire les barres obliques en tête et en
queue, si bien que `/orders/` et `orders` convergent. L'ordre des deux rognages compte — les
blancs d'abord, car ` /orders` porte son séparateur derrière un espace — et leur portée aussi :
les bords *seulement*. C'est la clause que l'énoncé souligne en demandant ce qu'une fusion de
segments produirait : `orders/42` doit garder sa barre interne, car elle sépare deux segments —
la collection et l'identifiant. Une normalisation qui retirerait toutes les barres fabriquerait
`orders42`, une route qui n'existe pas, et le client verrait ses liens réécrits vers le néant.
La distinction bord-contre-intérieur est la même que pour les blancs des chaînes, appliquée à
un séparateur structurel.

Le troisième nettoyage aplanit la casse en minuscules invariantes : les routes se veulent
insensibles à la casse pour l'utilisateur, et la forme canonique choisit une casse une fois
pour toutes — l'invariant garantissant que le choix ne dépend pas de la machine. C'est le trio
désormais familier — bords, structure, casse — appliqué au vocabulaire des chemins.

L'entrée vide ou blanche rend la chaîne vide : il n'y a pas de segment, et la convention du
vide compose bien avec les appelants qui testent la longueur. Un chemin réduit à des barres —
`///` — converge aussi vers le vide, par le rognage des séparateurs : le cas caché posé dessus
vérifie que la chaîne survit aux entrées dégénérées.

Les cas suivent l'énoncé : le déjà-propre qui traverse inchangé — l'idempotence de la
normalisation, propriété à vérifier —, le bordé qui se nettoie, l'interne qui se préserve, le
blanc qui se vide.

Le coût est linéaire, deux allocations au plus. La transposition est la notion de *forme
canonique* elle-même : chaque fois que deux représentations d'une même chose doivent être
comparées — routes, noms de fichiers, adresses de courriel —, on définit une normalisation, on
l'applique aux deux bords de la comparaison, et on exige qu'elle soit idempotente. Les
comparaisons « qui marchent presque » des systèmes réels sont presque toujours des canons
appliqués d'un seul côté.
