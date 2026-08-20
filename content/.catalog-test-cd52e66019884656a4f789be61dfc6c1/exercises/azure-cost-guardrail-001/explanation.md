# Explication

Avant de créer une ressource facturable, deux questions : combien par jour, et comment ça se
supprime ? La garde de coût de cet exercice les pose dans cet ordre — et l'ordre est la
leçon.

Le plan de suppression prime, et la question de l'énoncé dit pourquoi : que devient une
ressource créée sans date de fin au bout de quelques mois ? Un poste de facture que personne ne
sait expliquer. L'infrastructure d'essai oubliée est le premier gaspillage du nuage — la
machine du prototype abandonné, la base du test de charge d'il y a deux trimestres, le stockage
de la démonstration — chacune petite, leur somme considérable, et leur suppression paralysée
par la peur : « quelqu'un s'en sert peut-être encore ». Le plan de suppression *préalable* —
qui supprime, quand, comment on vérifie — coûte cinq minutes au moment où le contexte est
frais, et sa version outillée — une date d'expiration étiquetée sur la ressource, un
nettoyage automatique qui s'y fie — transforme la discipline en mécanique. D'où la garde en
tête : sans plan, blocage, même si le coût estimé est nul — car « gratuit aujourd'hui » ne
décrit pas la trajectoire d'une ressource immortelle.

Le budget ensuite, comparé au coût *estimé* — la garde s'exécute avant la création, sur une
estimation, et c'est sa force : le contrôle après facturation constate, le contrôle avant
création empêche. La borne est *incluse* — au budget exact, l'autorisation passe : le budget
est une enveloppe, pas un seuil d'alerte — et le cas caché posé dessus fige l'inclusivité. Les
invariants d'entrée lèvent : un coût négatif ne s'estime pas, un budget nul ou négatif n'est
pas une enveloppe — la validation des mesures avant la politique, régime constant du
catalogue.

Deux verdicts nommés — `allow`, `block` — plutôt qu'un booléen : le verdict s'insère dans une
chaîne d'approbation où d'autres états pourraient exister — un « escalade » pour les
dépassements justifiables — et le vocabulaire nommé s'étend sans casser les consommateurs.

Le coût d'exécution est constant ; le coût évité ne l'est pas. La transposition dépasse le
nuage : toute acquisition de ressource à coût récurrent — abonnements, licences, certificats,
noms de domaine — mérite sa garde à deux questions, l'enveloppe et la sortie. La règle se
retient par son inversion : on ne demande pas « peut-on se le payer ? » mais « sait-on s'en
débarrasser ? » — la première question a toujours une réponse optimiste, la seconde ne ment
pas.
