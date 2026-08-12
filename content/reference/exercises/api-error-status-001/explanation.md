# Explication

Mettre les erreurs connues sur liste fermée et rabattre le reste vers l'erreur interne.

Une correspondance sur des catégories venues du domaine doit être exhaustive dans un sens seulement : ce qui est connu reçoit son statut, tout le reste tombe dans le repli. Ce repli est délibérément le statut d'erreur interne, qui n'apprend rien à l'appelant — c'est le comportement voulu, puisqu'un défaut non prévu ne doit rien divulguer de sa cause.

La normalisation préalable évite qu'une différence de casse ou un blanc de bordure fasse basculer une erreur connue dans le repli, ce qui transformerait une réponse actionnable en erreur opaque. La décision est en temps constant une fois la normalisation faite.
