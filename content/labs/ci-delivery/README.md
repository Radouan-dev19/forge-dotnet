# Pipeline CI locale S20

`workflow.yml` limite les permissions à la lecture, construit et teste avant la répétition de livraison, publie un rapport borné et ne reçoit aucun secret. Les actions officielles sont référencées par version majeure ; dans une organisation, la politique de dépendances peut imposer leurs SHA autorisés.

`verify-ci.ps1` exécute localement les mêmes commandes de compilation, tests et image, en s’arrêtant au premier code de sortie non nul.
