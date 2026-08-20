# Séparer le message public du journal sur un échec de connexion

Implémentez `Submission.LoginResponse` avec la signature fournie. Un formulaire de connexion parle à
deux publics à la fois : l'appelant, qui ne doit rien apprendre qu'il ne sache déjà, et le journal de
sécurité, qui doit tout savoir. Votre fonction produit les deux faces de la réponse à partir du
résultat décrit d'une tentative.

## Le format de la tentative

Trois paires clé-valeur, points-virgules en séparateur, ordre libre :

- `account` — `known` ou `unknown` ;
- `password` — `correct`, `wrong` ou `expired` (correct mais périmé) ;
- `state` — l'état du compte : `active` ou `locked`.

## Les deux faces

Rendez `public|journal`. Les causes s'évaluent dans cet ordre strict :

1. `account=unknown` → `invalid-credentials|unknown-account` ;
2. `state=locked` → `invalid-credentials|locked-account` — même un mot de passe correct ne connecte
   pas un compte verrouillé, et le dire publiquement confirmerait que le compte existe ;
3. `password=wrong` → `invalid-credentials|wrong-password` ;
4. `password=expired` → `password-expired|expired-password` — la seule cause nommée publiquement,
   parce qu'elle n'est atteignable qu'après la preuve du mot de passe : l'appelant a démontré qu'il
   est le titulaire, l'informer ne renseigne aucun attaquant ;
5. sinon → `success|success`.

```text
LoginResponse("account=unknown;password=wrong;state=active")  →  "invalid-credentials|unknown-account"
LoginResponse("account=known;password=wrong;state=active")    →  "invalid-credentials|wrong-password"
LoginResponse("account=known;password=correct;state=active")  →  "success|success"
```

Le principe : la face publique s'uniformise sur tout ce qui précède la preuve du mot de passe — trois
causes, un seul message — pendant que le journal garde la cause exacte pour les défenseurs, qui
comptent les comptes inconnus, les verrouillages et les mots de passe erronés séparément pour voir
venir une attaque.

## Les refus

`ArgumentException` pour une tentative mal décrite : paire illisible, clé dupliquée, attribut
absent ou valeur imprévue.

## Avant d'écrire

Prédisez la réponse d'un mot de passe correct sur un compte verrouillé, puis d'un mot de passe périmé
sur ce même compte. Dites pourquoi la première ne nomme rien publiquement et pourquoi l'ordre des
causes suffit à trancher la seconde.
