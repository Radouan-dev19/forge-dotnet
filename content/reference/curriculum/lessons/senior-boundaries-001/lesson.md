# Découper ou non : le refus argumenté

## Objectif observable

À la fin de cette leçon, vous saurez estimer le coût d'un déployable supplémentaire, reconnaître où passe une vraie frontière de service, et rédiger une note de décision qui refuse un découpage prématuré avec des arguments qu'un responsable pressé peut entendre.

## Prérequis

- Avoir suivi `senior-consistency-001` : ce qu'une transaction ne couvre plus une fois la frontière franchie.
- Savoir ce qu'est un déploiement et une base de données partagée.

## Intuition

Découper en services se présente souvent comme un progrès. Ce n'en est pas un en soi : c'est un coût que l'on paie contre une frontière réelle. Un junior qui découpe par réflexe fabrique un système distribué dont il devra ensuite exploiter les pannes partielles, la latence et la cohérence éventuelle, sans avoir rien gagné. La compétence senior n'est pas de savoir découper, c'est de savoir refuser de découper quand les signaux ne le justifient pas.

## Explication

**Le coût d'un déployable est réel et immédiat.** Chaque service supplémentaire ajoute un artefact à construire, versionner, déployer et surveiller. Un appel de méthode devient un appel réseau, avec sa latence et son mode de panne propre. Une transaction locale devient une saga à compenser. Ces coûts se paient dès le premier jour, alors que les bénéfices supposés — équipes autonomes, montée en charge indépendante — n'arrivent que si la frontière est bien placée.

**Une vraie frontière réunit trois signaux.** D'abord, plusieurs équipes se gênent sur le même déployable et veulent livrer à leur rythme. Ensuite, ces parties peuvent réellement se déployer indépendamment : si elles doivent sortir ensemble, les séparer donne deux artefacts couplés, le pire des deux mondes. Enfin, elles ne partagent pas de données : une table commune trahit une frontière mal placée, et le service extrait resterait couplé par la base, un couplage caché plus dangereux que l'appel qu'il remplace.

**Le refus est une décision, pas une paresse.** Refuser un découpage se défend par écrit, dans une note de décision contradictoire : on nomme le motif avancé, on le confronte aux trois signaux, et on conclut. L'asymétrie des coûts justifie la prudence : garder un monolithe qui aurait pu être découpé se corrige plus tard, quand les signaux deviennent nets ; découper trop tôt crée une dette distribuée que l'on ne rembourse presque jamais, car recoller deux services est bien plus rare que d'en séparer un.

**Ce que sondent les entretiens.** Un entretien senior ne demande pas de réciter les patrons microservices. Il cherche à savoir si le candidat sait tenir une position contradictoire sous pression, chiffres et frontières en main, plutôt que de suivre une mode. Savoir dire « pas encore, et voici pourquoi » vaut mieux que de savoir dessiner dix services.

## Exemple commenté

Le noyau décidable de cette leçon tranche entre garder et extraire, avec le monolithe comme défaut :

```csharp
// Défaut : garder le monolithe. On n'extrait que si les trois signaux convergent.
public static string DecompositionAdvice(int teams, int deploysCoupled, int sharedTables)
{
    if (teams <= 1) { return "keep-monolith"; }        // une équipe ne gagne rien à se distribuer
    if (sharedTables > 0) { return "keep-monolith"; }  // données partagées : frontière mal placée
    if (deploysCoupled == 0) { return "extract-service"; }
    return "keep-monolith";                            // déploiements encore couplés
}
```

Chaque condition de refus est vérifiée avant la seule condition d'extraction : la prudence est encodée dans l'ordre.

## Contre-exemple et erreur fréquente

Le découpage réflexe sur le seul nombre d'équipes :

```csharp
// FAUTIF : on extrait dès qu'il y a plus d'une équipe, sans regarder le reste.
string advice = teams > 1 ? "extract-service" : "keep-monolith";
```

Le symptôme apparaît des mois plus tard : deux services qui doivent toujours sortir ensemble, ou recouplés par une table partagée, avec toute la latence et les pannes partielles en prime. La correction confronte le nombre d'équipes aux deux autres signaux avant de conclure.

## Vérification de compréhension

Avant le quiz, répondez à voix haute : pourquoi trois équipes qui partagent deux tables doivent-elles rester en monolithe ?

:::quiz
id=senior-boundaries-001-check
question=Quelle est la position par défaut face à une demande de découpage en services ?
option=Découper, car les microservices sont plus modernes et scalables
option=Garder le monolithe, et n'extraire que si plusieurs équipes, un déploiement indépendant et aucune donnée partagée convergent
option=Découper dès qu'une deuxième équipe rejoint le projet
correct=1
success=Exact : le monolithe est le défaut ; l'extraction est le coût que l'on ne paie que si les trois signaux d'une vraie frontière convergent.
retry=Repensez à ce que coûte un déployable de plus, et à ce qu'une table partagée révèle d'une frontière.
:::

## Exercice guidé

Ouvrez l'exercice `senior-decomposition-001` dans `/practice`, puis procédez ainsi.

1. Posez le monolithe comme décision par défaut.
2. Refusez d'extraire tant qu'il n'y a qu'une équipe.
3. Refusez d'extraire s'il reste des tables partagées, même avec plusieurs équipes.
4. N'extrayez que si les déploiements sont déjà indépendants ; sinon, gardez.

## Exercice autonome

Prenez un système que vous connaissez. Rédigez une note de décision d'une demi-page qui répond à « faut-il en extraire un service ? » : nommez le motif avancé, confrontez-le aux trois signaux, et concluez par un refus ou une extraction argumentés.

## Débogage

Un ticket indique : « Depuis qu'on a extrait le service de facturation, les déploiements sont plus lents et une panne de la facturation empêche même de consulter une commande. »

1. **Symptôme** : déploiements ralentis et panne partielle qui déborde sur un domaine voisin.
2. **Hypothèse** : la frontière était mal placée — services encore couplés au déploiement ou par les données.
3. **Preuve** : vérifier si facturation et commandes se déploient réellement seuls et s'ils partagent des tables.
4. **Prévention** : ne pas rextraire tant que les déploiements sont couplés, et documenter la décision.

## Entretien

Question posée à voix haute : *un responsable veut découper le monolithe pour aller plus vite ; comment répondez-vous ?*

Une réponse solide part du monolithe, demande ce qu'un service de plus achète vraiment, et confronte le motif aux trois signaux — équipes, déploiement indépendant, données partagées. Elle défend le refus par l'asymétrie des coûts, sans braquer : « pas encore, et voici le premier signal qui me ferait changer d'avis ».

## Résumé

- Découper n'est pas un progrès en soi : c'est un coût payé contre une frontière réelle.
- Une vraie frontière réunit plusieurs équipes, un déploiement indépendant et aucune donnée partagée.
- Le défaut est de garder le monolithe ; l'extraction est l'exception à justifier.
- Refuser un découpage se défend par écrit ; l'asymétrie des coûts justifie la prudence.

## Cartes de révision

Question : quels trois signaux, ensemble, justifient d'extraire un service ? Réponse attendue : plusieurs équipes qui se gênent, un déploiement réellement indépendant, et aucune donnée partagée entre les parties.

Question : pourquoi découper trop tôt est-il plus coûteux que découper trop tard ? Réponse attendue : parce que recoller deux services est bien plus rare que d'en séparer un ; la dette distribuée d'un découpage prématuré ne se rembourse presque jamais.

## Test de maîtrise

Sans relire, rédigez la note de décision qui refuse un découpage sur un cas de votre choix : motif avancé, confrontation aux trois signaux, conclusion, et le premier signal qui vous ferait reconsidérer.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
