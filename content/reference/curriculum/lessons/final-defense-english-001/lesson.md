# Défendre une décision technique en anglais

## Objectif observable

À la fin de cette leçon, vous saurez présenter une décision technique en anglais professionnel avec sa
justification et ses limites, répondre à une objection sans vous dérober, et rédiger un compte rendu
d'incident bref qui nomme un impact, cite une preuve et attribue une action.

## Prérequis

- Avoir lu `final-project-evidence-001` et savoir produire une preuve.
- Avoir lu `performance-security-incident-001` et savoir ce qu'un compte rendu contient.

## Intuition

Défendre une décision ne consiste ni à la vendre ni à s'excuser de l'avoir prise. C'est en exposer le
raisonnement : la contrainte, l'arbitrage, ce qui a été écarté, et ce qu'on accepte comme conséquence.

En anglais, la difficulté n'est presque jamais le vocabulaire technique — il est international. C'est
la structure de la réponse et la capacité à dire honnêtement « je ne sais pas, voici comment je le
vérifierais ».

## Explication

**Une structure en quatre temps.** *Le contexte* : ce qu'il fallait résoudre, en une phrase. *La
décision* : ce qui a été retenu. *L'arbitrage* : ce qui a été écarté et sur quel critère. *La limite* :
ce que cette décision coûte, et à quelle condition on en changerait.

Cette structure fonctionne dans les deux langues. En anglais, elle a un avantage supplémentaire : elle
vous évite d'improviser une syntaxe, puisque l'ordre est connu d'avance.

**Des phrases courtes.** Une idée par phrase. Une subordonnée de trop transforme une réponse claire en
phrase dont vous perdez le fil au milieu. Les tournures simples sont attendues d'un ingénieur, pas
excusées.

**Le vocabulaire utile est restreint.** *Trade-off*, *constraint*, *evidence*, *rollback*, *breaking
change*, *scope*, *ownership*, *root cause*, *mitigation*, *follow-up*. Une vingtaine de termes
couvrent l'essentiel des discussions techniques. Les connaître dispense de chercher un mot pendant
qu'on parle.

**Répondre à une objection sans se dérober.** Trois formes acceptables. *Reconnaître et préciser* :
l'objection est juste dans ce cas, voici où la décision tient quand même. *Reconnaître et corriger* :
l'objection est juste, voici ce que je changerais. *Demander une précision* : reformuler l'objection
pour vérifier qu'on a compris avant de répondre.

Ce qu'il faut éviter : répondre à côté, ou défendre par principe une décision devenue indéfendable.
Changer d'avis devant un argument est un signe de solidité, pas de faiblesse.

**« Je ne sais pas » se dit, complété.** La formulation utile ajoute comment on trouverait la réponse.
« Je n'ai pas mesuré ce cas ; je le vérifierais avec un test de charge sur le point d'entrée de
consultation, et je regarderais le nombre de requêtes émises par appel. » C'est une réponse forte, et
c'est exactement ce qu'un entretien technique cherche.

**Un compte rendu bref a trois exigences.** Nommer l'impact — qui a été touché, combien de temps.
Citer une preuve — un identifiant de corrélation, une mesure, un lien. Attribuer la prochaine action —
qui fait quoi, et quand. Les trois ensemble ; c'est ce que l'exercice de cette leçon fait écrire.

Un compte rendu qui décrit sans preuve n'est pas vérifiable ; un compte rendu sans action attribuée ne
produit rien.

**L'écrit permet ce que l'oral ne permet pas.** Relire avant d'envoyer. Une note technique en anglais
gagne à être écrite en phrases courtes, avec des titres, une liste de décisions et une liste
d'actions. La qualité perçue vient de la structure, pas de la richesse du vocabulaire.

**Se préparer, c'est répéter à voix haute.** Lire une réponse dans sa tête ne prépare pas à la dire.
Trois questions probables, trois réponses de quatre-vingt-dix secondes, répétées à voix haute : c'est
ce qui sépare une défense fluide d'une hésitation.

## Exemple commenté

La structure en quatre temps, appliquée à une décision réelle :

```text
Context     We needed order data with strong invariants between an order and its lines,
            on a four-week timeline, with one developer.

Decision    A relational database with an object-relational mapper and versioned
            migrations.

Trade-off   We considered a document store. It would have been simpler to deploy, but
            the invariant between an order and its lines would have moved into
            application code, with no engine guarantee. We also considered hand-written
            queries everywhere; the maintenance cost was too high for this domain size.

Limit       Generated queries need attention on list endpoints. We mitigate this with
            explicit projections and a test that fails if a single request issues more
            than three database queries. We would revisit this if part of the domain
            became genuinely schema-less.
```

Une réponse à objection, dans les trois formes :

```text
Objection  "Wouldn't a document store scale better here?"

Acknowledge and qualify
  "It would scale reads better, yes. Our constraint was correctness of the
   order-to-lines invariant, and we have thirty users. Scale was not the binding
   constraint. If read volume became the constraint, I would revisit it."

Acknowledge and correct
  "You are right for the audit log — it is append-only and schema-less. I would move
   that part to a document store and keep orders relational."

Ask for precision
  "Do you mean read throughput, or write concurrency? The answer differs."
```

Et le compte rendu bref, avec ses trois exigences :

```text
Subject   /orders degraded — 09:12 to 09:24 UTC

Impact    /orders returned errors intermittently for twelve minutes.
          About 340 requests failed. No data loss.

Evidence  Correlation IDs b7c1e2f4a9 and 3d81c04e7f. Error rate peaked at 12 %.
          Query plan attached: full scan on a non-indexed column under load.

Next      1. Add the missing index — A. — done, 12 Aug.
          2. Realistic data volume in the integration suite — B. — 26 Aug.
          3. Per-endpoint p95 latency alert — C. — 19 Aug.

Rolled back to 1.4.7 at 09:18. Service restored at 09:24.
```

## Contre-exemple et erreur fréquente

```text
Question  "Why did you choose a relational database here?"

Answer    "Well, actually, I mean, it is, how to say, the standard, and, you know,
           everybody uses it, and it is what I know, and also my previous team was
           using it, so I think it is, generally speaking, better, and there are a lot
           of resources online, and also it is well documented and it is a very robust
           technology which has been proven over many years by a lot of companies
           around the world..."

Question  "What happens if the order table grows to fifty million rows?"

Answer    "It will be fine."
```

Et le compte rendu correspondant :

```text
Subject   Problem
Body      There was an issue this morning. It is fixed now. Sorry for that.
```

Cinq défauts.

La première réponse n'a aucune structure. Aucune contrainte n'est nommée, aucune alternative n'est
écartée sur un critère, et la longueur ne compense pas l'absence de contenu.

« C'est ce que je connais » et « tout le monde l'utilise » ne sont pas des arbitrages. Ils peuvent
être des raisons honnêtes, mais il faut alors les nommer comme telles : « c'est la technologie que je
maîtrise, ce qui était une contrainte réelle sur quatre semaines. »

« It will be fine » est une affirmation invérifiable sur une question de dimensionnement. La réponse
attendue est « je ne l'ai pas mesuré, voici comment je le vérifierais ».

Le compte rendu ne nomme aucun impact — qui, combien de temps, quelle conséquence. Il ne cite aucune
preuve. Et il n'attribue aucune action : rien ne changera.

Enfin, « Sorry for that » remplace l'information par de l'excuse. Le lecteur n'apprend rien de ce qui
s'est passé ni de ce qui empêchera la répétition.

## Vérification de compréhension

Préparez en quatre temps la défense d'une décision que vous avez réellement prise dans un exercice de
ce cours. Écrivez les quatre parties en anglais, en phrases courtes.

:::quiz
id=final-defense-english-001-check
question=Comment répondre à une question technique dont vous n'avez pas la réponse ?
option=Proposer l'hypothèse la plus probable en la présentant comme certaine, pour ne pas paraître hésitant
option=Dire que vous ne l'avez pas mesuré, puis décrire précisément comment vous le vérifieriez
option=Détourner vers un sujet voisin que vous maîtrisez, afin de montrer vos compétences
correct=1
success=Correct : « je ne sais pas, voici comment je le vérifierais » est une réponse forte — c'est exactement ce qu'un entretien technique cherche à observer.
retry=Relisez le passage sur la formulation de l'incertitude, et demandez-vous ce qui est réellement évalué dans cette question.
:::

## Exercice guidé

Ouvrez `azure-incident-brief-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce qui manque à un compte rendu qui ne remplit que deux exigences sur
   trois, pour chacune des trois combinaisons.
2. Implémentez la règle comme une conjonction stricte.
3. Vérifiez les trois cas incomplets, puis le cas complet.
4. Rédigez ensuite en anglais un compte rendu réel — impact, preuve, action attribuée — sur un
   incident rencontré dans un scénario de `/debug`.

## Exercice autonome

Préparez une soutenance de dix minutes en anglais sur votre projet final.

Produisez : la présentation du parcours critique en deux minutes, trois décisions défendues au format
en quatre temps, trois objections probables avec leur réponse dans l'une des trois formes acceptables,
un compte rendu d'incident bref, et la liste des vingt termes que vous voulez pouvoir employer sans
hésiter. Répétez à voix haute et chronométrez.

## Débogage

Un ticket indique : « L'entretien technique s'est mal passé alors que les réponses étaient justes sur
le fond. »

1. **Symptôme** : contenu correct, impression défavorable.
2. **Hypothèse** : réponses non structurées, longueur sans arbitrage nommé, ou incertitude masquée
   plutôt qu'exprimée.
3. **Preuve** : réécoutez ou rejouez trois réponses et cherchez la contrainte nommée, l'alternative
   écartée, la limite reconnue.
4. **Prévention** : répéter trois réponses en quatre temps à voix haute, chronométrées à quatre-vingt-dix
   secondes.

## Entretien

Question posée à voix haute : *tell me about a technical decision you made and why.*

Une réponse solide suit les quatre temps, nomme une contrainte réelle plutôt qu'une préférence, écarte
au moins une alternative sur un critère explicite, et reconnaît une limite avec la condition qui
justifierait d'en changer.

## Résumé

- Contexte, décision, arbitrage, limite : quatre temps, dans les deux langues.
- Une idée par phrase ; la structure vaut mieux que le vocabulaire.
- Reconnaître et préciser, reconnaître et corriger, ou demander une précision.
- « Je ne sais pas » se complète par la façon de vérifier.
- Un compte rendu nomme un impact, cite une preuve, attribue une action.

## Cartes de révision

Question : que fait une réponse longue sans arbitrage nommé ? Réponse attendue : elle occupe du temps
sans transmettre de raisonnement.

Question : pourquoi changer d'avis devant un bon argument est-il favorable ? Réponse attendue : cela
montre que la décision reposait sur des critères, pas sur une préférence.

## Test de maîtrise

Sans relire, préparez en anglais la défense complète de trois décisions de votre projet : les quatre
temps de chacune, une objection probable et sa réponse, une question dont vous n'avez pas la réponse et
la façon de le dire, et un compte rendu d'incident bref respectant les trois exigences. Répétez à voix
haute.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
