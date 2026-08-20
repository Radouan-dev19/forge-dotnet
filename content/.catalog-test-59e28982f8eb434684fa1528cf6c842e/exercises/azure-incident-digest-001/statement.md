# Extraire le brief structuré d'un journal d'incident

Implémentez `Submission.IncidentDigest` avec la signature fournie. Pendant un incident, deux
documents vivent en parallèle : le journal, qui accumule tout, et le brief, qui répond aux quatre
questions que tout le monde pose — quel impact, depuis quand, l'atténuation a-t-elle commencé, qui
porte la suite. Votre fonction produit le brief à partir du journal.

## Le format du journal

Des entrées `minute:type:détail` séparées par des points-virgules, aux minutes croissantes au sens
large. Les types : `alert` (un signal automatique), `impact` (un effet constaté sur les
utilisateurs), `action` (un geste d'investigation), `mitigation` (une mesure qui réduit l'effet),
`assignment` (la prochaine action attribuée à un propriétaire), `note` (tout le reste).

## Le brief

Quand le journal contient au moins un impact et une attribution, rendez :

```text
impact=<détail du premier impact>|start=<minute de la première alerte ou du premier impact>|mitigation=<minute de la première mitigation, ou none>|next=<détail de la première attribution>
```

Sinon, rendez `incomplete|` suivi des vitaux manquants parmi `impact` et `next-owner`, dans cet
ordre. Un brief aux champs vides aurait l'air complet ; le déclarer incomplet dit la vérité et nomme
le travail restant.

```text
IncidentDigest("3:alert:latency;5:impact:checkout-errors;12:mitigation:rollback;14:assignment:on-call-db")
  →  "impact=checkout-errors|start=3|mitigation=12|next=on-call-db"
IncidentDigest("2:impact:login-failures;9:assignment:security-team")
  →  "impact=login-failures|start=2|mitigation=none|next=security-team"
```

Chaque champ retient la **première** occurrence, jamais la dernière : le début est la première trace
observable — alerte ou impact —, l'atténuation compte dès sa première mesure, et la première
attribution fixe le propriétaire. Prendre les dernières réécrirait l'histoire.

## Les refus

`ArgumentException` pour un journal vide, une entrée sans ses trois champs, un détail vide, une
minute non numérique ou négative, des minutes qui décroissent, ou un type hors vocabulaire.

## Avant d'écrire

Prédisez le brief d'un journal où l'impact est constaté avant toute alerte, puis celui d'un journal
qui n'a que des actions et des notes. Dites pourquoi le premier commence à l'impact et ce que le
second doit avouer.
