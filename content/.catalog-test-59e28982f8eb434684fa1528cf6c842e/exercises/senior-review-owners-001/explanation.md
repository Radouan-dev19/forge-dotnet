# Explication

Le fichier de propriété des chemins est l'un des rares mécanismes qui rendent la revue de code
**structurelle** au lieu de sociale : ce n'est plus l'auteur qui choisit ses relecteurs — avec le
biais évident vers les plus conciliants — mais la carte du dépôt qui les désigne. La résolution de
cette carte a trois règles, et chacune répond à une dérive observée.

**Le préfixe le plus long gagne, parce que la spécificité est la compétence.** Les propriétés
s'imbriquent naturellement : une équipe répond du répertoire des sources, une personne répond du
sous-répertoire de l'interface de paiement. Quand un fichier tombe sous les deux, convoquer les deux
double le coût de chaque demande et dilue la responsabilité — chacun suppose que l'autre relira
vraiment. Le plus long préfixe désigne la propriété la plus proche du code, celle qui connaît ses
invariants ; le propriétaire englobant reste la solution de repli pour tout ce que la propriété fine
ne couvre pas. Le cas du fichier exact pousse la règle au bout : le fichier hérité sensible, au
milieu du répertoire d'une équipe, peut porter son gardien attitré — sa propriété est le préfixe le
plus long possible, elle bat tout répertoire.

**La zone grise se refuse, parce qu'elle est le trou du mécanisme.** Un fichier qu'aucun préfixe ne
couvre serait fusionnable sans relecteur exigé — et les dérives vont précisément là : le nouveau
répertoire créé sans mise à jour de la carte devient la zone où tout passe. Refuser la résolution
force la mise à jour de la carte au moment où le trou apparaît, pas six mois après, à l'occasion
d'un incident. C'est la même politique que la porte de déploiement du socle : le silence de
l'instrument ne vaut jamais autorisation.

**La convocation se déduplique et se trie, parce qu'elle s'adresse à des humains.** La même personne
propriétaire de trois fichiers touchés reçoit une convocation, pas trois : la revue porte sur la
demande, pas sur les fichiers un à un, et trois notifications pour un même travail apprennent à
ignorer les notifications. Le tri ordinal, lui, rend la sortie stable — la liste des relecteurs
exigés se compare entre outils et entre exécutions, comme tout ce que ce parcours produit.

**La barre oblique finale n'est pas décorative.** Le préfixe de répertoire sans son séparateur
couvrirait tous les chemins qui partagent un début de nom — les sources et les sources-annexes — et
convoquerait des propriétaires sur du code qui ne les concerne pas. La convention est la même que
pour l'invalidation de cache des images : la portée d'un chemin se déclare sans ambiguïté ou ne se
déclare pas.

En entretien, ce mécanisme se nomme code owners, et la question type porte sur la zone grise :
« que fait votre politique d'un fichier que personne ne possède ? ». La réponse — le refus qui force
la carte à jour — vaut pour tout mécanisme de gouvernance : les trous se ferment à l'écriture, pas à
l'incident.
