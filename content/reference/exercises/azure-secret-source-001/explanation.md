# Explication

Une valeur non sensible reste en configuration ; une valeur sensible passe par l'identité gérée ou un magasin local hors dépôt.

La sensibilité tranche en premier, et le critère est net : faudrait-il changer cette valeur si elle devenait publique ? Non pour une adresse ou un délai, oui pour une clé. Placer une valeur banale dans un magasin de secrets ajoute un coût d'exploitation sans rien protéger, et brouille le signal pour les valeurs qui comptent.

Le second critère est contextuel. En déploiement, une identité attestée par la plateforme supprime le secret lui-même — son stockage, sa distribution, son renouvellement. En développement local, aucune plateforme n'atteste rien : un magasin local hors dépôt est la seule réponse honnête. La décision est en temps constant.
