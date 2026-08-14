# Le contrat client/serveur vu du client

## Objectif observable

À la fin de cette leçon, vous saurez ranger une réponse HTTP dans sa famille de statut et exploiter
un corps d'erreur normalisé, annuler une requête en vol et dire pourquoi c'est indispensable,
attacher un jeton d'accès et le renouveler sans déranger l'utilisateur, et décider si une donnée en
cache est fraîche, servable pendant sa revalidation, ou périmée.

## Prérequis

- Avoir lu `front-state-unidirectional-001` : l'état comme source unique de vérité.
- Savoir qu'un appel réseau prend du temps et peut échouer, arriver en désordre, ou ne jamais revenir.

## Intuition

Vu du client, une API est un correspondant à qui l'on parle par courrier : on envoie une demande,
on attend une réponse qui peut tarder, se perdre, ou revenir après une demande plus récente. Le
client ne contrôle ni le délai, ni l'ordre d'arrivée, ni le fait que l'autre réponde. Son travail
est donc défensif : classer ce qui revient, renoncer à une demande devenue inutile, prouver son
identité à chaque envoi sans reconnexion, et se souvenir des réponses récentes pour ne pas
redemander la même chose. Un client naïf suppose que tout arrive vite, dans l'ordre, une seule
fois ; un client robuste ne suppose rien de tout cela.

## Explication

**Les familles de statut disent quoi faire, pas seulement ce qui s'est passé.** Le client n'a pas
à mémoriser chaque code : il raisonne par familles. La famille 2xx signale un succès et le corps
porte la donnée attendue. La famille 4xx dit que la requête est en faute — donnée invalide, droit
manquant, ressource absente — et réessayer à l'identique ne changera rien ; il faut corriger la
demande ou informer l'utilisateur. La famille 5xx dit que le serveur a échoué sur une requête
peut-être correcte ; un nouvel essai plus tard peut réussir. Cette classification décide du
comportement : erreur définitive, guidage d'une correction, ou nouvel essai.

**Le corps d'erreur normalisé porte le détail exploitable.** Un statut seul est grossier. Beaucoup
d'API renvoient, pour les erreurs, un corps normalisé — un format de problem-details — décrivant le
type du problème, un titre lisible, et souvent les champs fautifs. Le client gagne à lire ce corps
plutôt qu'à deviner : il affiche un message précis et, pour une erreur de validation, surligne les
champs rejetés. Le statut oriente la stratégie ; le corps normalisé fournit le message.

**Une requête en vol doit pouvoir être annulée.** Une requête part, puis devient inutile avant de
revenir : l'utilisateur a changé de page, relancé une recherche, quitté l'écran. Sans annulation,
deux maux surviennent. Le premier est le gaspillage : on attend et on traite une réponse dont
personne ne veut. Le second, plus grave, est la condition de course : une recherche lente partie en
premier revient après une recherche rapide partie ensuite, et écrase le bon résultat par l'ancien.
Rendre chaque requête annulable — via un jeton d'annulation transmis à l'appel, l'équivalent d'un
`CancellationToken` — coupe la précédente dès qu'une nouvelle la remplace : seul le résultat voulu
met à jour l'état.

**Le jeton s'attache à chaque requête, et se renouvelle en silence.** Une API protégée exige une
preuve d'identité : un jeton porteur, joint à chaque requête dans un en-tête d'autorisation. Ce
jeton a une durée de vie courte, par sécurité. Quand il expire, on ne veut pas renvoyer
l'utilisateur à l'écran de connexion : on effectue un rafraîchissement silencieux. À la première
réponse signalant le jeton invalide, le client utilise un jeton de renouvellement, de vie plus
longue, pour obtenir un nouveau jeton d'accès, puis rejoue la requête d'origine sans que
l'utilisateur remarque rien. Le point délicat est de ne lancer qu'un seul rafraîchissement même si
plusieurs requêtes échouent ensemble, et de faire attendre les autres, sous peine de renouvellements
concurrents.

**Le cache se raisonne en trois âges.** Une réponse en cache passe par trois états. Fraîche
(fresh) : plus récente qu'un seuil, on la sert directement sans appel réseau. Servable pendant
revalidation (stale-while-revalidate) : au-delà du seuil de fraîcheur mais dans une fenêtre de
tolérance, on affiche immédiatement la valeur en cache pour ne pas faire attendre, tout en lançant
en arrière-plan une requête qui rafraîchira le cache. Périmée (expired) : au-delà de la fenêtre, la
donnée est trop vieille pour être montrée ; il faut attendre une réponse fraîche. Ces trois âges
arbitrent le compromis entre vitesse perçue et exactitude : servir vite une valeur presque à jour,
ou attendre une valeur sûre.

## Exemple commenté

Le coeur de la décision stale-while-revalidate, en C# : classer une entrée de cache selon son âge.

```csharp
public enum CacheVerdict { Fresh, ServeStaleAndRevalidate, Expired }

// freshFor : duree pendant laquelle la donnee est servie sans appel.
// staleFor : fenetre supplementaire ou l'on sert l'ancienne valeur tout en revalidant.
public static CacheVerdict Classify(TimeSpan age, TimeSpan freshFor, TimeSpan staleFor)
{
    if (age <= freshFor) return CacheVerdict.Fresh;                  // recente : servir tel quel
    if (age <= freshFor + staleFor) return CacheVerdict.ServeStaleAndRevalidate; // servir et rafraichir
    return CacheVerdict.Expired;                                     // trop vieille : attendre du frais
}
```

Le point à retenir : le verdict dépend de deux seuils cumulés ; entre les deux, on privilégie la
vitesse perçue en servant l'ancienne valeur tout en préparant la suivante.

## Contre-exemple et erreur fréquente

L'erreur classique ne rend pas les requêtes annulables et laisse la dernière réponse arrivée gagner.

```csharp
// FAUTIF : chaque recherche ecrit dans l'etat des son retour, sans annuler la precedente.
async Task Search(string term)
{
    var results = await api.SearchAsync(term); // aucun jeton d'annulation
    state.Results = results;                   // la reponse la plus lente ecrase la plus recente
}
```

Symptôme : l'utilisateur tape « abc », la recherche « ab » lente revient après « abc » rapide, et
la liste affiche les résultats de « ab » alors que le champ montre « abc ». La correction transmet
un jeton d'annulation et coupe la requête précédente à chaque nouvelle saisie.

```csharp
// CORRIGE : la recherche precedente est annulee avant d'en lancer une nouvelle.
async Task Search(string term, CancellationToken ct)
{
    var results = await api.SearchAsync(term, ct); // ct annule si une nouvelle recherche part
    ct.ThrowIfCancellationRequested();             // on n'ecrit l'etat que si toujours pertinent
    state.Results = results;
}
```

## Vérification de compréhension

Avant le quiz, dites à voix haute : pourquoi une réponse en famille 4xx n'appelle-t-elle pas le
même réflexe qu'une réponse en famille 5xx ?

:::quiz
id=front-client-server-contract-001-check
question=Pourquoi une requête de recherche en vol doit-elle pouvoir être annulée quand l'utilisateur relance sa recherche ?
option=Parce qu'une requête non annulée consomme de la mémoire serveur de façon permanente
option=Parce qu'une réponse lente partie plus tôt peut revenir après une réponse rapide partie plus tard et écraser le bon résultat par un résultat périmé
option=Parce que le serveur refuse deux requêtes du même utilisateur en parallèle
correct=1
success=Exact : sans annulation, l'ordre d'arrivée n'est pas garanti et une ancienne réponse peut écraser la récente ; annuler garantit que seul le résultat voulu met à jour l'état.
retry=Repensez à l'ordre d'arrivée des réponses quand deux requêtes partent l'une après l'autre à des vitesses différentes.
:::

## Exercice guidé

Ouvrez l'exercice `front-cache-decision-001` dans `/practice`, puis procédez ainsi.

1. Relevez l'âge de l'entrée en cache et les deux seuils : durée de fraîcheur et fenêtre de tolérance.
2. Comparez l'âge au premier seuil pour distinguer une donnée fraîche d'une donnée dépassée.
3. Pour une donnée dépassée mais dans la fenêtre, décidez de servir l'ancienne valeur tout en revalidant.
4. Au-delà de la fenêtre cumulée, classez la donnée périmée et exigez une réponse fraîche avant affichage.

## Exercice autonome

Prenez un écran listant des données rafraîchies périodiquement. Décidez ses seuils de fraîcheur et
de tolérance, puis déroulez trois scénarios d'âge — juste après le chargement, un peu après le
seuil, longtemps après — en donnant le verdict pour chacun. Reliez ensuite le sujet à
`front-route-guard-001` : dites comment un jeton expiré devrait influencer l'accès à cet écran, et
si l'utilisateur doit être renvoyé vers la connexion ou servi par un rafraîchissement silencieux.

## Débogage

Un ticket indique : « Dans la recherche, en tapant vite, la liste affiche parfois les résultats
d'un terme que je ne cherche plus. »

1. **Symptôme** : la liste montre des résultats correspondant à une saisie antérieure, pas à la saisie courante.
2. **Hypothèse** : les requêtes ne sont pas annulées ; une réponse plus lente partie plus tôt
   revient après une réponse plus rapide et écrase l'état.
3. **Preuve** : transmettre un jeton d'annulation, couper la requête précédente à chaque frappe, et
   vérifier que l'état n'est écrit que pour la requête encore pertinente.
4. **Prévention** : rendre toute requête liée à une saisie annulable, et n'écrire l'état qu'après
   avoir confirmé que la requête n'a pas été annulée.

## Entretien

Question posée à voix haute : *du point de vue du client, comment gérez-vous une requête qui peut
échouer, expirer ou devenir obsolète avant de revenir ?*

Une réponse solide raisonne par familles de statut et lit le corps normalisé des erreurs, rend les
requêtes annulables contre les conditions de course, attache un jeton porteur et décrit le
rafraîchissement silencieux partagé, puis arbitre le cache entre frais, servable pendant
revalidation et périmé selon la vitesse perçue voulue.

## Résumé

- On classe par familles : 2xx succès, 4xx faute du client à corriger, 5xx échec serveur à retenter.
- Le corps normalisé d'erreur fournit le message précis et les champs fautifs, au-delà du statut.
- Toute requête en vol doit être annulable, sinon une vieille réponse écrase la récente.
- Le jeton porteur s'attache à chaque requête et se renouvelle en silence, en un seul rafraîchissement partagé.
- Le cache a trois âges : frais (servi tel quel), servable pendant revalidation, périmé (attendre du frais).

## Cartes de révision

Question : que signifie servir une donnée en stale-while-revalidate ? Réponse attendue : la donnée
a dépassé son seuil de fraîcheur mais reste dans la fenêtre de tolérance ; on l'affiche
immédiatement pour la vitesse perçue tout en lançant en arrière-plan une requête qui rafraîchira le
cache pour la prochaine fois.

Question : pourquoi ne faut-il lancer qu'un seul rafraîchissement de jeton même si plusieurs
requêtes échouent en même temps ? Réponse attendue : pour éviter des renouvellements concurrents ;
on lance un unique rafraîchissement, on fait attendre les autres requêtes, puis on les rejoue avec
le nouveau jeton une fois obtenu.

## Test de maîtrise

Sans relire, décrivez la réaction du client aux trois familles de statut, l'usage du corps
normalisé d'erreur, l'annulation d'une requête en vol et la condition de course qu'elle prévient,
le rafraîchissement silencieux du jeton, et les trois âges du cache avec leur arbitrage entre
vitesse et exactitude.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
