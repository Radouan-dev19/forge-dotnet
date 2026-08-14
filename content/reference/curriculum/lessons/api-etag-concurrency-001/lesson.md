# ETag et If-Match : la concurrence optimiste, côté HTTP

## Objectif observable

À la fin de cette leçon, vous saurez calculer un ETag stable pour une représentation, décider
entre 200, 304 et 412 selon les en-têtes conditionnels reçus, et surtout reconnaître que ce
mécanisme est le même que la concurrence optimiste des bases — le jeton de version, remonté d'un
cran, dans le protocole.

## Prérequis

- Avoir lu `api-http-semantics-001` : les statuts que cette leçon aiguille.
- Avoir lu `ef-core-data-access-001` : le jeton de concurrence dont l'ETag est le cousin HTTP.

## Intuition

Un ETag est l'empreinte d'une représentation à un instant donné : deux états identiques ont le
même ETag, deux états différents en ont un différent. Muni de cette empreinte, le client peut
poser deux questions conditionnelles — « as-tu changé depuis cette empreinte ? » pour économiser
un transfert, « es-tu toujours à cette empreinte ? » avant d'écrire pour ne pas écraser le
travail d'un autre. La première question fait de la performance ; la seconde fait de la
correction.

## Explication

**L'ETag remonte le jeton de concurrence dans HTTP.** Vous avez vu dans `ef-core-data-access-001`
la concurrence optimiste en base : une colonne de version, ajoutée au `WHERE` de la mise à jour,
qui fait échouer l'écriture si la ligne a changé depuis la lecture. `sql-isolation-001` en
donnait le fondement relationnel. L'ETag est *exactement* ce mécanisme, exposé au client HTTP :
l'empreinte de la représentation joue le rôle du numéro de version, et l'en-tête conditionnel
`If-Match` joue le rôle du `WHERE` versionné. Cette continuité — de la ligne en base jusqu'à
l'appel réseau — est rarement montrée d'un bout à l'autre, et c'est le cœur de la leçon : ce
n'est pas trois mécanismes à mémoriser, c'est un seul, décliné à trois niveaux.

**Le calcul de l'ETag doit être stable et sensible.** Stable : la même représentation produit
toujours la même empreinte, indépendamment de détails non significatifs — ordre de sérialisation,
espaces. Sensible : le moindre changement de contenu qui compte change l'empreinte. Un condensat
du corps canonique remplit les deux conditions ; une version de ligne en base en tient lieu
aussi, moins chère à calculer. Le piège est l'empreinte instable — recalculée à partir d'une
sérialisation non déterministe — qui change sans que rien n'ait changé, et rend tout le mécanisme
inutile : le client revalide sans cesse, les écritures conditionnelles échouent au hasard.

**Fort ou faible : deux promesses.** Un ETag *fort* affirme l'identité octet pour octet des deux
représentations ; un ETag *faible*, marqué d'un préfixe, affirme seulement leur équivalence
sémantique — même sens, présentation possiblement différente. Le fort est requis pour les
écritures conditionnelles, où l'on veut la certitude ; le faible suffit pour la validation de
cache, où l'équivalence de sens économise déjà le transfert.

**Trois statuts, deux questions.** En lecture, `If-None-Match` porte l'empreinte que le client
possède déjà : si elle correspond à l'état actuel, la ressource n'a pas changé, et le serveur
répond **304 Non modifié** — sans corps, le client réutilise sa copie. Sinon, **200** avec le
corps et le nouvel ETag. En écriture, `If-Match` porte l'empreinte sur laquelle le client a fondé
sa modification : si elle correspond encore, l'écriture procède et rend **200** (ou 204) ; si
elle ne correspond plus — quelqu'un a écrit entre-temps —, le serveur refuse par **412 Condition
préalable échouée**, et le client doit relire l'état actuel avant de retenter. Le 412 est le
`DbUpdateConcurrencyException` du protocole : la mise à jour perdue évitée, un cran plus haut.

**Ce que le 412 protège, et ce qu'il exige du client.** Sans écriture conditionnelle, deux clients
qui lisent le même état, le modifient chacun de leur côté et enregistrent, laissent le dernier
écraser le premier en silence — la mise à jour perdue. Le 412 transforme cet écrasement muet en
refus visible : le client sait qu'il a travaillé sur un état périmé, relit, et décide — refaire
sa modification sur le nouvel état, ou la présenter à l'utilisateur. Comme en base, le refus est
technique ; la résolution du conflit est une décision métier.

## Exemple commenté

Décider le statut d'une lecture conditionnelle — le noyau de l'un des exercices :

```csharp
// If-None-Match : le client demande à n'être servi que s'il a une copie périmée.
public static int ConditionalReadStatus(string currentETag, string ifNoneMatch)
{
    // Empreinte à jour : rien à renvoyer, le client garde sa copie.
    if (string.Equals(currentETag, ifNoneMatch, StringComparison.Ordinal))
    {
        return 304;
    }

    // Copie absente ou périmée : on envoie la représentation actuelle.
    return 200;
}
```

Et la décision d'écriture, où l'empreinte devient une garde de concurrence :

```csharp
// If-Match : l'écriture n'est permise que sur l'état que le client croit connaître.
public static int ConditionalWriteStatus(string currentETag, string ifMatch)
{
    if (string.IsNullOrEmpty(ifMatch))
    {
        // Écriture aveugle refusée : sans condition, la mise à jour perdue redevient possible.
        return 428;   // Condition préalable requise.
    }

    return string.Equals(currentETag, ifMatch, StringComparison.Ordinal)
        ? 200    // L'état n'a pas bougé : l'écriture procède.
        : 412;   // L'état a changé : refus, le client doit relire.
}
```

## Contre-exemple et erreur fréquente

Le code fautif calcule un ETag depuis une sérialisation non déterministe :

```csharp
// FAUTIF : l'ordre des clés du dictionnaire n'est pas garanti d'une fois sur l'autre.
string body = SerializeInAnyOrder(resource);
string etag = Hash(body);   // La même ressource peut produire deux empreintes.
```

Le symptôme est déroutant : les clients revalident en boucle bien que rien ne change, et les
écritures conditionnelles échouent par intermittence en 412 sans conflit réel. La cause est
l'instabilité de l'empreinte. La correction impose une forme canonique avant de condenser :

```csharp
// CORRIGÉ : une représentation canonique — clés triées, espaces normalisés — puis le condensat.
string body = SerializeCanonical(resource);
string etag = Hash(body);   // Stable : même contenu, même empreinte.
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : quel statut relie l'ETag au
`DbUpdateConcurrencyException` de la leçon EF Core, et pourquoi ce lien n'est pas une analogie mais
une identité de mécanisme ?

:::quiz
id=api-etag-concurrency-001-check
question=Un client envoie If-Match avec une empreinte qui ne correspond plus à l'état actuel de la ressource. Que répond le serveur ?
option=200, en appliquant l'écriture puisque le client a fourni une condition
option=412 Condition préalable échouée : l'état a changé depuis, l'écriture est refusée pour éviter la mise à jour perdue
option=304 Non modifié, car les empreintes diffèrent
correct=1
success=Exact : If-Match qui ne correspond plus signale que quelqu'un a écrit entre-temps ; le 412 refuse l'écrasement, exactement comme le WHERE versionné fait échouer la mise à jour en base.
retry=Distinguez les deux en-têtes : If-None-Match sert la lecture (200/304), If-Match garde l'écriture (200/412). Lequel est en jeu ici ?
:::

## Exercice guidé

Ouvrez l'exercice `api-etag-compute-001` dans `/practice`, puis procédez ainsi.

1. Identifiez ce qui doit être stable dans l'empreinte et ce qui doit la faire changer.
2. Canonisez l'entrée avant de condenser, pour que l'ordre et les espaces ne comptent pas.
3. Encodez le condensat dans la forme attendue par le contrat, entre guillemets s'il le demande.
4. Vérifiez qu'une même représentation, présentée deux fois différemment, rend le même ETag.

## Exercice autonome

Décrivez, pour une ressource « commande » que vous imaginez, ce qui entre dans son ETag et ce qui
en est exclu — le statut ? la date de dernière lecture ? Justifiez chaque inclusion par la
question « ce changement doit-il invalider les caches et les écritures fondées sur l'ancien
état ? ».

## Débogage

Un ticket indique : « Nos clients se plaignent d'erreurs 412 fréquentes lors de l'enregistrement,
alors qu'ils sont seuls à modifier la ressource. »

1. **Symptôme** : 412 en l'absence de conflit réel, un seul écrivain à la fois.
2. **Hypothèse** : l'ETag est instable — recalculé depuis une sérialisation non déterministe —,
   donc l'empreinte fournie en If-Match ne correspond jamais tout à fait à celle recalculée côté
   serveur.
3. **Preuve** : demander deux fois la même ressource inchangée et comparer les ETag ; s'ils
   diffèrent, l'instabilité est confirmée.
4. **Prévention** : canoniser la représentation avant le condensat, ou dériver l'ETag d'un jeton
   de version de ligne stable plutôt que du corps sérialisé.

## Entretien

Question posée à voix haute : *qu'est-ce qu'un ETag, et comment s'en sert-on pour éviter les mises
à jour perdues ?*

Une réponse solide définit l'ETag comme l'empreinte stable d'une représentation, distingue les
deux en-têtes conditionnels et leurs statuts — 304 pour la lecture, 412 pour l'écriture —, et fait
le lien explicite avec la concurrence optimiste en base : l'If-Match est le WHERE versionné, le
412 est l'exception de concurrence. Elle mentionne l'exigence de stabilité comme la condition sans
laquelle tout le mécanisme se dérègle.

## Résumé

- L'ETag est le jeton de concurrence optimiste remonté dans HTTP — même mécanisme que la version
  de ligne en base.
- Il doit être stable — insensible à l'ordre et aux espaces — et sensible aux vrais changements.
- If-None-Match sert la lecture : 200 avec corps, ou 304 si le client a déjà l'état courant.
- If-Match garde l'écriture : 200 si l'état n'a pas bougé, 412 s'il a changé depuis.
- Le 412 rend visible la mise à jour perdue ; sa résolution est une décision métier, comme en
  base.

## Cartes de révision

Question : à quel mécanisme de base de données l'If-Match correspond-il exactement, et pourquoi ?
Réponse attendue : au `WHERE` versionné de la concurrence optimiste — il conditionne l'écriture à
l'inchangement de l'état lu, et le 412 est le pendant de l'exception de concurrence.

Question : pourquoi un ETag instable provoque-t-il des 412 sans conflit réel ? Réponse attendue :
l'empreinte change sans que le contenu change, donc l'If-Match du client ne correspond jamais à
l'empreinte recalculée, et l'écriture est refusée à tort.

## Test de maîtrise

Sans relire, écrivez la table de décision complète des deux en-têtes conditionnels — quel en-tête,
quelle correspondance, quel statut — puis reliez chaque ligne à son équivalent en base de données
ou expliquez pourquoi elle n'en a pas. Terminez en décrivant ce qui rend une empreinte stable.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
