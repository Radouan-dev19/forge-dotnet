# Explication

Retourner le même message public pour ne révéler ni l'existence du compte ni la nature de l'échec.

Deux messages distincts transforment un point de connexion en outil d'énumération : un tiers apprend quels comptes existent avant même de tenter un mot de passe. L'uniformité du message est donc un contrôle de sécurité, pas une pauvreté d'ergonomie.

Elle doit être complète pour valoir quelque chose. Un message identique accompagné d'un statut différent, ou d'un temps de réponse différent parce qu'on ne calcule pas d'empreinte quand le compte n'existe pas, divulgue la même information par un autre canal. Le détail réel part au journal serveur, jamais dans la réponse. La décision est en temps constant.
