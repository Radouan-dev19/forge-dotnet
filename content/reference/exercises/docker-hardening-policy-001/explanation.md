# Explication

Exiger simultanément identité non privilégiée, racine en lecture seule et interdiction d'élévation.

Les trois sont indissociables, et le troisième est celui qu'on oublie. Sans interdiction d'élévation, un exécutable portant un bit d'élévation peut faire remonter les droits du processus pendant l'exécution, y compris s'il a démarré sans privilège : le premier réglage devient contournable, donc décoratif.

La racine en lecture seule ferme la persistance : ce qui doit s'écrire est monté explicitement et borné, et toute écriture inattendue devient une erreur immédiate plutôt qu'une modification durable. Un conteneur isole des processus, il ne les enferme pas : c'est la conjonction de ces réglages qui rapproche l'isolation de ce qu'on en attend. La décision est en temps constant.
