# Résoudre les relecteurs exigés d'une demande par propriété des chemins

Implémentez `Submission.RequiredReviewers` avec la signature fournie. Les fichiers d'un dépôt ont des
propriétaires — l'équipe ou la personne qui répond de chaque zone — et une demande de fusion convoque
les propriétaires des fichiers qu'elle touche. Votre fonction résout cette convocation.

## Les formats

Les propriétés : des entrées `préfixe=propriétaire` séparées par des points-virgules — un préfixe
terminé par `/` couvre un répertoire, un préfixe sans `/` final désigne un fichier exact. Les
fichiers touchés : des chemins séparés par des virgules.

## La résolution

Pour chaque fichier, le propriétaire du **préfixe le plus long** qui le couvre est convoqué — la
propriété la plus spécifique connaît le mieux le code, et convoquer toute la chaîne des propriétaires
imbriqués transformerait chaque demande en réunion plénière. La propriété d'un fichier exact bat
toujours celle de ses répertoires. Rendez les propriétaires convoqués, **distincts**, triés par ordre
ordinal, joints par des virgules.

```text
RequiredReviewers("src/api/=alice;src/=bob;docs/=carol", "src/api/users.cs,src/core/db.cs")
  →  "alice,bob"
RequiredReviewers("src/api/=alice;src/=bob;docs/=carol", "docs/readme.md")
  →  "carol"
```

## Les refus

`ArgumentException` pour des propriétés ou des fichiers vides, une entrée illisible, un préfixe
déclaré deux fois, ou un fichier qu'**aucun** préfixe ne couvre : une zone sans propriétaire est
exactement le trou que la politique existe pour fermer — le code qui n'appartient à personne n'est
relu par personne.

## Avant d'écrire

Prédisez la convocation quand un fichier hérité porte sa propriété exacte au milieu du répertoire
d'un autre, puis quand trois fichiers du même répertoire partent dans la même demande. Dites pourquoi
la déduplication n'est pas un détail : que devient la revue si la même personne reçoit trois
convocations pour une demande ?
