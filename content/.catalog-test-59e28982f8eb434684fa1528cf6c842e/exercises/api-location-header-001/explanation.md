# Explication

Après une création, la réponse porte l'adresse de la nouveauté — c'est l'en-tête de
localisation qui accompagne le 201 — et cette fonction en construit la valeur. Deux décisions
tiennent l'exercice : la forme de l'adresse, et le refus qui la précède.

La forme d'abord : une route *relative*, `/orders/42`, sans schéma ni hôte. L'énoncé demande de
nommer ce qu'une adresse absolue casserait, et la liste est longue : le passage d'un
environnement à l'autre — l'hôte de développement codé en dur pointerait vers nulle part en
production —, la terminaison TLS en frontal — le service interne voit du HTTP, le client du
HTTPS, et l'adresse absolue fabriquée à l'intérieur ment sur le schéma —, les passerelles et
préfixes de routage qui réécrivent les chemins. La route relative laisse chaque maillon
résoudre l'adresse dans *son* contexte : c'est l'information minimale suffisante, et le choix
par défaut de toute API derrière un frontal. L'adresse absolue se justifie dans des cas
précis — liens croisés entre domaines — et alors elle se construit depuis la requête entrante,
jamais depuis une constante.

Le refus ensuite : un identifiant nul ou négatif lève avant toute composition. La raison est
plus forte qu'une simple hygiène de domaine — cette fonction s'exécute *après* une création
réussie, et son identifiant vient de la persistance. S'il est invalide ici, c'est que
l'insertion a menti ou que le mauvais champ a été passé : une adresse `/orders/0` publiée dans
une réponse serait un lien mort distribué au client, qui le rangera et le suivra plus tard,
loin du bug d'origine. Lever immédiatement rattache le symptôme à sa cause. La plus petite
valeur acceptée — un — passe, et le cas caché posé sur cette borne fige l'inclusivité.

L'interpolation `$"/orders/{id}"` formate l'entier en invariant de fait — les entiers ne
portent pas de séparateurs culturels ici — et compose la seule forme stable : préfixe de
collection, identifiant. La stabilité de cette forme est un contrat vis-à-vis des clients qui,
malgré toutes les recommandations, construisent des adresses à la main.

Les cas cachés suivent l'énoncé : l'ordinaire, la borne minimale, le zéro et le négatif qui
lèvent.

Le coût est trivial ; la transposition ne l'est pas : toute adresse publiée par une API —
localisation de création, liens de pagination, références croisées — pose les deux mêmes
questions. Relative ou absolue, et selon quel contexte ? Et : que vaut l'identifiant que
j'insère, et d'où vient-il ? Une API qui publie des adresses fausses érode la seule chose
qu'elle a à offrir — la confiance dans ses réponses.
