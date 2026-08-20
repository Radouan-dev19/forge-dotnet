# Explication

Cette fonction est le poste d'aiguillage des requêtes conditionnelles : selon l'en-tête présent et
la correspondance des empreintes, elle rend l'un de quatre statuts. Sa difficulté n'est pas le
calcul — trois comparaisons — mais de garder distincts deux mondes qu'on confond : la lecture
conditionnelle, qui fait de la performance, et l'écriture conditionnelle, qui fait de la
correction.

La lecture est gouvernée par `If-None-Match`, et son intention est « ne m'envoie que si j'ai une
copie périmée ». Si l'empreinte que le client possède égale l'empreinte courante, il est déjà à
jour : le serveur répond **304**, sans corps, et le client réutilise sa copie — un transfert
économisé. Sinon, **200** avec la représentation. C'est de l'optimisation : le pire cas d'une
erreur ici est un transfert de trop, pas une donnée corrompue.

L'écriture est gouvernée par `If-Match`, et son intention est tout autre : « n'applique ma
modification que si l'état est encore celui sur lequel je l'ai fondée ». C'est la concurrence
optimiste de la leçon EF Core, remontée dans HTTP. Trois issues, et l'ordre compte. D'abord
l'absence de condition : une écriture sans `If-Match` est refusée par **428**, car l'appliquer
rouvrirait la mise à jour perdue que tout le mécanisme existe pour fermer — c'est le point le plus
contre-intuitif, refuser une écriture *parce qu'*elle ne pose aucune condition. Ensuite, condition
présente et correspondante : l'état n'a pas bougé, l'écriture procède, **200**. Enfin, condition
présente mais non correspondante : quelqu'un a écrit entre-temps, et le **412** refuse
l'écrasement. Le 412 est le pendant HTTP du `DbUpdateConcurrencyException` : le conflit est
technique, sa résolution — relire et refaire, ou demander à l'utilisateur — est métier.

L'erreur qui guette est la confusion des deux mondes : rendre 304 sur une écriture ou 412 sur une
lecture, en consultant le mauvais en-tête. La branche par méthode, en tête de fonction, existe
pour rendre cette séparation impossible à rater — l'en-tête consulté découle de la nature de la
requête, pas l'inverse. Le second piège est la non-correspondance traitée comme un succès « parce
qu'une condition a été fournie » : fournir une condition ne suffit pas, il faut qu'elle
*corresponde*, et le cas caché de l'écriture concurrente le vérifie.

Les comparaisons sont exactes, guillemets compris : les guillemets font partie de l'ETag, et les
ignorer confondrait un ETag fort avec un ETag faible de même corps.

Le coût est constant. La transposition est la distinction performance/correction : partout où un
mécanisme sert les deux — un cache qui accélère et un verrou qui protège —, il faut savoir lequel
est en jeu, car une erreur de performance coûte du temps quand une erreur de correction corrompt
des données. Le 428 en est le rappel : mieux vaut refuser une écriture non protégée que la laisser
écraser en silence.
