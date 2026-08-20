# Choisir le canal d'une valeur selon sa sensibilité et son consommateur

Implémentez `Submission.SecretChannel` avec la signature fournie. « Mettez ça dans le coffre » est
un réflexe, pas une politique : le bon canal d'une valeur dépend de sa sensibilité, de qui la
consomme et de sa rotation. Votre fonction lit ce profil et recommande le canal motivé.

## Le format du profil

Trois paires clé-valeur jointes par des points-virgules ; leur ordre n'a aucune importance :

- `sensitivity` — `public` (une adresse de service, un nom de file) ou `secret` (ce dont la fuite
  fait un incident) ;
- `consumer` — `platform-hosted` (l'application tourne sur la plateforme, avec identité gérée
  possible) ou `local-dev` (le poste d'une personne qui développe) ;
- `rotation` — `static` ou `rotated` (la valeur change périodiquement).

## La recommandation

Rendez `canal|raison`, en appliquant les règles dans cet ordre :

1. `sensitivity=public` → `configuration|not-a-secret` : monter du non-sensible dans un canal de
   secret rend la configuration illisible et banalise le canal ;
2. `consumer=platform-hosted` → `managed-identity|no-stored-credential` : l'identité attestée par la
   plateforme supprime le secret stocké au lieu de le déplacer — il n'y a plus rien à faire fuiter ;
3. `consumer=local-dev` et `rotation=rotated` → `key-vault|central-rotation` : seule une source
   centrale sert la valeur courante après chaque rotation ;
4. `consumer=local-dev` et `rotation=static` → `user-secrets|out-of-git` : le magasin utilisateur
   garde la valeur hors du dépôt, sans infrastructure.

```text
SecretChannel("sensitivity=secret;consumer=platform-hosted;rotation=rotated")
  →  "managed-identity|no-stored-credential"
SecretChannel("sensitivity=secret;consumer=local-dev;rotation=static")
  →  "user-secrets|out-of-git"
```

## Les refus

`ArgumentException` quand la description ne tient pas debout : paire illisible, répétition de clé,
attribut manquant, valeur hors du vocabulaire.

## Avant d'écrire

Prédisez le canal d'une valeur secrète et tournante consommée par une application hébergée, et dites
pourquoi la rotation n'a pas pesé dans la réponse. Puis expliquez la régression que l'identité
attestée interrompt : avec quoi s'authentifie-t-on auprès du gardien des secrets ?
