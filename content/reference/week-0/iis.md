# IIS : héberger une application .NET sur Windows

IIS (Internet Information Services) est le serveur web intégré à Windows Server. Là où une
application PaaS comme App Service masque la machine sous-jacente, IIS s'installe et se configure
sur un serveur — physique, virtuel ou une machine virtuelle Azure — ce qui reste courant pour des
applications d'entreprise hébergées sur une infrastructure existante. Pour une application ASP.NET
Core moderne, IIS ne s'occupe pas d'exécuter le code .NET lui-même : il reçoit la requête HTTP,
puis la relaie à un processus qui exécute réellement l'application.

## Sites, liaisons et pools d'applications

Un **site** IIS associe un dossier physique à une ou plusieurs **liaisons** (bindings) — un
protocole, une adresse IP, un port et éventuellement un en-tête d'hôte, qui déterminent quelle
requête entrante atteint quel site. Un **pool d'applications** (application pool) est la
frontière d'isolation d'exécution : chaque pool tourne dans son propre processus, avec sa propre
identité Windows, et un problème dans un pool — fuite mémoire, plantage — n'affecte pas les
applications des autres pools. La pratique recommandée est un pool dédié par application, jamais
un pool partagé entre plusieurs applications indépendantes.

## Comment IIS lance une application .NET moderne

Une application ASP.NET Core s'exécute normalement dans son propre serveur intégré, Kestrel, pas
dans IIS lui-même. Le **module ASP.NET Core** (ANCM, installé par le pack d'hébergement .NET) fait
le lien : IIS reçoit la requête et la transmet au processus de l'application. Deux modèles
d'hébergement existent : **in-process**, où l'application s'exécute directement dans le processus
de travail IIS (le plus performant, et le mode par défaut aujourd'hui), et **out-of-process**, où
IIS agit en relais (reverse proxy) vers un processus Kestrel séparé — utile notamment quand
plusieurs applications historiques cohabitent avec des contraintes différentes.

## Exemple commenté

La commande `dotnet publish` génère un fichier `web.config` qui indique à IIS comment démarrer
l'application :

```xml
<configuration>
  <system.webServer>
    <aspNetCore processPath="dotnet"
                arguments=".\MonApplication.dll"
                stdoutLogEnabled="false"
                hostingModel="inprocess" />
  </system.webServer>
</configuration>
```

`processPath` et `arguments` indiquent le programme et les arguments à lancer pour démarrer
l'application, et `hostingModel` choisit entre les deux modèles d'hébergement décrits plus haut.

## HTTPS et certificats

Une liaison HTTPS associe un certificat au site, chargé depuis le magasin de certificats Windows
de la machine. Un certificat auto-signé convient pour un poste de développement, mais un
navigateur le signale comme non fiable ; un déploiement réel utilise un certificat émis par une
autorité reconnue. Plusieurs sites HTTPS peuvent partager le port 443 sur une même adresse IP
grâce à l'indication du nom de serveur (SNI), qui laisse le client préciser quel nom d'hôte il
vise avant même l'échange du certificat.

## Pièges fréquents

Oublier d'installer le **pack d'hébergement .NET** (Hosting Bundle) sur le serveur — qui installe
le runtime et le module ANCM — produit une erreur 500.19 ou 500.30 au premier démarrage, alors que
l'application fonctionne parfaitement en local. Une **identité de pool** sans les permissions
nécessaires sur le dossier de l'application, ou sur une ressource externe (base de données,
dossier réseau), produit des erreurs d'accès refusé qui n'apparaissent qu'une fois déployé,
jamais en développement où l'application tourne sous l'identité de l'utilisateur. Enfin, un pool
configuré pour recycler trop fréquemment sous prétexte de « libérer la mémoire » interrompt les
requêtes en cours à chaque recyclage et masque un vrai problème de fuite mémoire au lieu de le
corriger.

## Ce qu'il faut retenir

Un site IIS écoute sur des liaisons, un pool d'applications isole l'exécution, et pour une
application .NET moderne, IIS relaie la requête via le module ASP.NET Core plutôt que d'exécuter
le code lui-même — en mode in-process ou out-of-process. Le `web.config` généré au déploiement
porte cette configuration, et la plupart des pannes de mise en production viennent d'un pack
d'hébergement manquant ou d'une identité de pool mal autorisée, deux causes invisibles en local.
