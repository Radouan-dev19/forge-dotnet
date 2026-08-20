# Résilience d'un appel : délai, réessai, disjoncteur, cloisonnement

## Objectif observable

À la fin de cette leçon, vous saurez expliquer les quatre protections d'un appel distant — le
budget de délai, le réessai avec gigue, le disjoncteur et le cloisonnement — dire ce que chacune
prévient, et justifier pourquoi un réessai naïf transforme une panne partielle en effondrement
généralisé.

## Prérequis

- Avoir lu `api-async-cancellation-001` : le jeton d'annulation et la propagation d'un délai.
- Savoir qu'un appel réseau peut échouer, tarder ou revenir bien après qu'on cesse de l'attendre.

## Intuition

Un appel distant qui échoue une fois n'est pas une catastrophe ; c'est la façon dont votre code
*réagit* à cet échec qui décide si l'incident reste local ou se propage. La résilience n'est pas
l'absence de panne : c'est un ensemble de décisions prises d'avance sur le comportement en cas de
panne. Chaque protection répond à une question précise. Combien de temps j'attends avant
d'abandonner ? Est-ce que je retente, et à quel rythme ? Quand est-ce que je cesse même d'essayer ?
Comment j'empêche un appel malade de contaminer tout le reste ? Un ingénieur qui ne sait répondre
qu'à la première question a un système qui tient les jours calmes et casse les jours de tempête.

## Explication

**Le budget de délai borne l'attente.** Sans délai explicite, un appel bloqué occupe une ressource
— un fil d'exécution, une connexion — jusqu'à ce que la couche réseau abandonne, ce qui peut
prendre bien plus longtemps que ce que l'utilisateur tolère. Le budget de délai fixe la durée
maximale accordée à cet appel, dérivée du temps total dont dispose la requête entrante. La règle
clé est la propagation : si la requête d'origine a mille millisecondes, un sous-appel ne peut pas
en réclamer mille aussi ; il hérite du temps restant, diminué. Un budget non propagé est un budget
fictif, car chaque couche croit disposer du temps total alors qu'il est déjà consommé en amont.

**Le réessai rattrape l'aléa, mais amplifie la surcharge.** Retenter a du sens face à une erreur
*transitoire* — un paquet perdu, une bascule momentanée. Cela n'en a aucun face à une erreur
*permanente* — une requête invalide, une ressource absente : retenter ne fera que répéter l'échec.
Le piège central est le réessai en cascade sous charge. Quand un service ralentit, tous ses
appelants retentent en même temps ; le service reçoit alors non pas sa charge normale mais son
double ou son triple, précisément au pire moment. Le réessai synchronisé transforme une lenteur
passagère en panne complète : c'est l'effondrement par réessai. Deux mesures cassent cette
mécanique. La *temporisation croissante* espace de plus en plus les tentatives. La *gigue* — un
délai aléatoire ajouté à chaque attente — désynchronise les appelants pour qu'ils ne frappent pas
tous à la même milliseconde. Sans gigue, mille clients qui retentent ensemble restent groupés à
chaque vague ; avec gigue, ils s'étalent et laissent le service respirer. On borne aussi le nombre
de tentatives : retenter à l'infini, c'est refuser d'admettre qu'une panne est réelle.

**Le disjoncteur arrête de frapper une porte fermée.** Un réessai raisonne à l'échelle d'un appel ;
le disjoncteur raisonne à l'échelle de la *dépendance*. Il observe le taux d'échec récent et bascule
entre trois états. En état *fermé*, les appels passent normalement et le disjoncteur compte les
échecs. Quand ce taux dépasse un seuil, il passe *ouvert* : les appels sont refusés immédiatement,
sans même toucher le service en difficulté, ce qui le laisse récupérer au lieu de l'achever. Après
un délai de repos, il passe *demi-ouvert* : il laisse passer un petit nombre d'appels d'essai. Si
ceux-ci réussissent, il se referme et le trafic reprend ; s'ils échouent, il se rouvre pour un
nouveau repos. L'échec immédiat en état ouvert est un service rendu à l'appelant : mieux vaut une
erreur rapide et prévisible qu'une attente longue qui finira mal de toute façon. Le disjoncteur
transforme une dépendance morte en réponse instantanée.

**Le cloisonnement empêche la contagion.** Le nom vient des compartiments étanches d'un navire :
une brèche dans l'un n'envahit pas les autres, et le bateau flotte. Appliqué au code, cela signifie
attribuer à chaque dépendance un lot borné de ressources — un nombre maximal d'appels simultanés,
un pool de connexions dédié. Si une dépendance devient lente, elle épuise *son* lot et rien de plus ;
les appels vers les autres dépendances continuent avec leurs propres ressources. Sans cloisonnement,
une seule dépendance lente peut accaparer tous les fils d'exécution partagés, et le service entier
devient indisponible à cause d'un unique composant malade. Le cloisonnement échange un peu de
capacité maximale contre une garantie d'isolement, et cet échange est presque toujours le bon quand
la disponibilité globale compte plus que le débit d'une fonction isolée.

**Les quatre protections se composent.** Le budget de délai décide quand abandonner un appel ; le
réessai avec gigue décide comment rattraper un échec sans aggraver la charge ; le disjoncteur décide
quand cesser d'essayer une dépendance en panne ; le cloisonnement décide comment contenir les
dégâts d'une dépendance à cette seule dépendance. Aucune ne remplace les autres, et l'ordre compte :
un réessai sans budget de délai peut attendre indéfiniment, et un réessai sans disjoncteur peut
marteler une porte définitivement fermée.

## Exemple commenté

Le noyau décidable est la transition d'état du disjoncteur — noyau de l'exercice guidé :

```csharp
public enum BreakerState { Closed, Open, HalfOpen }

// Décide l'état suivant à partir de l'état courant et du résultat du dernier appel.
public static BreakerState Next(
    BreakerState current, bool callSucceeded, int consecutiveFailures, int failureThreshold)
{
    return current switch
    {
        // Fermé : on ouvre dès que les échecs consécutifs atteignent le seuil.
        BreakerState.Closed =>
            !callSucceeded && consecutiveFailures >= failureThreshold
                ? BreakerState.Open
                : BreakerState.Closed,

        // Demi-ouvert : un essai réussi referme, un essai raté rouvre.
        BreakerState.HalfOpen =>
            callSucceeded ? BreakerState.Closed : BreakerState.Open,

        // Ouvert : la transition vers demi-ouvert est déclenchée par le délai de repos,
        // pas par un appel ; on reste ouvert tant que le repos n'a pas expiré.
        _ => BreakerState.Open
    };
}
```

La logique reste pure : l'état suivant ne dépend que de l'état courant et d'un fait observé, ce qui
la rend testable sans réseau ni horloge réelle.

## Contre-exemple et erreur fréquente

Le code fautif retente une erreur permanente, sans gigue ni borne :

```csharp
// FAUTIF : boucle infinie de réessai, sans distinction ni espacement.
while (true)
{
    var response = await client.SendAsync(request);
    if (response.IsSuccessStatusCode) return response;
    await Task.Delay(200); // délai fixe, aucune gigue, aucune borne
}
```

Le symptôme n'apparaît que sous charge : quand le service ralentit, tous les clients bouclent au
même rythme fixe et le saturent. Pire, une requête invalide — erreur permanente — sera retentée
sans fin, car le code ne regarde jamais *pourquoi* l'appel a échoué. La correction distingue le
transitoire du permanent, borne les tentatives et ajoute de la gigue :

```csharp
for (int attempt = 0; attempt < maxAttempts; attempt++)
{
    var response = await client.SendAsync(request);
    if (response.IsSuccessStatusCode) return response;
    if (!IsTransient(response.StatusCode)) return response; // permanent : on abandonne
    var backoff = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt));
    await Task.Delay(backoff + Jitter());
}
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : pourquoi mille clients qui retentent avec un délai fixe
identique sont-ils plus dangereux pour un service ralenti que les mêmes clients avec une gigue
aléatoire ?

:::quiz
id=senior-resilience-001-check
question=Pourquoi ajoute-t-on une gigue aléatoire au délai entre deux réessais ?
option=Pour retenter plus vite quand le service répond de nouveau
option=Pour désynchroniser les appelants afin qu'ils ne frappent pas tous le service au même instant et ne l'empêchent pas de récupérer
option=Pour garantir que chaque appel finira toujours par réussir
correct=1
success=Exact : sans gigue, les appelants restent groupés à chaque vague de réessai et maintiennent la surcharge. La gigue les étale dans le temps.
retry=Repensez à ce qui se passe quand mille clients retentent tous exactement au même rythme fixe.
:::

## Exercice guidé

Ouvrez l'exercice `senior-circuit-breaker-001` dans `/practice`, puis procédez ainsi.

1. Modélisez les trois états — fermé, ouvert, demi-ouvert — comme une énumération explicite.
2. Écrivez la transition depuis l'état fermé : ouvrir quand les échecs consécutifs atteignent le
   seuil, rester fermé sinon.
3. Écrivez la transition depuis l'état demi-ouvert : un essai réussi referme, un essai raté rouvre.
4. Prédisez l'état résultant pour une séquence d'appels donnée, dont un passage complet
   fermé, ouvert, demi-ouvert, fermé.

## Exercice autonome

Pour un service qui appelle trois dépendances — une base, un cache, un fournisseur de paiement —,
écrivez la politique de résilience de chacune : budget de délai, faut-il retenter et combien de
fois, un disjoncteur est-il justifié, quel lot de cloisonnement. Justifiez chaque choix par la
nature de la dépendance, et notez au moins un cas où retenter serait une faute.

## Débogage

Un ticket indique : « À chaque pic de trafic, tout le site devient lent, pas seulement la page de
recommandations qui appelle un service tiers connu pour ses ralentissements. »

1. **Symptôme** : une seule dépendance lente rend le service *entier* indisponible, pas seulement
   la fonction qui en dépend.
2. **Hypothèse** : absence de cloisonnement — les appels vers le service tiers accaparent le pool
   de fils partagé, ne laissant plus rien aux autres requêtes.
3. **Preuve** : observer que les fils d'exécution sont tous en attente sur le même appel distant, et
   que les délais de réponse des autres pages montent en même temps que ceux du service tiers.
4. **Prévention** : cloisonner la dépendance lente dans un lot borné, ajouter un budget de délai
   court et un disjoncteur pour renvoyer une erreur immédiate quand elle est en panne.

## Entretien

Question posée à voix haute : *votre service appelle une dépendance qui se met à répondre en cinq
secondes au lieu de cinquante millisecondes ; que se passe-t-il, et comment l'auriez-vous prévu ?*

Une réponse solide décrit la contagion — les fils bloqués, la saturation, l'indisponibilité globale
— puis nomme les protections dans l'ordre : budget de délai pour borner l'attente, cloisonnement
pour contenir la dépendance, disjoncteur pour renvoyer une erreur immédiate. Elle mentionne le
danger du réessai sous charge et la gigue comme remède, et distingue erreur transitoire et
permanente.

### Le nom en entretien

Cette leçon parle français ; l'entretien, même en France, emploie les noms anglais. Le disjoncteur
se dit **circuit breaker**, ses états closed, open et half-open ; le recul exponentiel avec bruit se
dit **retry with exponential backoff and jitter** ; le cloisonnement se dit **bulkhead**. L'outil
que l'industrie associe à ces motifs en .NET est **Polly** : la bibliothèque de résilience qui
fournit disjoncteurs, relances et cloisons sous forme de politiques composables. Ce paragraphe est du
vocabulaire, pas une dépendance : rien dans ce parcours n'exige Polly, mais savoir nommer le motif et
citer l'outil en une phrase est exactement ce qu'un entretien senior attend.

## Résumé

- Le budget de délai borne l'attente et doit se propager de couche en couche, diminué.
- Le réessai n'a de sens que sur une erreur transitoire ; sans gigue ni borne, il amplifie la panne.
- Le disjoncteur bascule fermé, ouvert, demi-ouvert pour cesser de frapper une dépendance en panne.
- Le cloisonnement isole chaque dépendance dans un lot de ressources pour empêcher la contagion.
- Les quatre protections se composent : aucune ne remplace les autres.

## Cartes de révision

Question : quels sont les trois états d'un disjoncteur et qu'est-ce qui déclenche chaque
transition ? Réponse attendue : fermé, où les appels passent et les échecs se comptent ; ouvert,
atteint quand le seuil d'échec est franchi, où les appels sont refusés immédiatement ; demi-ouvert,
atteint après un repos, où un essai réussi referme et un essai raté rouvre.

Question : pourquoi un réessai sans disjoncteur est-il dangereux face à une dépendance en panne
durable ? Réponse attendue : le réessai continue de marteler une dépendance définitivement
indisponible, gaspille des ressources et l'empêche de récupérer, là où un disjoncteur cesserait
d'essayer et renverrait une erreur immédiate.

## Test de maîtrise

Sans relire, décrivez les quatre protections d'un appel distant, ce que chacune prévient, et
l'ordre dans lequel elles se composent. Puis expliquez le mécanisme de l'effondrement par réessai
et les deux mesures qui le cassent.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
