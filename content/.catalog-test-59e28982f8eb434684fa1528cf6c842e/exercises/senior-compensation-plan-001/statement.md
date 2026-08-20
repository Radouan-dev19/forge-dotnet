# Ordonner le plan de compensation d'une saga interrompue

Implémentez `Submission.CompensationPlan` avec la signature fournie. Sans transaction distribuée, une
opération multi-services qui échoue à mi-parcours ne s'annule pas : elle se **compense** — chaque
étape déjà exécutée est défaite par son geste inverse, dans l'ordre inverse de l'exécution. Votre
fonction reçoit le journal des étapes accomplies avant l'échec et produit le plan de compensation.

## Le catalogue des étapes

| Étape exécutée | Geste de compensation |
|---|---|
| `create-order` | `void-order` |
| `reserve-stock` | `release-stock` |
| `charge-card` | `refund-card` |
| `book-carrier` | `cancel-carrier` |
| `notify-customer` | `send-correction` |

## Ce qu'il faut produire

Le journal arrive en étapes séparées par des virgules, dans l'ordre d'exécution. Rendez les gestes de
compensation dans l'ordre **inverse**, joints par des virgules : la dernière étape exécutée se défait
la première.

```text
CompensationPlan("reserve-stock,charge-card,book-carrier")
  →  "cancel-carrier,refund-card,release-stock"
CompensationPlan("create-order,reserve-stock,charge-card")
  →  "refund-card,release-stock,void-order"
```

L'ordre inverse n'est pas une convention esthétique : les étapes tardives reposent sur les
précoces — le transporteur est réservé pour un stock réservé, le débit correspond à une commande
créée. Défaire la fondation d'abord laisse les étapes du dessus pointer dans le vide, et si la
compensation s'interrompt elle-même — elle aussi peut échouer — l'état intermédiaire de l'ordre
inverse reste toujours interprétable : un préfixe intact, un suffixe défait.

## Les refus

`ArgumentException` pour un journal vide ou blanc, une étape hors catalogue — son compensateur ne se
devine pas —, ou une étape répétée : une saga n'exécute pas deux fois la même étape, et un
remboursement double coûte plus cher qu'un refus.

## Avant d'écrire

Écrivez le plan pour le journal complet des cinq étapes, puis pour un journal réduit à la
notification. Dites pourquoi l'étape qui a échoué — celle qui a interrompu la saga — ne figure
jamais dans le journal reçu, et ce que cela suppose de l'atomicité de chaque étape.
