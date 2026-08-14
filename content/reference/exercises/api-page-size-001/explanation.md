# Explication

Borner la taille de page demandée par un client : deux lignes, et l'une des défenses les plus
importantes d'une API publique. L'énoncé demande de nommer ce qu'une taille non bornée
permettrait avec une seule adresse — la réponse tient en un scénario : `?pageSize=10000000`,
et le serveur matérialise dix millions de lignes — mémoire, base, bande passante — pour un
appel qui a coûté à l'attaquant une ligne de texte. La pagination non bornée est un déni de
service en libre-service ; le plafond est la digue, et il n'est *pas négociable* — c'est le
même principe que le budget d'annulation voisin : la borne d'en face ne se croit pas.

La fonction distingue deux anomalies et les traite différemment, et c'est sa subtilité. La
taille *non positive* — zéro, négatif — reçoit la valeur *par défaut*, vingt : c'est le cas
« le client n'a rien demandé d'utilisable », typiquement un paramètre absent que la liaison a
rempli de zéro, et la réponse servie avec une page raisonnable vaut mieux qu'une erreur pour
une omission banale. La taille *excessive* est rabattue au plafond, cent : la demande est
compréhensible — « donne-m'en beaucoup » — et le serveur honore l'esprit en bornant la lettre.
Deux régimes, deux corrections : le défaut remplace l'absurde, le plafond tempère l'excès. On
pourrait discuter la troisième voie — refuser l'excès par une erreur — et elle se défend pour
des API à contrat strict ; le rabattement silencieux est le choix ergonomique dominant, et il
impose une contrepartie que le contrat de l'API doit documenter : la réponse annonce la taille
*réellement servie*, sans quoi le client croit avoir reçu une page pleine et arrête de
paginer trop tôt.

Les cas de l'énoncé encadrent les trois régimes : la taille raisonnable qui passe telle
quelle, le plafond exact qui passe — la borne est incluse —, le plafond plus un qui redescend
à cent, et le zéro qui devient vingt. Les deux constantes — défaut et plafond — sont le
paramétrage métier de la fonction : dans un service réel, elles viendraient de la
configuration, mais la *structure* — défaut pour l'invalide, minimum contre le plafond —
resterait celle-ci.

Le coût est constant. La transposition est la liste des ressources qu'un client peut demander
en quantité : tailles de page, profondeurs d'inclusion, périodes d'historique, tailles de
lot — chacune mérite son défaut et son plafond, appliqués côté serveur, annoncés dans la
réponse. La règle se retient en une phrase : tout paramètre de volume est une suggestion du
client, jamais un ordre.
