# Observabilité distribuée : corrélation et budget d'erreur

## Objectif observable

À la fin de cette leçon, vous saurez suivre une requête unique à travers plusieurs services grâce à un identifiant de corrélation, lire un budget d'erreur pour décider si l'on continue à livrer ou si l'on gèle, et dire pourquoi une trace locale ne suffit plus dès qu'un appel traverse une frontière de service.

## Prérequis

- Avoir suivi `senior-boundaries-001` : où passe une frontière de service et ce qu'elle coûte.
- Savoir ce qu'est un objectif de niveau de service, ou SLO.

## Intuition

Sur un monolithe, une pile d'appel raconte toute l'histoire d'une requête. Dès que la requête saute d'un service à l'autre, cette histoire se fragmente : chaque service n'en voit qu'un morceau, et personne ne voit le tout. L'observabilité distribuée recolle ces morceaux avec un fil conducteur, l'identifiant de corrélation, propagé de bout en bout. Le budget d'erreur, lui, transforme la fiabilité en une décision chiffrée plutôt qu'en une intuition.

## Explication

**La corrélation recolle une histoire fragmentée.** Quand un service en appelle un autre, il transmet un identifiant de corrélation unique à la requête d'origine. Chaque service journalise ses événements avec cet identifiant. Recoller l'histoire complète revient alors à filtrer tous les journaux sur une seule valeur : on reconstitue le chemin, les durées de chaque saut, et le point exact où une erreur est apparue. Sans ce fil, un incident distribué se diagnostique à l'aveugle, en comparant des horodatages qui ne s'alignent jamais parfaitement.

**Le budget d'erreur chiffre la tolérance à la panne.** Un SLO ne demande pas zéro erreur : viser 99,90 % de succès, c'est accepter d'avance 0,10 % d'échecs. Cette part tolérée est le budget d'erreur. Tant qu'il n'est pas dépensé, on peut prendre des risques — livrer, expérimenter. Dès qu'il est épuisé, on gèle les livraisons et on remet la fiabilité au premier plan. Le budget transforme une promesse floue en une règle opérationnelle que l'on peut appliquer sans débat.

**Le budget se consomme le long d'une chaîne.** Dans un système distribué, une requête utilisateur traverse plusieurs services, et chacun consomme sa part du budget global. Un service peut afficher un taux d'erreur excellent tout en propageant les échecs d'un autre : c'est la corrélation qui permet d'attribuer un dépassement au maillon fautif, pas à celui qui n'a fait que relayer l'erreur.

**Trois signaux, une seule question.** On instrumente souvent avec des journaux, des métriques et des traces. Les journaux racontent un événement, les métriques agrègent une tendance, les traces suivent une requête. Le fil de corrélation les relie : une métrique de latence anormale mène à une trace, qui mène aux journaux du saut fautif. La question reste toujours la même : cette requête a-t-elle réussi, et sinon, où et pourquoi.

## Exemple commenté

Le noyau décidable de cette leçon décide un gel de livraison à partir d'un budget d'erreur :

```csharp
// Budget = part d'échecs tolérée par le SLO, appliquée au volume.
// Le budget est épuisé quand les échecs DÉPASSENT strictement la tolérance.
public static string BudgetDecision(int totalRequests, int failedRequests, int sloBasisPoints)
{
    long toleranceBasisPoints = 10000L - sloBasisPoints;
    long allowedFailures = (long)totalRequests * toleranceBasisPoints / 10000L;
    return failedRequests > allowedFailures ? "freeze" : "ship";
}
```

Sur mille requêtes à 99,90 %, la tolérance vaut dix points de base, donc un échec autorisé : cinq échecs dépassent le budget et gèlent les livraisons.

## Contre-exemple et erreur fréquente

Le piège classique consiste à confondre le taux de succès visé et la part d'échecs tolérée :

```csharp
// FAUTIF : on gèle dès que le taux de succès observé passe sous 100 %.
bool freeze = failedRequests > 0;
```

Le symptôme est un gel permanent : la moindre panne stoppe les livraisons, alors que le SLO autorise justement une marge. La correction calcule le budget à partir de la tolérance, `10000 - sloBasisPoints`, et ne gèle qu'au dépassement strict.

## Vérification de compréhension

Avant le quiz, répondez à voix haute : sur dix mille requêtes à 99,90 %, combien d'échecs sont tolérés avant de geler ?

:::quiz
id=senior-observability-001-check
question=Pourquoi une trace locale ne suffit-elle plus dès qu'une requête traverse plusieurs services ?
option=Parce que les journaux locaux sont chiffrés et illisibles
option=Parce que chaque service ne voit que son morceau de la requête, et seul un identifiant de corrélation propagé recolle l'histoire complète
option=Parce que les traces distribuées sont interdites par la spécification HTTP
correct=1
success=Exact : sans identifiant de corrélation propagé de bout en bout, chaque service ne voit qu'un fragment, et l'histoire complète de la requête reste invisible.
retry=Repensez à ce que voit un service isolé quand la requête a déjà traversé deux autres services avant lui.
:::

## Exercice guidé

Ouvrez l'exercice `senior-error-budget-001` dans `/practice`, puis procédez ainsi.

1. Convertissez le SLO en points de base tolérés : `10000 - sloBasisPoints`.
2. Appliquez cette tolérance au volume pour obtenir le nombre d'échecs autorisés, en division entière.
3. Gelez si les échecs observés dépassent strictement ce budget, livrez sinon.
4. Prédisez le verdict pour un cas pile au budget et un cas un échec au-dessus.

## Exercice autonome

Pour une chaîne de trois services que vous imaginez, décidez comment répartir un budget d'erreur global entre eux, et écrivez la requête de journal qui, à partir d'un identifiant de corrélation, reconstituerait le chemin complet d'une requête en échec.

## Débogage

Un ticket indique : « Les utilisateurs signalent des lenteurs intermittentes sur la commande, mais chaque service affiche des temps de réponse normaux dans son propre tableau de bord. »

1. **Symptôme** : lenteur perçue de bout en bout, invisible service par service.
2. **Hypothèse** : la latence s'accumule sur plusieurs sauts, ou un service attend un autre sans que sa propre métrique ne le montre.
3. **Preuve** : filtrer les journaux et les traces sur l'identifiant de corrélation d'une requête lente, et sommer les durées de chaque saut.
4. **Prévention** : propager systématiquement l'identifiant de corrélation et suivre une latence de bout en bout, pas seulement par service.

## Entretien

Question posée à voix haute : *comment décidez-vous, chiffres en main, s'il faut geler les livraisons cette semaine ?*

Une réponse solide définit le budget d'erreur comme la part d'échecs tolérée par le SLO, le calcule à partir de la tolérance et du volume, et gèle au dépassement strict. Elle relie ensuite le budget à la corrélation inter-service pour attribuer un dépassement au bon maillon, plutôt qu'au service qui n'a fait que propager l'erreur.

### Le nom en entretien

Le traçage distribué se dit **distributed tracing**, ses segments **spans**, le temps de travail
propre **self time**, la vitesse de consommation du budget **burn rate**, et la latence de queue
**tail latency** — p95, p99 à l'oral. L'outil que l'industrie associe à toute cette corrélation est
**OpenTelemetry** : le standard ouvert qui unifie traces, métriques et journaux, et dont les
bibliothèques instrumentent les applications .NET sans adhérence à un fournisseur d'analyse. Une
phrase suffit en entretien — le standard d'instrumentation, indépendant de l'outil d'analyse — et
c'est tout ce que ce parcours en exige : le nom, pas la dépendance.

## Résumé

- Un identifiant de corrélation propagé de bout en bout recolle l'histoire d'une requête distribuée.
- Un SLO définit un budget d'erreur : une part d'échecs tolérée, pas la perfection.
- Le budget est épuisé au dépassement strict ; on gèle alors les livraisons.
- Le budget se consomme le long d'une chaîne, et la corrélation attribue un dépassement au maillon fautif.

## Cartes de révision

Question : à quoi sert un identifiant de corrélation dans un système distribué ? Réponse attendue : à suivre une requête unique à travers tous les services qu'elle traverse, en filtrant journaux et traces sur une seule valeur.

Question : quand gèle-t-on les livraisons selon un budget d'erreur ? Réponse attendue : quand les échecs observés dépassent strictement la part tolérée par le SLO appliquée au volume.

## Test de maîtrise

Sans relire, décrivez comment vous diagnostiqueriez un incident distribué de bout en bout à partir d'un identifiant de corrélation, puis expliquez le calcul d'un budget d'erreur et la décision de gel qui en découle.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
