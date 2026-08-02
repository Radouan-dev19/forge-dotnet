# Incrément 04C — Runner Docker

## 1. Statut

Validé le 28 juillet 2026.

## 2. Objectif

Exécuter compilation et tests C# dans un conteneur éphémère fortement isolé, avec quotas, nettoyage garanti et mode manuel honnête.

## 3. Contexte à lire

Intégralité de `SECURITY.md`, stratégie runner d'`ARCHITECTURE.md`, `CONTENT_GUIDE.md`, `ROADMAP.md`, fiches `04A`/`04B`.

## 4. Prérequis

`04B` validé ; Docker fonctionnel ; contrat runner gelé ; revue de menace préparée.

## 5. Périmètre inclus

Image .NET minimale épinglée par digest, création conteneur, réseau none, rootfs lecture seule, tmpfs, non-root, capabilities supprimées, no-new-privileges, quotas CPU/mémoire/PID/disque/temps/sortie, concurrence bornée, commande en liste blanche, nettoyage et export manuel.

## 6. Périmètre explicitement exclu

SqlLab, Kubernetes/cloud, exécution dans Web, montage socket Docker dans le conteneur, contenu des 10 exercices et prétendue isolation absolue.

## 7. Fichiers ou projets principalement concernés

`ForgeDotNet.CodeRunner`, image/Dockerfile dédié, composition Web, tests sécurité/Integration, `SECURITY.md`, runbook.

## 8. Étapes d'implémentation

Mettre à jour menace ; préparer workspace aléatoire confiné ; construire arguments sans shell ; lancer image par digest avec tous contrôles ; compiler puis tester séparément ; borner/censurer sortie ; tuer sur timeout/annulation ; supprimer conteneur/fichiers en `finally` ; limiter concurrence ; implémenter mode manuel sans preuve automatique ; inspecter options effectives.

## 9. Règles d'architecture

Adaptateur Docker derrière `ICodeRunner` ; aucun détail Docker dans Domain/Web ; runner hors processus web ; chaque tentative isolée et corrélée par ID opaque.

## 10. Règles de sécurité

Revue renforcée obligatoire : réseau désactivé ; non-root ; aucune capability/device/socket/secret ; rootfs RO ; seul tmpfs/workspace dédié ; commandes/arguments whitelistés ; quotas effectifs ; image scannée ; logs sans code ; nettoyage après succès, crash et redémarrage ; fail closed si contrôle absent.

## 11. Tests à écrire

Programme normal, compilation/test KO, boucle infinie, mémoire, process/fork bomb, output bomb, disque, réseau, traversal, lecture hôte, env secret, subprocess interdit, annulation, concurrence, conteneur/fichier orphelin, indisponibilité Docker et mode manuel non validant.

## 12. Tests manuels à effectuer

Exécuter batterie d'abus sur machine cible ; `docker inspect` pour chaque contrôle ; couper Docker pendant tentative ; contrôler `docker ps -a`, fichiers temporaires, logs et absence de secrets.

## 13. Commandes de vérification

```powershell
docker build --pull --no-cache -t forge-dotnet-runner:test <chemin-runner>
docker image inspect forge-dotnet-runner:test
dotnet build --no-restore
dotnet test --no-build --filter "Category=CodeRunnerSecurity"
docker ps -a
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## 14. Critères d'acceptation

Tous contrôles observables dans `inspect`, abus contenus, timeouts/nettoyage prouvés, aucune fuite secret/hôte, scan sans critique non acceptée, mode manuel clairement non automatisé.

## 15. Conditions d'arrêt

Runner privilégié, réseau/montage/secret accessible, quota non effectif, conteneur orphelin, image non épinglée, vulnérabilité critique ou test d'abus en échec.

## 16. Mise à jour attendue de la roadmap

Cocher `04C` uniquement après revue sécurité formelle ; prochaine fiche `04D`.

## 17. Format obligatoire du rapport final

Modèle de menace ; options `inspect` ; image/digest/scan ; matrice d'abus avec résultats ; quotas mesurés ; nettoyage ; mode manuel ; commandes exactes ; risques résiduels ; confirmation d'absence de SqlLab/contenu.
