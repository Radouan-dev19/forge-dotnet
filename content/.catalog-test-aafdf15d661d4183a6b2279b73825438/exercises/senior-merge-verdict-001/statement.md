# Décider la fusion d'une demande depuis l'état de ses revues

Implémentez `Submission.MergeVerdict` avec la signature fournie. La règle « deux approbations pour
fusionner » a l'air simple jusqu'aux cas qui comptent : l'approbation donnée avant un remaniement
complet, l'opposition isolée face à trois approbations, la demande fraîche sans aucune revue. Votre
fonction rend le verdict de fusion depuis le relevé des revues et le seuil exigé.

## Le format du relevé

Des revues `relecteur=état` séparées par des points-virgules, dans l'ordre où elles ont été
déposées. Les états : `approved`, `changes-requested`, `stale` (une approbation donnée avant que la
demande ne change — elle a validé un autre code). Un relevé **vide** est légitime : la demande vient
d'ouvrir.

## Le verdict

1. au moins une demande de changements → `blocked|changes:relecteur` — le **premier** opposant dans
   l'ordre du relevé, celui qu'il faut aller voir. Une opposition ne se vote pas : trois
   approbations à côté ne l'effacent pas, seule la personne qui l'a posée peut la lever ;
2. sinon, les approbations **non périmées** se comptent face au seuil : atteint → `merge` ; sinon →
   `blocked|approvals:obtenues/exigées`.

```text
MergeVerdict("alice=approved;bob=approved", 2)            →  "merge"
MergeVerdict("alice=approved;bob=changes-requested", 2)   →  "blocked|changes:bob"
MergeVerdict("alice=approved;bob=stale", 2)               →  "blocked|approvals:1/2"
```

L'approbation périmée est le piège du triptyque : elle a existé, elle est affichée, et elle ne vaut
rien — la personne a validé un code qui n'est plus celui de la demande. La compter fusionnerait un
code que personne n'a relu ; c'est précisément le trou que les plateformes ferment avec l'option qui
invalide les approbations à chaque nouveau commit.

## Les refus

`ArgumentOutOfRangeException` pour un seuil non positif. `ArgumentException` pour une revue
illisible, un état hors vocabulaire, ou un relecteur présent deux fois — une personne n'a qu'une
voix, la dernière plateforme venue le garantit.

## Avant d'écrire

Prédisez le verdict d'un relevé vide sous un seuil de deux, puis d'un relevé où l'unique demande de
changements est suivie de quatre approbations fraîches. Dites pourquoi le second verdict est le bon,
même s'il frustre quatre personnes.
