# Explication

Cet exercice encode une these que le parcours defend partout : le decoupage en services n'est pas un
progres en soi, c'est un cout que l'on paie contre une frontiere reelle. Un junior qui decoupe par
reflexe fabrique un systeme distribue dont il devra ensuite exploiter les pannes partielles, la
latence reseau et la coherence eventuelle, sans y avoir rien gagne. La decision par defaut, ici,
est donc de garder le monolithe, et l'extraction est l'exception qu'il faut justifier par des
signaux concrets.

Les trois entrees sont ces signaux, et leur combinaison compte plus que chacune prise isolement.
Plusieurs equipes qui se marchent dessus sur le meme deployable est un vrai motif de separation :
chacune veut livrer a son rythme. Mais ce motif ne suffit pas seul. Si les deploiements restent
couples, extraire un service ne rend pas les livraisons independantes : on obtient deux artefacts
qui doivent toujours sortir ensemble, c'est-a-dire le pire des deux mondes. Et si des tables sont
partagees, la frontiere que l'on croit tracer passe en realite au milieu des donnees : le service
extrait restera couple par la base, un couplage cache et bien plus dangereux que l'appel de methode
qu'il remplace.

Le noyau decidable ordonne ces conditions de facon a ce que le refus l'emporte des qu'un signal
manque. Une seule equipe : on garde. Des tables partagees : on garde, meme avec dix equipes. Ce
n'est qu'avec plusieurs equipes, aucune table partagee et aucun deploiement couple que la fonction
rend extract-service. Cet ordre traduit une prudence : chaque condition de refus est verifiee avant
la seule condition d'extraction.

Les cas caches eprouvent precisement les combinaisons ou l'on est tente d'extraire trop vite. Trois
equipes qui partagent deux tables doivent rester monolithe, alors que le nombre d'equipes pousse
dans l'autre sens ; c'est le piege classique du decoupage premature. La validation des compteurs
protege contre des entrees absurdes, une equipe nulle ou un compte negatif, qui n'ont pas de sens
metier et signalent un appel fautif plutot qu'une decision.

Le cout d'une mauvaise decision est asymetrique. Garder un monolithe qui aurait pu etre decoupe se
corrige plus tard, quand les signaux deviennent nets. Decouper trop tot cree une dette distribuee
que l'on ne rembourse presque jamais : recoller deux services est bien plus rare que d'en separer un.
La bonne reponse en entretien senior n'est donc pas de savoir decouper, c'est de savoir refuser de
decouper avec des arguments, et ce noyau entraine exactement ce reflexe.
