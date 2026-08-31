# Plan de la veille : quoi lire, dans quel ordre, en combien de temps

Le poste visé : ingénieur logiciel en délégation, digitalisation des processus d'un service
public, équipe SCRUM, .NET (ASP.NET Core, Entity Framework), Azure (Service Bus, DevOps), IIS,
Vue.js 3 et TypeScript, architecture microservices. Ce dossier ordonne la préparation dans le
temps réellement disponible — une soirée, une pause déjeuner, une deuxième soirée, vingt minutes
le matin — et relie chaque étape au cours correspondant de la plateforme. Le principe directeur :
l'entretien se gagne sur vos histoires vraies et sur la clarté de votre périmètre, pas sur la
quantité de théorie avalée.

## Soirée 1 — trois heures, l'essentiel

**Première heure et demie : vos histoires, à voix haute.** C'est la partie la plus rentable et la
plus négligée. Travaillez trois récits structurés avec le
[carnet STAR](/career/career-star-workbook-001) puis chiffrez-les avec le
[CV par preuves](/career/career-cv-evidence-001) : la migration d'un ERP WebForms vers .NET 8
(racontée comme une architecture : modules isolés derrière des frontières nettes, livrés
séparément), le système de promotions centralisé (avec son gain mesuré), et un projet mené du
besoin aux spécifications détaillées — l'offre demande exactement cette capacité. Dites chaque
récit deux fois à voix haute, deux minutes chrono.

**Quarante-cinq minutes : le geste API.** Lire ne suffit pas quand l'écart porte sur la pratique.
Créez un projet Web API minimal, un contrôleur avec les quatre verbes sur une liste en mémoire,
observez les codes 200, 201, 400 et 404 dans l'interface générée. Ce soir-là, « jamais fait
d'API » devient « j'en ai monté une hier pour vérifier que la marche est petite depuis le MVC ».
Les leçons du bloc API de la plateforme approfondiront après l'entretien, en commençant par la
sémantique HTTP et les contrats.

**Quarante-cinq minutes : deux guides de la Semaine 0.** D'abord
[Microservices : les concepts pour en parler](/learn/week-0/week0-microservices-001) — il se
termine par le motif de l'étrangleur appliqué à une migration d'ERP, votre point d'entrée naturel
dans la conversation. Ensuite
[Azure Service Bus](/learn/week-0/week0-azure-service-bus-001), qui prolonge le premier :
messagerie asynchrone, files contre sujets, verrou de réception et lettres mortes. Retenez trois
phrases par guide, pas davantage.

## Pause déjeuner — trente minutes

[Vue.js 3 et TypeScript](/learn/week-0/week0-vue-typescript-001). Objectif volontairement
modeste : reconnaître un composant en script setup, savoir ce que font une valeur réactive, une
valeur calculée et des props typées, pour ne pas rester muet devant un extrait de code. La phrase
honnête à préparer : de formation back-end, le front moderne est précisément ce que ce poste
permet d'apprendre en production.

## Soirée 2 — une heure et demie

**Une heure : l'outillage.** [Azure DevOps](/learn/week-0/week0-azure-devops-001) — la structure
étapes, jobs, steps d'un pipeline, les groupes de variables secrets, les environnements avec
approbation — puis [IIS](/learn/week-0/week0-iis-001) — sites, pools d'applications, le module
qui relaie vers l'application .NET. Deux sujets où quelques repères précis suffisent à tenir une
conversation d'équipe.

**Trente minutes : répétition des scripts.** Le second dossier de ce sous-thème,
[Scripts d'entretien](/prep/icube-scripts-entretien-001), contient les réponses préparées aux
questions sensibles et les questions à poser. Répétez-les à voix haute, comme les récits.

## Si le temps le permet

Deux leçons de la piste senior donnent la profondeur d'architecte sur les mêmes sujets, chacune
avec sa section Entretien et son vocabulaire anglais :
[les frontières de services](/learn-senior/senior-boundaries-001) et
[la messagerie entre services](/learn-senior/senior-messaging-001). Pour s'entraîner à répondre
avant de lire une réponse modèle, les [fiches d'entretien](/interviews) de la famille senior
couvrent disjoncteur, idempotence et cohérence.

## Le matin — vingt minutes, pas plus

Relire uniquement : les trois récits, les trois phrases par guide, et les scripts du second
dossier. Rien de neuf le matin d'un entretien — la valeur se joue sur ce qui est déjà répété.

## Ce que ce plan assume

Terraform, Kubernetes et Helm figurent dans l'offre comme des atouts, pas des exigences : le
[chapitre Cloud](/cloud) couvre les concepts Kubernetes si une soirée se libère, et il est
parfaitement défendable de répondre « pas encore pratiqué, et voici comment j'apprends — la
preuve, ce que j'ai préparé cette semaine ». Ce dossier ne produit aucune preuve de maîtrise et ne
promet aucun résultat : il ordonne l'effort là où il rapporte.
