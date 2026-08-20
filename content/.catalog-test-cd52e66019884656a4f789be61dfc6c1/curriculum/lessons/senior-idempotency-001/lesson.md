# Idempotence, rejeu et sémantiques de livraison

## Objectif observable

À la fin de cette leçon, vous saurez définir l'idempotence, expliquer comment une clé d'idempotence
neutralise un rejeu de requête, distinguer la livraison au moins une fois de la livraison exactement
une fois, et justifier pourquoi cette dernière, de bout en bout, est un mythe que l'on remplace par
un consommateur idempotent.

## Prérequis

- Avoir lu `senior-resilience-001` : le réessai, et le fait qu'un appel réessayé peut arriver deux
  fois côté serveur.
- Savoir qu'une réponse perdue en chemin n'implique pas que l'action côté serveur n'a pas eu lieu.

## Intuition

Le réessai que vous avez appris à faire proprement crée un problème nouveau : si le premier appel a
bien agi mais que sa réponse s'est perdue, le réessai déclenche une *seconde* action. Vous avez
peut-être débité deux fois le même client. L'idempotence est la propriété qui rend ce doublon
inoffensif : une opération est idempotente si l'exécuter une fois ou dix fois laisse le système dans
le même état. La question centrale n'est pas « comment garantir qu'un message n'arrive qu'une fois »
— c'est presque impossible — mais « comment faire pour que peu importe le nombre de fois où il
arrive, le résultat soit le même ». Le fardeau se déplace de la livraison vers le traitement.

## Explication

**Idempotent ne veut pas dire sans effet.** Une opération idempotente peut parfaitement changer
l'état du système ; ce qu'elle garantit, c'est que la *répéter* ne le change pas davantage. Fixer
un solde à cent est idempotent : le fixer trois fois de suite laisse cent. Ajouter dix au solde ne
l'est pas : trois répétitions ajoutent trente. Cette distinction est la clé pratique. Beaucoup
d'opérations métier sont naturellement non idempotentes — un débit, une création, un envoi — et
c'est précisément celles-là qu'il faut protéger, car ce sont elles que le réessai duplique
dangereusement.

**La clé d'idempotence identifie une intention, pas une requête.** Le client génère un identifiant
unique par *action métier voulue* — pas par tentative réseau — et l'envoie avec chaque tentative de
cette action. Le serveur tient un registre des clés déjà traitées. À la première réception d'une
clé, il exécute l'opération, enregistre le résultat sous cette clé, et le renvoie. À toute
réception ultérieure de la *même* clé, il ne réexécute rien : il renvoie le résultat mémorisé. Le
client qui réessaie après une réponse perdue envoie la même clé ; il reçoit le résultat de la
première exécution, comme si le réseau n'avait jamais failli. Le point subtil est que la clé doit
survivre au réessai côté client : si le client en génère une nouvelle à chaque tentative, la
protection disparaît, car le serveur voit deux intentions distinctes là où il n'y en a qu'une.

**Le premier appel et le rejeu doivent donner le même résultat visible.** Pour l'appelant, il ne
doit y avoir aucune façon de distinguer « j'ai réussi du premier coup » de « ma première réponse
s'est perdue et j'ai réessayé ». Le serveur renvoie dans les deux cas le même corps et le même
statut. C'est ce qui rend le réessai sûr : le client n'a pas à savoir si son action a déjà eu lieu,
il rejoue et obtient la vérité. Attention à la fenêtre de concurrence : si deux tentatives portant
la même clé arrivent *en même temps*, avant que la première n'ait fini, le registre doit les
sérialiser — verrou ou contrainte d'unicité — sinon les deux s'exécutent et le doublon revient par
la porte que l'on croyait fermée.

**Les sémantiques de livraison décrivent des garanties, pas des vœux.** *Au plus une fois* : le
message est envoyé sans réessai ; s'il se perd, il est perdu — jamais de doublon, mais des pertes
possibles. *Au moins une fois* : on réessaie jusqu'à confirmation ; jamais de perte, mais des
doublons possibles. Ces deux garanties sont réelles et faciles à obtenir. La troisième, *exactement
une fois*, promet ni perte ni doublon — et c'est là que le vocabulaire trompe.

**Exactement une fois de bout en bout est un mythe.** Le problème est fondamental : entre deux
parties séparées par un réseau, il est impossible de garantir qu'une action est *à la fois*
effectuée et connue comme effectuée par les deux côtés, car tout message de confirmation peut se
perdre à son tour. On ne peut donc pas savoir, en cas de silence, si l'action a eu lieu — d'où le
réessai, d'où le doublon possible. Ce qu'on appelle « exactement une fois » dans les systèmes réels
n'est pas une livraison magique sans doublon : c'est une livraison *au moins une fois* combinée à un
*traitement idempotent* qui neutralise les doublons à l'arrivée. Le message peut arriver deux fois ;
le consommateur, grâce à la clé, ne l'applique qu'une fois. L'effet observable est « exactement une
fois », mais le mécanisme est « au moins une fois plus idempotence ». Un ingénieur qui promet
l'exactement-une-fois sans idempotence promet ce qu'aucun réseau ne peut tenir.

**La conséquence pratique tient en une phrase.** Ne cherchez pas à empêcher les doublons sur le
réseau ; rendez leur traitement inoffensif. C'est plus simple, plus robuste, et cela reste vrai
quelle que soit la façon dont le réseau vous trahit.

## Exemple commenté

Le noyau décidable est le résultat d'une première exécution comparé à celui d'un rejeu — noyau de
l'exercice guidé :

```csharp
public record Outcome(string Key, int Status, string Body, bool WasReplay);

// Le registre mémorise la clé et son résultat. Première clé : exécuter et mémoriser.
// Clé déjà vue : renvoyer le résultat mémorisé, sans réexécuter.
public static Outcome Handle(
    IDictionary<string, (int Status, string Body)> store,
    string key,
    Func<(int Status, string Body)> execute)
{
    if (store.TryGetValue(key, out var saved))
    {
        // Rejeu : même statut, même corps que la première fois. Aucune réexécution.
        return new Outcome(key, saved.Status, saved.Body, WasReplay: true);
    }

    var result = execute();          // Première exécution de cette intention.
    store[key] = result;             // Mémorisation sous la clé.
    return new Outcome(key, result.Status, result.Body, WasReplay: false);
}
```

Le premier appel porte `WasReplay: false` et exécute ; tout appel suivant de la même clé porte
`WasReplay: true` et renvoie le résultat mémorisé, indiscernable du premier pour l'appelant.

## Contre-exemple et erreur fréquente

Le code fautif génère une nouvelle clé à chaque tentative :

```csharp
// FAUTIF : la clé est régénérée à chaque réessai, donc chaque tentative
// est vue comme une intention nouvelle par le serveur.
async Task<Response> Charge(decimal amount)
{
    var key = Guid.NewGuid().ToString(); // regénéré à chaque appel : faute
    return await client.PostChargeAsync(amount, idempotencyKey: key);
}
```

Le symptôme est un double débit lors d'un réessai après réponse perdue : le serveur voit deux clés
distinctes et exécute deux fois. La clé doit être fixée *avant* la première tentative et réutilisée
pour toutes les suivantes :

```csharp
// CORRIGÉ : une clé par intention métier, générée une fois, réutilisée à chaque réessai.
async Task<Response> Charge(decimal amount, string idempotencyKey)
{
    return await RetryWithBackoff(() =>
        client.PostChargeAsync(amount, idempotencyKey)); // même clé à chaque tentative
}
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : si un client réessaie un paiement après une réponse perdue,
qu'est-ce qui fait que le second appel ne débite pas une seconde fois ?

:::quiz
id=senior-idempotency-001-check
question=Pourquoi dit-on que la livraison exactement une fois de bout en bout est un mythe ?
option=Parce que les réseaux modernes sont trop lents pour garantir un seul envoi
option=Parce qu'on ne peut jamais savoir avec certitude, en cas de confirmation perdue, si l'action a eu lieu ; on obtient l'effet par une livraison au moins une fois plus un traitement idempotent
option=Parce que les files de messages perdent toujours au moins un message sur mille
correct=1
success=Exact : l'impossibilité vient de la confirmation qui peut se perdre. On simule l'exactement-une-fois par au moins une fois plus idempotence à l'arrivée.
retry=Repensez à ce qui arrive quand le message de confirmation lui-même se perd, et à qui neutralise alors le doublon.
:::

## Exercice guidé

Ouvrez l'exercice `senior-idempotency-key-001` dans `/practice`, puis procédez ainsi.

1. Tenez un registre associant chaque clé d'idempotence à son résultat mémorisé.
2. À la première réception d'une clé, exécutez l'opération et enregistrez le résultat.
3. À toute réception ultérieure de la même clé, renvoyez le résultat mémorisé sans réexécuter.
4. Prédisez le résultat de chaque cas, dont une clé nouvelle puis un rejeu de cette même clé.

## Exercice autonome

Pour un point d'entrée de création de commande, concevez le protocole d'idempotence complet : où le
client génère la clé, comment elle survit au réessai, ce que le serveur stocke, combien de temps il
le conserve, et comment il traite deux requêtes concurrentes portant la même clé. Écrivez la réponse
attendue pour une première requête, puis pour son rejeu.

## Débogage

Un ticket indique : « Certains clients sont débités deux fois, mais seulement quand le réseau est
mauvais ; en conditions normales tout va bien. »

1. **Symptôme** : doublons de débit corrélés aux mauvaises conditions réseau, donc aux réessais.
2. **Hypothèse** : la clé d'idempotence est régénérée à chaque tentative, ou le point d'entrée n'en
   exige aucune, si bien que le serveur voit deux intentions distinctes.
3. **Preuve** : comparer les clés reçues lors des tentatives d'un même paiement ; deux clés
   différentes pour une seule intention confirment la faute.
4. **Prévention** : générer la clé une fois par intention côté client, la réutiliser à chaque
   réessai, et rendre le point d'entrée refuser une action sensible sans clé.

## Entretien

Question posée à voix haute : *comment garantissez-vous qu'un paiement réessayé ne débite pas deux
fois le client ?*

Une réponse solide refuse de promettre l'exactement-une-fois sur le réseau, explique pourquoi c'est
impossible, puis décrit la clé d'idempotence : une par intention, réutilisée au réessai, un registre
côté serveur qui renvoie le résultat mémorisé pour toute clé déjà vue. Elle mentionne la fenêtre de
concurrence et la façon de la fermer, et conclut que l'effet exactement-une-fois vient d'une
livraison au moins une fois plus un traitement idempotent.

### Le nom en entretien

Les termes de cette leçon ont leurs noms anglais consacrés : la clé d'idempotence se dit
**idempotency key** — l'en-tête HTTP du même nom est une convention répandue des interfaces de
paiement —, la livraison au moins une fois se dit **at-least-once delivery**, et son idéal
inaccessible **exactly-once**. Deux noms d'outils reviennent dans ces conversations : **RabbitMQ** et
**Kafka**, les deux messageries que l'industrie cite, promettent chacune l'at-least-once — c'est
précisément ce qui rend le consommateur idempotent obligatoire chez elles. Vocabulaire, pas
dépendance : aucun exercice n'en installe, mais l'entretien attend ces mots-là.

## Résumé

- Idempotent signifie que répéter l'opération ne change pas l'état au-delà de la première exécution.
- La clé d'idempotence identifie une intention métier et doit être réutilisée à chaque réessai.
- Premier appel et rejeu renvoient le même résultat visible : l'appelant ne peut pas les distinguer.
- Au plus une fois perd sans doubler ; au moins une fois double sans perdre ; ces deux-là sont réels.
- Exactement une fois de bout en bout est un mythe : c'est au moins une fois plus consommateur
  idempotent.

## Cartes de révision

Question : quelle est la différence entre « fixer le solde à cent » et « ajouter dix au solde » du
point de vue de l'idempotence ? Réponse attendue : fixer à cent est idempotent — trois répétitions
laissent cent — tandis qu'ajouter dix ne l'est pas — trois répétitions ajoutent trente ; ce sont les
opérations non idempotentes qu'il faut protéger.

Question : que doit faire le serveur quand il reçoit une clé d'idempotence déjà connue ? Réponse
attendue : ne rien réexécuter et renvoyer le résultat mémorisé lors de la première exécution, avec le
même statut et le même corps, pour que le rejeu soit indiscernable du premier appel.

## Test de maîtrise

Sans relire, expliquez le mécanisme complet d'une clé d'idempotence, de sa génération côté client à
la réponse du serveur sur un rejeu. Puis distinguez les trois sémantiques de livraison et démontrez
pourquoi l'exactement-une-fois de bout en bout se ramène à au moins une fois plus idempotence.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
