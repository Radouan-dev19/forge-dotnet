# Forge.NET — Guide de contribution

## Mission

Forge.NET est une application locale d'apprentissage actif pour rendre mesurable l'autonomie d'un développeur C#/.NET. Elle ne promet ni emploi ni salaire et ne confond jamais consultation d'une solution avec maîtrise.

## Périmètre technique

- .NET 10, ASP.NET Core Blazor Web App, EF Core et SQLite.
- Monolithe modulaire ; aucune architecture microservices.
- SQL Server sous Docker réservé aux laboratoires SQL.
- Contenu pédagogique versionné sous `content/`; progression sous SQLite.
- Code soumis exécuté uniquement dans un conteneur éphémère isolé.
- Interface et documentation en français ; code, identifiants et vocabulaire technique en anglais.

## Commandes de référence

À partir de la phase 1 :

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
docker compose config
```

Ne pas déclarer une phase terminée si une commande applicable échoue. Rapporter l'erreur telle quelle.

## Règles d'architecture

- `Domain` contient les règles métier pures et ne dépend d'aucun autre projet applicatif.
- `Application` orchestre les cas d'usage et dépend de `Domain`.
- `Infrastructure` implémente persistance, fichiers et adaptateurs ; il dépend de `Application` et `Domain`.
- `CodeRunner` porte le contrat et l'adaptateur Docker ; il n'exécute jamais le code dans le processus web.
- `Web` compose l'application et porte l'UI, sans règles métier importantes dans les composants.
- Les modules métier sont des dossiers/espaces de noms cohésifs, pas des projets supplémentaires sans besoin démontré.
- Les changements de règles de maîtrise exigent des tests unitaires couvrant les contournements.

## Sécurité

- Aucun secret, jeton, donnée personnelle sensible ou solution d'examen dans les logs.
- Toute entrée de contenu est validée avant exposition.
- Toute requête SQL de laboratoire vise une base jetable dédiée, jamais SQLite de progression.
- Le runner impose image épinglée, réseau désactivé, utilisateur non-root, limites CPU/mémoire/PID/temps/sortie, montage temporaire et suppression garantie.
- Ne pas annoncer que le mode manuel a validé du code automatiquement.

## Contenu

- Respecter `docs/CONTENT_GUIDE.md`, `docs/CONTENT_AUTHORING_STANDARD.md` et les schémas versionnés.
- Aucun placeholder, aucune dépendance obligatoire à une ressource externe. Trois règles du
  validateur le font respecter : `unsubstituted-placeholder`, `cloned-content`, `hollow-lesson`.
- La dette héritée est déclarée dans `content/authoring/content-debt.json` et ne peut que décroître.
  Ne jamais y ajouter de ligne pour faire passer un contenu neuf : la règle vaut pour l'existant.
- Les échafaudeurs `scripts/New-S*Content.ps1` ne réécrivent aucun fichier existant sans `-Force`.
- Chaque activité automatisable possède tests visibles et cachés, indices progressifs, solution, explication, erreurs fréquentes et variante.
- Après affichage d'une solution : tentative non maîtrisée, explication personnelle et révision à blanc planifiée.
- Les identifiants de contenu sont stables ; une modification incompatible exige une montée de version du schéma.

## Travail incrémental

- Suivre `docs/ROADMAP.md`, une tranche verticale révisable à la fois.
- Ajouter les tests avant ou avec les règles critiques.
- Mettre à jour documentation et roadmap dans le même changement.
- Préserver les modifications utilisateur non liées.
- Ne pas générer le projet final à la place de l'apprenant.

## Définition de terminé

Un incrément est terminé lorsque ses critères d'acceptation sont démontrés, ses tests passent, les migrations et données de démonstration sont reproductibles, les risques de sécurité ont été revus et la documentation permet de reproduire le résultat.
