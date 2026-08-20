# Autoriser l'accès à une route depuis un jeton

Implémentez
`Submission.GuardDecision(string token, string requiredScope, int nowUnix, string currentPath)`.

La méthode est le garde de route d'une application : à partir d'un jeton JWT, du droit exigé par la
route, de l'instant courant et du chemin demandé, elle rend l'une de trois décisions exactes :
`"allow"`, `"forbidden"`, ou `"redirect:login?return=CHEMIN"` où CHEMIN est `currentPath`.

Ce garde s'exécute **après** la validation cryptographique de la signature, faite ailleurs. Il ne
revérifie pas la signature : il se contente de lire les revendications d'un jeton déjà réputé
authentique quant à son intégrité.

Décodage du jeton : découpez-le sur le point. Il doit avoir exactement trois segments. Décodez le
segment du milieu en base64url, c'est-à-dire du base64 où `+` devient `-`, `/` devient `_`, et le
remplissage `=` de fin est retiré. Analysez ensuite le texte obtenu comme du JSON et lisez deux
revendications : `exp`, un entier en secondes Unix, et `scope`, une liste de droits séparés par des
espaces.

Décisions, dans cet ordre :

- si `token` est absent, ou si le décodage échoue à n'importe quelle étape (nombre de segments
  différent de trois, base64url invalide, JSON invalide, revendication `exp` absente), ou si le
  jeton est expiré, c'est-à-dire `exp` inférieur ou égal à `nowUnix` : rendez
  `"redirect:login?return=CHEMIN"` ;
- sinon, si les droits de `scope` ne contiennent pas `requiredScope` par correspondance exacte :
  rendez `"forbidden"` ;
- sinon : rendez `"allow"`.

Un `requiredScope` absent ou un `currentPath` absent lève `ArgumentNullException`. Un `token` absent,
lui, décrit un utilisateur non authentifié et conduit à la redirection.

Écrivez avant le code : un jeton valide au bon droit, un jeton expiré, un jeton valide au mauvais
droit, et une chaîne qui n'est pas un jeton.

Exemple : entrée
`["eyJhbGciOiJub25lIn0.eyJleHAiOjIwMDAsInNjb3BlIjoib3JkZXJzLnJlYWQifQ.sig", "orders.read", 1000, "/orders"]`,
sortie `"allow"`.
