# Livraison conteneurisée locale S19–S20

L’image utilise des bases épinglées, un runtime non-root et un contexte borné. Compose publie seulement sur la boucle locale, monte les preuves d’authentification depuis des fichiers hors Git, supprime les capacités, active `no-new-privileges`, limite mémoire, CPU et PID, rend le système de fichiers en lecture seule et ajoute un health check.

```powershell
$env:FORGE_OPERATOR_KEY_FILE = 'C:\chemin\hors-git\operator-key.txt'
$env:FORGE_READER_KEY_FILE = 'C:\chemin\hors-git\reader-key.txt'
docker compose -f content/labs/container-delivery/compose.yaml config
docker compose -f content/labs/container-delivery/compose.yaml up --build --wait
docker compose -f content/labs/container-delivery/compose.yaml down --volumes
```

Utilisez uniquement des valeurs factices dans un bac à sable. Ne copiez jamais un secret réel dans le dépôt ou les logs.
