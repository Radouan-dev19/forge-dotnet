# Explication

Le formulaire de connexion est l'interface la plus attaquée d'un système, et son risque principal ne
demande aucun exploit : il suffit de lire les réponses. Chaque nuance de message est un bit
d'information, et l'énumération de comptes — savoir quelles adresses existent — se construit
entièrement avec ces bits. Cet exercice pousse la version adulte de la règle « message uniforme » :
l'uniformité a un périmètre précis, et le journal n'y est pas soumis.

**Où passe exactement la frontière de l'uniformité.** La règle naïve — « toujours le même message » —
est à la fois trop faible et trop forte. Trop faible si on l'applique aux seuls cas évidents : le
verrouillage annoncé publiquement confirme l'existence du compte aussi sûrement qu'un « compte
inconnu », puisque seul un compte existant se verrouille. Trop forte si on l'applique après la preuve
du mot de passe : l'appelant qui a fourni le mot de passe correct mais périmé a démontré qu'il est le
titulaire — lui cacher l'expiration ne protège personne et le laisse devant un échec inexplicable,
qu'il résoudra en appelant le support, c'est-à-dire en coûtant cher. La frontière est donc la preuve :
tout ce qui précède la vérification réussie du mot de passe s'uniformise, ce qui vient après peut se
nommer.

**Pourquoi le journal dit tout quand le public ne dit rien.** L'uniformité appliquée au journal
serait une faute symétrique : les défenseurs ont besoin des causes exactes pour distinguer un
utilisateur qui se trompe — quelques mots de passe erronés sur un compte connu — d'une pulvérisation
de mots de passe — un mot de passe unique essayé sur des milliers de comptes, connus ou non. Les deux
attaques ont la même face publique et des signatures de journal opposées. Fusionner les causes dans
le journal, c'est aveugler la détection pour un bénéfice nul : l'attaquant ne lit pas le journal.

**Pourquoi le verrouillage prime sur la validité du mot de passe.** Évaluer le mot de passe d'un
compte verrouillé avant le verrou aurait deux conséquences : le succès apparent d'un compte gelé —
contradiction dangereuse — et, plus subtil, un canal d'information temporel si le coût de
vérification diffère. L'ordre strict de la cascade n'est pas une élégance : chaque inversion possible
correspond à une fuite précise.

**Pourquoi la réponse est une paire et pas deux fonctions.** Produire les deux faces au même endroit
garantit leur cohérence : la cause du journal correspond toujours à la décision publique rendue, au
même instant, sur les mêmes attributs. Deux chemins de code séparés finissent par diverger — un
correctif sur l'un, pas sur l'autre — et une divergence entre ce que le système dit et ce qu'il
journalise est le pire terrain d'analyse d'incident.

La transposition dépasse la connexion : réinitialisation de mot de passe, invitation à un espace,
vérification d'adresse — toute interface qui répond « existe ou pas » à un anonyme mérite la même
frontière entre face publique uniforme et journal précis.
