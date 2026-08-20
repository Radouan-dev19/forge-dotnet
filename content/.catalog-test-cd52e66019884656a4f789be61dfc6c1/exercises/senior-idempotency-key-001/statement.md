# Distinguer un traitement d'un rejeu par cle d'idempotence

Implementez `Submission.ProcessOutcomes(string keys)`.

Un client renvoie parfois la meme requete, munie de la meme cle d'idempotence, apres un delai ou
une coupure reseau. Le serveur doit executer l'effet une seule fois par cle et se contenter de
rejouer la reponse ensuite.

La chaine `keys` liste les cles reues, dans l'ordre, separees par le point-virgule. Les segments
vides sont ignores et ne comptent pas comme des cles.

Regles exactes :

- la premiere fois qu'une cle apparait, le verdict est `processed` ;
- toute apparition ulterieure de cette meme cle donne `replayed` ;
- rendez les verdicts joints par le point-virgule, dans l'ordre des cles ;
- une entree vide, ou uniquement composee de segments vides, rend la chaine vide ;
- une entree nulle leve `ArgumentNullException`.

Ecrivez avant le code : une cle unique, la meme cle repetee trois fois, et deux cles entrelacees.

Exemple : entree `["a;b;a"]`, sortie `"processed;processed;replayed"`.
