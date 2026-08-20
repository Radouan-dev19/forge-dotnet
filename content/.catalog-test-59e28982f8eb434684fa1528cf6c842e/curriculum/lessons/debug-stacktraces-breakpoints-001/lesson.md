# Lire une stack trace et cibler un breakpoint

## Objectif observable

À la fin de cette leçon, vous saurez extraire d'une trace d'exception la ligne applicative utile et
la cause probable, distinguer le point de levée du point d'origine, et poser un point d'arrêt
conditionnel qui ne s'arrête que sur l'état fautif.

## Prérequis

- Avoir lu `structures-trees-001` et savoir raisonner sur une profondeur d'appels.
- Savoir exécuter un programme sous débogueur et ouvrir la fenêtre des variables locales.

## Intuition

Une trace d'exception n'est pas un message d'erreur : c'est le chemin d'appels au moment précis où
l'exception a été levée, la plus récente en haut. Elle dit **où ça a cassé**. Elle ne dit presque
jamais **pourquoi**.

La différence est tout le métier du débogage : le point de levée est là où une hypothèse fausse a
enfin produit un effet visible ; la cause est en amont, là où cette hypothèse a été introduite.

## Explication

**Lire une trace dans le bon ordre.** La première ligne est l'endroit exact de la levée. Les lignes
suivantes remontent la chaîne des appelants jusqu'au point d'entrée. Le réflexe utile n'est pas de
lire la première ligne mais de chercher **la première ligne de votre code** : les cadres de la
bibliothèque standard ou du framework sont rarement fautifs, ils ne font que subir une entrée
invalide qu'on leur a passée.

Une trace comporte trois informations souvent négligées : le **type** de l'exception, qui restreint
déjà énormément les causes possibles ; le **message**, qui contient parfois la valeur fautive ; et le
numéro de ligne, qui n'est fiable que si les symboles de débogage correspondent au binaire exécuté.

**Le type d'exception est un diagnostic à lui seul.** `NullReferenceException` signifie qu'une
référence attendue non nulle était nulle — la cause est en amont, là où elle a été affectée ou pas
affectée. `InvalidOperationException` signale un appel légitime fait au mauvais moment ou dans un
mauvais état. `KeyNotFoundException` révèle une hypothèse de présence non vérifiée. Chacune oriente
vers une famille de causes différente.

**`InnerException` porte souvent la vraie cause.** Beaucoup de couches encapsulent : un appel de base
de données qui échoue remonte en `DbUpdateException`, dont l'`InnerException` contient la vraie erreur
SQL avec le nom de la contrainte violée. S'arrêter à l'exception externe, c'est lire le résumé au lieu
du rapport. Sur une opération concurrente, `AggregateException` en contient même plusieurs :
`Flatten()` puis parcours de `InnerExceptions` est le geste correct.

**Async change l'apparence de la trace.** Avec `await`, la pile physique ne correspond plus à la
logique métier : vous verrez des cadres de machine à états générés par le compilateur.
`await` préserve néanmoins la trace d'origine, contrairement à `.Result` et `.Wait()` qui encapsulent
tout dans une `AggregateException` et brouillent la lecture. C'est une raison de plus, développée dans
`async-fundamentals-001`, de ne jamais bloquer sur une tâche.

**Le point d'arrêt conditionnel est ce qui rend le débogueur utilisable.** Sur une boucle de dix mille
tours, s'arrêter à chaque passage est inexploitable. Une condition — `quantity < 0`, `line == 842`,
`customer.Id == "C-193"` — suspend l'exécution uniquement sur l'état recherché. C'est la différence
entre feuilleter un livre et l'ouvrir à la bonne page.

Trois variantes complètent l'outil. Le **compteur de passages** s'arrête au n-ième tour, utile quand
on sait que le problème survient tard. Le **point de trace** écrit un message sans suspendre : c'est
un affichage de débogage qu'on n'a pas besoin de retirer du code ensuite. Le **point d'arrêt sur
exception** suspend au moment de la levée, avant que la pile ne soit déroulée — indispensable quand un
`catch` en amont avale l'information.

**La méthode qui structure tout.** Reproduire avec des valeurs connues ; formuler une hypothèse
falsifiable ; choisir l'endroit où cette hypothèse serait **réfutée** ; observer ; conclure. La
troisième étape est celle qui distingue un débogage de quinze minutes d'un après-midi perdu : on ne
pose pas un point d'arrêt là où l'on croit que ça casse, mais là où l'on pourrait prouver qu'on a
tort.

## Exemple commenté

Trace obtenue en production, réduite à l'essentiel :

```text
System.NullReferenceException: Object reference not set to an instance of an object.
   at Forge.Billing.InvoiceFormatter.Format(Invoice invoice) in InvoiceFormatter.cs:line 42
   at Forge.Billing.InvoiceService.Send(String invoiceId) in InvoiceService.cs:line 118
   at Forge.Api.InvoiceController.Post(String invoiceId) in InvoiceController.cs:line 27
```

Lecture. Le type dit qu'une référence attendue non nulle était nulle. La ligne 42 est le point de
**levée** : c'est là que l'on a déréférencé. Mais `Format` reçoit `invoice` : si l'objet est nul, la
cause est dans `Send`, à la ligne 118 — c'est-à-dire un cadre plus bas.

L'erreur de débutant est de corriger la ligne 42 en ajoutant un test de nullité. Le symptôme
disparaît, la facture n'est plus envoyée, et personne ne sait pourquoi.

Le point d'arrêt utile n'est donc pas ligne 42 mais ligne 118, avec une condition qui ne s'arrête que
sur le cas fautif :

```csharp
public void Send(string invoiceId)
{
    Invoice? invoice = _repository.Find(invoiceId);

    // Point d'arrêt conditionnel posé ici, condition : invoice is null
    // On observe alors invoiceId pour savoir QUELLE facture est introuvable —
    // l'information que la trace ne donnait pas.
    _formatter.Format(invoice!);
}
```

La correction définitive ne consiste pas à tester la nullité au point de levée, mais à rendre le
contrat explicite au point d'origine :

```csharp
Invoice invoice = _repository.Find(invoiceId)
    ?? throw new InvalidOperationException($"Facture « {invoiceId} » introuvable.");
```

L'exception nomme désormais la facture. La prochaine occurrence sera diagnostiquée en lisant le
journal, sans débogueur.

## Contre-exemple et erreur fréquente

```csharp
public decimal ComputeTotal(string orderId)
{
    try
    {
        return _repository.Load(orderId).Lines.Sum(line => line.Amount);
    }
    catch (Exception exception)
    {
        _logger.LogError("Erreur de calcul");   // Ni le type, ni le message, ni la pile.
        throw new Exception("Calcul impossible");   // La trace d'origine est perdue.
    }
}
```

Le journal produit une ligne inexploitable. Pire, la nouvelle exception **remplace** la trace
d'origine : l'endroit réel de la levée a disparu, et le seul cadre visible est celui du `catch`. Le
diagnostic devient impossible sans reproduire localement.

Trois gestes corrigent cela. Passer l'exception au journal — `_logger.LogError(exception, "…")` —
pour conserver type, message et pile. Utiliser `throw;` seul si l'on veut relancer, car il préserve
la trace, contrairement à `throw exception;` qui la réinitialise. Et si l'on encapsule volontairement,
passer l'exception d'origine en `innerException` du nouveau constructeur, pour qu'elle reste
atteignable.

## Vérification de compréhension

Sur la trace donnée en exemple, dites quelle ligne est le point de levée, quelle ligne est le point
de cause probable, et quelle condition de point d'arrêt vous poseriez.

:::quiz
id=debug-stacktraces-breakpoints-001-check
question=Une méthode attrape une exception, la journalise, puis lève `throw exception;`. Qu'arrive-t-il à la trace d'origine ?
option=Elle est conservée intégralement, throw et throw exception sont équivalents
option=Elle est réinitialisée à partir du point de relance : l'endroit réel de la levée est perdu
option=Elle est conservée mais déplacée dans InnerException automatiquement
correct=1
success=Correct : seul `throw;` sans opérande préserve la pile d'origine. `throw exception;` repart du point courant et efface l'information la plus utile.
retry=Relisez la fin du contre-exemple : trois gestes distinguent une relance qui conserve l'information d'une relance qui la détruit.
:::

## Exercice guidé

Ouvrez `debug-stack-origin-001` dans `/practice`, puis procédez ainsi.

1. Lisez la trace fournie et notez, avant tout code, le type, le point de levée et le point de cause
   probable.
2. Formulez une hypothèse falsifiable en une phrase.
3. Choisissez l'endroit où cette hypothèse serait réfutée, et écrivez la condition du point d'arrêt.
4. Observez sans modifier, puis corrigez à l'origine et non au point de levée.

Le DebugLab `debug-stacktrace-origin-001` propose le même exercice avec un dépôt cassé complet et un
journal de bug à remplir.

## Exercice autonome

Vous recevez cette trace : `KeyNotFoundException` levée dans une méthode `Resolve` appelée par un
chargement de configuration au démarrage.

Écrivez, sans voir le code : les trois causes possibles classées par probabilité, la condition de
point d'arrêt que vous poseriez pour chacune, et l'information que vous iriez lire dans la fenêtre des
variables locales.

## Débogage

Un ticket indique : « Une exception apparaît une fois sur mille dans le traitement par lot, sans
indiquer la ligne concernée. »

1. **Symptôme** : rare, non reproductible à la demande, trace sans contexte métier.
2. **Hypothèse** : une seule entrée du lot porte une valeur hors contrat.
3. **Preuve** : posez un point d'arrêt sur exception plutôt qu'un point d'arrêt de ligne — il suspend
   au moment de la levée, avant qu'un `catch` en amont n'avale le contexte. Lisez alors l'élément en
   cours de traitement.
4. **Prévention** : enrichissez le message d'exception avec l'identifiant de la ligne traitée, et
   ajoutez un test portant sur cette valeur.

## Entretien

Question posée à voix haute : *on vous donne une trace d'exception de production. Quelles sont vos
trois premières actions ?*

Une réponse solide cite le type d'exception comme premier filtre, la recherche de la première ligne
de code applicatif, et l'inspection de `InnerException`. Elle distingue explicitement le point de
levée du point de cause, et n'annonce pas de correction avant d'avoir formulé une hypothèse.

## Résumé

- La trace dit où ça a cassé, presque jamais pourquoi.
- Le type d'exception restreint déjà la famille de causes.
- `InnerException` porte souvent la cause réelle.
- Un point d'arrêt se pose là où l'hypothèse serait réfutée, pas là où l'on croit que ça casse.
- `throw;` préserve la pile ; `throw exception;` la détruit.

## Cartes de révision

Question : pourquoi corriger au point de levée est-il souvent une erreur ? Réponse attendue : cela
supprime le symptôme sans traiter la cause, qui se situe dans un cadre appelant.

Question : quel type de point d'arrêt utiliser quand un `catch` en amont avale l'information ?
Réponse attendue : le point d'arrêt sur exception, qui suspend au moment de la levée.

## Test de maîtrise

Sans relire, décrivez la démarche complète pour diagnostiquer une `InvalidOperationException` levée
dans une méthode appelée depuis trois endroits différents. Nommez l'hypothèse, le point d'arrêt et sa
condition, ce que vous observez, et la non-régression que vous ajoutez.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
