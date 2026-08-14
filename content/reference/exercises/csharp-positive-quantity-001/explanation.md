# Explication

Une garde qui rend sa propre entrée : la fonction la plus courte du catalogue, et l'une des plus
mal comprises en pratique, parce que sa valeur n'est pas dans ce qu'elle calcule — rien — mais
dans ce qu'elle *établit* pour tout le code qui suit.

`RequireQuantity` est un point de passage : au-delà de lui, la quantité est positive ou nulle,
prouvé. Chaque fonction aval peut cesser de re-vérifier — plus de gardes défensives dispersées,
plus de branches « au cas où » impossibles à couvrir en test. Ce style, la validation aux
frontières qui rend la valeur validée, permet des écritures en ligne :
`stock.Reserve(RequireQuantity(input))`. La valeur circule, le contrat voyage avec elle. C'est
la version fonction de ce que les assistants `ArgumentNullException.ThrowIfNull` font pour les
références, et la préfiguration des types dédiés — une `Quantity` qui n'admet pas de négatif à
la construction — qu'un domaine plus riche introduirait.

Le choix de l'exception importe autant que sa présence. `ArgumentOutOfRangeException` désigne
précisément la situation : le paramètre existe, il a une valeur, et cette valeur sort du domaine
accepté. Le `nameof(value)` dans le constructeur attache le message au bon paramètre — un
diagnostic qui survit aux renommages, puisque le compilateur suit. Une exception générique ou un
booléen de retour affaibliraient le contrat : le booléen surtout, qui remet à chaque appelant la
charge de vérifier le verdict, exactement ce que la garde devait centraliser.

La frontière est stricte et le contrat la nomme : *seulement* négatif. Zéro passe — une quantité
nulle est légitime dans une commande en cours de construction, une ligne annulée, un panier
vidé — et le refuser serait un excès de zèle qui casserait des appelants corrects. Les cas
cachés encadrent la borne : moins un lève, zéro passe, et les valeurs positives traversent
inchangées — car l'autre moitié du contrat, silencieuse, est là : la fonction rend son entrée
*telle quelle*, sans normalisation, sans plancher, sans surprise. Une garde qui modifie en
passant n'est plus une garde, c'est une transformation déguisée.

Le coût est une comparaison. La transposition est une discipline d'architecture plus qu'une
technique : décider *où* vivent les frontières de validation — entrées d'API, constructeurs,
points d'intégration — y concentrer les gardes, et laisser l'intérieur du système travailler sur
des valeurs prouvées. Un système où la validation est partout est un système où elle n'est
garantie nulle part ; celui où elle est aux frontières se lit, se teste et se fait confiance.
