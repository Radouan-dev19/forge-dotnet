# Explication

Une décision d'autorisation se juge sur ce qu'elle fait quand elle manque d'information, et sur ce
qu'elle fait quand deux règles se contredisent. Le cas nominal, lui, est facile.

**Le défaut négatif n'est pas une règle, c'est l'absence de règle.** Il n'y a rien à écrire pour
refuser une portée non accordée : la décision part de la négative et seule une correspondance
explicite la déplace. C'est ce qui distingue une autorisation d'un filtre. Un filtre qui oublie un cas
laisse passer ; une autorisation qui oublie un cas refuse. Cette asymétrie doit être visible dans la
structure du code, pas seulement dans son résultat, sinon la prochaine personne qui ajoute une branche
choisira le mauvais défaut.

**La contradiction se tranche par le refus, indépendamment de l'ordre.** Conclure sur la première
entrée qui correspond paraît naturel et introduit une faille : la décision dépend alors de l'ordre des
entrées, or cet ordre vient du jeton, donc de l'extérieur. Il suffirait de réémettre un jeton dont les
portées sont rangées autrement pour contourner un refus. Le refus prioritaire supprime cette prise :
quelle que soit la façon dont le jeton est écrit, la même décision en sort.

D'où les deux asymétries du parcours, qui ne sont pas symétriques et c'est le cœur du sujet. Un refus
qui correspond permet de conclure tout de suite : rien de ce qui suit ne peut le contredire. Une
autorisation qui correspond ne permet **rien** de conclure — elle ouvre une voie qu'un refus rencontré
plus loin peut encore fermer. Il faut donc continuer à lire.

**La casse n'est pas une commodité d'écriture.** Une portée est un identifiant, comme une clé
primaire. Comparer sans tenir compte de la casse crée des droits qui n'ont jamais été émis : deux
graphies deviennent le même droit, et une revue d'habilitations ne peut plus énumérer ce qui est
réellement accordé.

**Le générique de famille couvre ce qui suit les deux points, pas la famille seule.** La nuance
compte : conserver les deux points dans le préfixe comparé évite d'accorder une portée que personne n'a
nommée, et évite aussi qu'une famille dont le nom commence par les mêmes lettres soit couverte par
accident. Une longueur strictement supérieure à celle du préfixe assure qu'il reste quelque chose
après les deux points.

Le coût est linéaire dans la taille du jeton, avec une comparaison de préfixe par entrée. Rien n'est
alloué : la décision se prend en un seul passage, ce qui compte pour une vérification exécutée à
chaque requête.
