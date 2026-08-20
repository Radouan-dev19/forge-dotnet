# Explication

Le seau de jetons est la forme de limitation la plus expressive parce qu'il règle deux choses
indépendamment : le débit moyen, par la cadence de recharge, et la rafale tolérée, par la
capacité. Cet exercice isole la transition d'état — de combien de jetons on part, de combien on
arrive — et sa subtilité tient tout entière dans l'endroit où s'applique le plafond.

Le plafond porte sur la *recharge*, pas sur le solde final. Un seau resté longtemps inactif
aurait, sans plafond, accumulé une recharge proportionnelle à la durée d'inactivité — de quoi
envoyer, d'un coup, bien plus que la rafale prévue. Le plafonnement à la capacité au moment de la
recharge est exactement ce qui borne la rafale : le seau se remplit jusqu'au bord et pas au-delà,
si bien qu'un client inactif retrouve un seau plein — sa rafale maximale — mais jamais davantage.
L'erreur classique plafonne le solde *après* consommation, ce qui a un effet pervers subtil :
elle empêche de consommer le dernier jeton d'un seau plein, car le disponible n'a jamais été
calculé correctement. L'ordre recharge-plafond-puis-consommation est donc porteur de sens, pas
cosmétique.

L'admission est le second geste : on ne consomme un jeton que s'il en reste. Un seau vide refuse
l'appel *sans* toucher au solde — surtout pas en le rendant négatif. Le solde négatif serait un
état interdit qui fausserait la prochaine recharge et pourrait, selon les implémentations, laisser
passer des appels par accident arithmétique. La règle « refuser sans consommer » garde le solde
dans son domaine légal, entre zéro et la capacité. Le cas caché du seau vide vérifie précisément
que le refus laisse le solde à zéro.

L'arithmétique en 64 bits avant le plafond est la précaution habituelle : sur une très longue
inactivité, `tokensBefore + refilled` pourrait déborder un entier de 32 bits avant même d'être
plafonné, et un solde enroulé en négatif passerait le test d'admission à tort. Élargir puis
plafonner ramène la valeur dans le domaine du type de retour sans risque.

La validation refuse une capacité nulle ou négative — un seau sans capacité n'a pas de sens — et
une recharge négative, qui décrirait une fuite de jetons plutôt qu'un remplissage.

Le coût est constant. La transposition est la modélisation par état borné : un compteur qui se
remplit et se vide, avec un plafond qui définit la réserve maximale, décrit aussi bien un crédit
d'appels qu'un budget de tentatives, une file à capacité, un stock tampon. La question à se poser
est toujours la même : où s'applique le plafond, et que fait-on quand la réserve est épuisée ?
Le seau de jetons y répond proprement, et c'est pour cela qu'il est le modèle des passerelles.
