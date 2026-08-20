# Explication

Décider si un changement est cassant est la décision qui gouverne tout le versionnage : seule
elle oblige à incrémenter, et la rater dans un sens fabrique des régressions chez des
consommateurs qu'on ne verra jamais, dans l'autre multiplie les versions sans raison. La fonction
est une correspondance sur étiquette, et sa vraie subtilité est le sens du défaut.

Le principe qui range les cas est l'*asymétrie ajouter/retirer/restreindre*. Un appel déjà en
place chez un consommateur continue de fonctionner si l'on se contente d'ajouter à côté :
un champ d'entrée facultatif qu'il ne fournit pas garde le comportement d'avant, un champ de
sortie qu'il ne lit pas ne le gêne pas, un point d'accès qu'il n'appelle pas ne l'affecte pas.
En revanche, retirer ce qu'il utilise, ou restreindre ce qu'il envoyait, casse son appel. La
liste blanche des trois cas sûrs n'est donc pas arbitraire : ce sont exactement les additions
strictes, les seules opérations qui préservent les appels existants. Le renommage, souvent cru
anodin, est le contre-exemple à retenir — il *retire* l'ancien nom et *ajoute* le nouveau, donc
il casse doublement, et il n'a pas sa place dans les cas sûrs.

Le cœur de l'exercice est le sens du défaut : tout ce qui n'est pas explicitement sûr est
cassant, *l'inconnu compris*. C'est une décision de sécurité, pas de commodité. Présumer
« compatible sauf preuve du contraire » laisse une étiquette nouvelle — un changement qu'on n'a
pas encore classé — filer en production comme non cassant, et la régression se découvre chez les
consommateurs. La présomption de danger inverse le risque : au pire, on crée une version dont on
n'avait pas strictement besoin — coût maîtrisé, visible, interne — plutôt que de casser
silencieusement. Le cas caché à l'étiquette inconnue verrouille précisément ce défaut, et c'est
lui qui distingue une implémentation prudente d'une implémentation optimiste.

La normalisation en tête — rognage, casse invariante — traite l'étiquette comme l'identifiant
technique qu'elle est : `Add-Optional-Input` doit valoir `add-optional-input`, et l'invariant
garantit le même verdict quelle que soit la machine. L'étiquette absente ou blanche tombe
naturellement du côté cassant par le défaut, sans garde spéciale — l'absence d'information n'est
pas une information de sûreté.

Le coût est constant. La transposition dépasse HTTP : toute évolution d'un contrat partagé —
schéma de base de données consommé par d'autres, format de message d'une file, signature d'une
bibliothèque publiée — pose la même question, avec la même asymétrie et la même présomption. La
règle tient en une phrase à garder en revue : on peut presque toujours ajouter sans casser,
presque jamais retirer ni restreindre — et dans le doute, c'est cassant.
