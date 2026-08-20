# Décider le sort des drapeaux de fonctionnalité vieillissants

Implémentez `Submission.FlagRetirements` avec la signature fournie. Les drapeaux de fonctionnalité
naissent utiles et meurent rarement : la base héritée typique en accumule des dizaines dont l'issue
est jouée depuis des mois, et chaque lecture du code traverse leurs branchements fantômes. Votre
fonction audite le registre et décide, drapeau par drapeau, du sort de ceux qui peuvent partir.

## Le format du registre

Des drapeaux `nom:état:âgeJours` séparés par des points-virgules :

- `état` — `on-for-all` (allumé pour tout le monde), `off-for-all` (éteint pour tout le monde) ou
  `mixed` (il pilote encore des populations différentes) ;
- `âgeJours` — depuis combien de jours l'état est stable.

## La décision

Un drapeau se retire quand son issue est **jouée** — état stable des deux côtés impossibles à
confondre — et **assumée** — l'âge atteint le minimum donné, car une issue trop récente peut encore
s'inverser. Le sort dépend du côté :

- `on-for-all` et assez vieux → `nom=inline` : le code de la fonctionnalité **reste**, le
  branchement part — la nouvelle voie devient la seule ;
- `off-for-all` et assez vieux → `nom=delete` : la branche est morte, elle part avec son drapeau ;
- `mixed`, ou trop jeune → rien : il pilote encore, ou pourrait devoir s'inverser.

Rendez les sorts `nom=sort` dans l'ordre du registre, joints par des points-virgules ; la chaîne
vide si rien ne se retire.

```text
FlagRetirements("new-checkout:on-for-all:120;old-flow:off-for-all:200;beta-search:mixed:400", 90)
  →  "new-checkout=inline;old-flow=delete"
FlagRetirements("fresh:on-for-all:10", 90)
  →  ""
```

La distinction des deux sorts n'est pas cosmétique : les confondre détruit du code vivant — supprimer
la branche d'un drapeau allumé partout — ou garde du code mort pour toujours.

## Les refus

`ArgumentOutOfRangeException` pour un âge minimal non positif. `ArgumentException` pour un registre
vide, un drapeau sans ses trois champs, un état hors vocabulaire, un âge illisible ou négatif, ou un
nom répété.

## Avant d'écrire

Prédisez le sort d'un drapeau dont l'âge tombe exactement sur le minimum, puis d'un registre où tous
les drapeaux sont mixtes. Dites ce que le second cas raconte sur la discipline de l'équipe — et
pourquoi ce n'est pas à cette fonction de le corriger.
