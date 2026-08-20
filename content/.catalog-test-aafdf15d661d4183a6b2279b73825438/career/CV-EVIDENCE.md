# Le CV par preuves

Un CV de reconversion a un problème structurel : la rubrique « expérience » ne raconte pas encore le
métier visé. La réponse classique — gonfler des intitulés, maquiller une formation en poste — se
détecte en entretien et coûte la candidature. Ce guide prend le chemin inverse : chaque ligne du CV
s'appuie sur une preuve que vous pouvez montrer, rejouer ou expliquer, et annonce elle-même sa limite.

## Ce que le produit collecte réellement

Forge.NET ne mesure pas des impressions : il enregistre des faits datés, reproductibles sur votre
machine.

- **Les portes.** La porte A « Junior fiable » exige des seuils par domaine (C#, débogage, SQL), dix
  exercices vérifiés sans aide, un mini-projet console dont les suites d'acceptation passent, et un
  examen de quatre-vingt-dix minutes sans assistance. Les portes B, C et D empilent des livrables
  vérifiés : API, EF Core, tests, Docker, intégration continue, incident simulé.
- **Les examens.** Chaque tirage est scellé par un engagement cryptographique publié pendant
  l'épreuve ; le rapport final révèle la graine et rend le tirage vérifiable. Un score d'examen
  Forge.NET n'est pas une auto-déclaration.
- **Les projets vérifiables.** Un projet n'est « livré » que quand toutes ses suites d'acceptation —
  cas visibles et cachés — passent sur la même soumission, dans un bac à sable isolé.
- **Les DebugLabs.** Trente scénarios de diagnostic sur du code cassé, avec un journal exportable en
  Markdown : symptôme, hypothèse, preuve, cause, correctif, prévention.
- **Le SqlLab.** Quarante scénarios exécutés contre une vraie base SQL Server jetable.

## Écrire une ligne de CV à partir d'une preuve

Chaque ligne suit six champs, dans cet ordre : contexte exact, action personnelle, technologie
utile, résultat observé, artefact reproductible, limite. Trois exemples rédigés, tous étiquetés
comme ce qu'ils sont — un programme personnel structuré, jamais un emploi :

1. « Programme personnel Forge.NET — conçu un analyseur de journaux en console .NET ; parsing
   robuste aux lignes malformées ; livraison validée par des suites de tests cachées exécutées en
   conteneur isolé. Limite : projet individuel, sans utilisateurs en production. »
2. « Diagnostiqué trente scénarios de code cassé (NullReference, bornes, concurrence) avec journal
   méthodique symptôme-hypothèse-preuve ; chaque correctif accompagné d'un test de non-régression.
   Limite : défauts plantés à des fins pédagogiques, pas d'astreinte réelle. »
3. « Réussi un examen C#/.NET de quatre-vingt-dix minutes sans aide, à tirage aléatoire vérifiable
   par graine cryptographique. Limite : évalue le code produit seul, pas le travail en équipe. »

## Ce qu'une preuve Forge.NET démontre — et ne démontre pas

| La preuve démontre | Elle ne démontre pas |
|---|---|
| Du code qui passe des tests cachés, sans aide, à une date donnée | Une expérience professionnelle ou un historique d'emploi |
| La capacité à diagnostiquer un défaut avec méthode | La tenue d'une astreinte sous pression réelle |
| Une discipline de travail sur plusieurs mois, datée | La collaboration quotidienne avec une équipe |
| Un niveau mesuré par domaine, à seuils publiés | Un niveau « senior », que seul le terrain construit |

Présentez toujours la colonne de gauche, jamais la droite. Un recruteur qui découvre l'écart en
entretien ne retient que l'écart.

## Structure recommandée pour une reconversion

1. **Accroche factuelle** (deux lignes) : le métier visé, le programme suivi, la preuve la plus
   forte — sans adjectif d'auto-évaluation.
2. **Bloc projets** : trois à cinq lignes au format ci-dessus, les plus proches du poste en premier.
3. **Compétences prouvées** : uniquement celles couvertes par une porte ou un examen ; les autres
   vont dans « en cours d'apprentissage », rubrique honnête que les recruteurs lisent bien.
4. **Parcours antérieur** : deux ou trois lignes qui extraient les atouts transférables — rigueur,
   relation client, gestion de contraintes — sans forcer le lien technique.

## Extraire une preuve proprement

Le script `Export-CareerEvidence.ps1` de ce dossier génère un Markdown local à partir d'un fichier
de données que vous remplissez. Il refuse tout champ ressemblant à une adresse de courriel ou à un
numéro de téléphone : les coordonnées vont dans l'en-tête du CV, jamais dans une preuve destinée à
circuler. Gardez CV et exports dans une copie locale exclue de Git, et relisez métadonnées et
historique avant tout envoi.

Ce guide ne promet ni entretien ni embauche : il rend vos faits présentables, le reste appartient au
marché et à votre préparation.
