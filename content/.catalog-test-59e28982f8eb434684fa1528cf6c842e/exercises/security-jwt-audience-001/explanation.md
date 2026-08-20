# Explication

Le contrôle d'audience répond à une question que la signature ne pose pas : ce jeton, authentique
et valide, m'était-il destiné ? Un émetteur unique sert souvent plusieurs services — l'API de
consultation, celle d'administration, un service de rapports. Tous vérifient la même signature,
puisque la clé est celle de l'émetteur. Sans contrôle d'audience, un jeton obtenu légitimement pour
le service le moins sensible ouvre tous les autres : c'est le rejeu croisé, et il ne demande à
l'attaquant aucune falsification, seulement un jeton volé au bon endroit et présenté au mauvais.

La difficulté technique de l'exercice est ailleurs : elle tient au polymorphisme de la
revendication. La norme autorise `aud` sous deux formes — une chaîne quand le jeton vise un seul
service, un tableau de chaînes quand il en vise plusieurs — et les émetteurs réels utilisent les
deux, parfois selon le nombre de destinataires du moment. Un vérificateur qui ne gère que la forme
chaîne fonctionnera parfaitement en développement, où l'émetteur de test n'émet qu'une audience,
puis rejettera en production tous les jetons multi-services. C'est un bug de compatibilité
silencieux, découvert au pire moment. `JsonElement.ValueKind` rend la distinction explicite et
force à écrire les deux chemins, plus le troisième : toute autre forme JSON est un refus, car un
émetteur qui met un nombre dans `aud` ne respecte pas le contrat, et deviner son intention n'est
pas le rôle d'un vérificateur.

Deux choix de rigueur complètent la règle. La comparaison est stricte et sensible à la casse :
`forge-api` et `Forge-API` sont deux identifiants différents, et une correspondance laxiste
recréerait le rejeu croisé entre services aux noms voisins — précisément ce que le contrôle doit
empêcher. Et l'absence de la revendication est un refus, pas une valeur par défaut : un jeton sans
destinataire déclaré n'est destiné à personne. La tentation inverse — « pas d'audience, donc pas de
restriction » — inverse la charge de la preuve et transforme l'oubli d'un émetteur en faille du
vérificateur.

Un dernier mot sur les éléments non textuels d'un tableau : ils s'ignorent, ils ne font pas
échouer. La nuance est défendable dans les deux sens, mais l'option retenue est la plus robuste en
pratique — un émetteur qui ajoute demain un élément structuré à son tableau ne doit pas casser la
correspondance des éléments valides qui s'y trouvent déjà. L'important est que ce choix soit écrit
dans le contrat de la méthode et éprouvé par un cas, ce que fait cet exercice : le comportement aux
bords n'est jamais un détail d'implémentation, c'est une décision.
