# Négocier le type de représentation

Implémentez `Submission.SelectMediaType` avec la signature fournie. Le client dit ce qu'il sait lire,
le serveur ce qu'il sait produire ; votre fonction dit ce qui sera envoyé.

## Le format des deux listes

`accepted` est un en-tête de négociation : des types de média séparés par des virgules, chacun
pouvant porter un facteur de qualité `;q=` entre 0 et 1. Sans facteur, la qualité vaut 1. Le type
passe-partout `*/*` désigne n'importe quel type.

`supported` est la liste des types que le serveur sait produire, **dans son ordre de préférence**.

```text
accepted  = "text/html;q=0.8, application/json;q=0.9"
supported = "text/html,application/json"
```

## La règle

La qualité demandée par le client classe. L'ordre du serveur ne sert qu'à départager deux types de
même qualité — sans quoi la réponse dépendrait de l'ordre dans lequel le client a écrit sa liste.

Une qualité nulle n'est pas une absence de préférence : c'est un **refus explicite**, et le type
concerné est écarté même si le passe-partout l'autoriserait par ailleurs.

Un `accepted` vide ou fait de blancs signifie que le client n'exprime aucune préférence : le premier
type du serveur convient.

Aucune correspondance rend une **chaîne vide** — au serveur d'en tirer un refus, pas à cette
fonction. Une entrée absente lève `ArgumentNullException`.

Les types de média se comparent sans tenir compte de la casse, mais le résultat rend la forme écrite
par le serveur.

## Avant d'écrire

Prédisez quatre cas : deux qualités différentes, deux qualités égales, un refus explicite couplé à un
passe-partout, et un client qui ne demande rien. Nommez ce qui se passerait si l'ordre du client
l'emportait sur la qualité.
