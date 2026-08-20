# Kit carrière local — preuves, pas promesses

Ces supports transforment des apprentissages en faits vérifiables. Ils ne promettent ni emploi, ni
entretien, ni niveau de salaire. Une activité pédagogique reste nommée comme telle et ne devient
jamais une expérience professionnelle inventée.

## Comment ce contenu est servi — décision d'architecture

Chaque guide porte un manifeste plat (`career-<nom>-001.json`, schéma `career.schema.json`) qui
référence son Markdown. Le type de document `CareerGuide` a été retenu plutôt qu'une page statique,
pour trois raisons : le validateur applique aux guides les mêmes règles d'authenticité qu'au reste
du catalogue (marqueurs, clones, HTML brut) ; la règle de joignabilité de
`ContentReachabilityWebTests` couvre le type automatiquement — un guide sans route ferait échouer
le build ; et l'index `/career` se construit depuis le catalogue, donc un guide ajouté demain est
servi sans code nouveau. Aucune page de ce kit ne produit de preuve de maîtrise.

## Données personnelles

Les données d'un CV et d'un suivi de candidature sont personnelles. Travaillez dans une copie locale
exclue de Git, minimisez les champs, retirez adresse complète, date de naissance, identifiants
privés et noms de tiers, puis inspectez métadonnées et historique avant toute publication.

Le fichier d'exemple utilise uniquement des personnes et organisations fictives.
`Export-CareerEvidence.ps1` refuse les champs qui ressemblent à des coordonnées directes — adresse
de courriel ou numéro de téléphone — et génère un Markdown local à l'emplacement explicitement
choisi.
