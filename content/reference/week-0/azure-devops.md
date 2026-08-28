# Azure DevOps : Boards, Repos, Pipelines et Artifacts en un coup d'œil

Azure DevOps n'est pas un seul outil mais une suite de services qui couvrent tout le cycle de vie
d'une équipe : planifier le travail, héberger le code, le construire et le livrer, distribuer des
paquets, et le tester. **Boards** gère les éléments de travail, sprints et tableaux kanban.
**Repos** héberge des dépôts Git et leurs demandes de tirage (pull requests). **Pipelines**
automatise la construction et le déploiement. **Artifacts** héberge des flux de paquets internes
(NuGet, npm). **Test Plans** organise des campagnes de test manuel. Pour une équipe qui livre du
logiciel, Pipelines est le service qui structure le plus directement le passage du code écrit au
code en production.

## Pipelines : le cœur de la livraison

Un pipeline Azure DevOps se décrit en YAML, versionné avec le code dans un fichier
`azure-pipelines.yml` à la racine du dépôt. Sa structure s'emboîte en trois niveaux : une ou
plusieurs **étapes** (stages, par exemple construire puis déployer), chaque étape contient un ou
plusieurs **jobs** exécutés sur un agent, chaque job exécute une suite de **steps** — des tâches
prédéfinies ou de simples commandes shell. Un pipeline se déclenche par défaut sur chaque
poussée vers les branches configurées, mais peut aussi être planifié ou déclenché manuellement.

## Exemple commenté

Un pipeline qui restaure, construit, teste puis publie un artefact :

```yaml
trigger:
  branches:
    include: [main]

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '9.x'
  - script: dotnet restore
  - script: dotnet build --configuration Release --no-restore
  - script: dotnet test --configuration Release --no-build
  - task: PublishPipelineArtifact@1
    inputs:
      targetPath: 'bin/Release'
      artifact: 'app'
```

Chaque `task` invoque une action réutilisable versionnée (ici `UseDotNet@2`, dont le `@2` fixe la
version majeure de la tâche), tandis que `script` exécute une commande shell brute.

## Variables et secrets

Une variable se référence dans le YAML par la syntaxe `$(nomDeVariable)`. Les valeurs sensibles —
chaîne de connexion, jeton d'API — ne doivent jamais être écrites en clair dans le YAML : elles se
déclarent dans un **groupe de variables** marqué secret, ou proviennent directement d'un coffre
Key Vault lié au pipeline, et Azure DevOps masque automatiquement leur valeur dans les journaux
d'exécution.

## Environnements et approbations

Un **environnement** représente une cible de déploiement (recette, production) et peut porter des
**contrôles d'approbation** : une étape de déploiement vers production s'arrête et attend qu'une
personne désignée valide la poursuite, avant d'exécuter les jobs qui lui sont rattachés. La
connexion technique vers la cible réelle (un abonnement Azure, un registre de conteneurs) passe
par une **connexion de service** (service connection), configurée une fois puis référencée par
plusieurs pipelines sans y exposer d'identifiants.

## Repos et la revue de code

Une **politique de branche** sur la branche principale peut exiger un nombre minimal
d'approbateurs sur chaque demande de tirage, et rendre obligatoire la réussite d'un pipeline de
validation avant de pouvoir fusionner : le code qui casse la construction ou les tests
n'atteint jamais la branche principale, quelle que soit l'urgence perçue par son auteur.

## Pièges fréquents

Mélanger construction et déploiement dans un seul job sans séparer les étapes rend impossible de
rejouer un déploiement sans reconstruire, et empêche d'ajouter une approbation propre au
déploiement. Oublier de publier l'artefact de construction avant l'étape de déploiement, ou
oublier de le télécharger dans cette dernière, produit une étape de déploiement qui ne trouve
rien à déployer — les étapes d'un pipeline ne partagent pas leur système de fichiers par défaut.
Enfin, coller un jeton ou un mot de passe directement dans le YAML au lieu d'un groupe de
variables secret le rend visible dans l'historique Git pour quiconque a accès au dépôt, bien
après sa révocation.

## Ce qu'il faut retenir

Un pipeline s'imbrique en étapes, jobs puis steps, et se décrit en YAML versionné avec le code.
Les secrets vivent dans des groupes de variables ou un coffre lié, jamais en clair dans le
fichier. Un environnement porte les approbations de déploiement, une connexion de service porte
l'accès à la cible, et un artefact doit être explicitement publié puis téléchargé pour circuler
d'une étape à l'autre.
