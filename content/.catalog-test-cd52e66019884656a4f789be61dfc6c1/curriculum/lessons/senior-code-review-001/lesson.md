# Revue de code : gravité, faux positifs et ton

## Objectif observable

À la fin de cette leçon, vous saurez classer chaque constat d'une revue par gravité et par catégorie, reconnaître qu'un constat de style ne bloque jamais une fusion, et formuler un blocage sur un fait vérifiable plutôt que sur une opinion. Vous saurez aussi dire ce qu'aucun contenu ne peut vous apprendre à ce sujet.

## Prérequis

- Avoir suivi `senior-observability-001` : décider sur des faits chiffrés plutôt que sur une intuition.
- Savoir lire un diff et distinguer une modification de comportement d'une modification de forme.

## Intuition

Une revue utile tient dans un signal net : ce qui doit être corrigé avant de fusionner, et ce qui reste un avis. Le relecteur débutant confond les deux et bloque sur tout ; le relecteur expérimenté trie. Ce tri n'est pas une politesse, c'est ce qui rend la revue exploitable : un flot de remarques toutes présentées comme urgentes ne se distingue plus d'un flot de bruit.

## Explication

**Deux axes, pas un.** Un constat se classe sur sa gravité — bloquant ou mineur — et sur sa catégorie — correction, sécurité, concurrence, style. Le couple porte les deux informations sans les fondre. Savoir qu'un défaut existe ne suffit pas : le relecteur doit dire s'il empêche la fusion, et pourquoi.

**Trois familles bloquent, une jamais.** Une faute de correction casse le résultat. Une faille de sécurité expose l'utilisateur. Un accès concurrent non synchronisé produit un bug non déterministe qui échappera aux tests. Ces trois familles justifient de bloquer une fusion. Le style — un nommage, un espacement, un commentaire manquant — ne bloque jamais : c'est un avis que l'auteur suit ou non.

**Le faux positif de style coûte.** Présenter une préférence de style comme bloquante est l'erreur la plus fréquente et la plus chère. Elle retarde des fusions saines, elle noie les vrais bloquants sous le bruit, et surtout elle érode la confiance : à force de blocages sur des broutilles, les remarques sérieuses du relecteur finissent ignorées. Un relecteur qui veut peser garde son veto pour ce qui le mérite.

**Le ton fait partie de la compétence.** Un blocage se formule sur un fait — « cette requête concatène une entrée utilisateur, voici l'injection possible » — pas sur un jugement — « ce code est mauvais ». Le fait se discute et se corrige ; le jugement braque. Un bon constat propose souvent la correction, ou au moins la direction.

**Admettre le doute.** Un relecteur honnête qui ne sait pas classer un constat le dit, au lieu de trancher au hasard. Un « je ne suis pas sûr de l'impact ici, peux-tu m'expliquer » vaut mieux qu'un blocage arbitraire ou qu'un silence complice.

## Exemple commenté

Le noyau décidable de cette leçon classe un constat par gravité et catégorie :

```csharp
// Correction, sécurité et concurrence bloquent ; le style reste mineur, jamais bloquant.
public static string Triage(string findingId) => findingId switch
{
    "sql-injection" => "blocking:security",
    "unsynchronized-shared-state" => "blocking:concurrency",
    "off-by-one" => "blocking:correctness",
    "variable-naming" => "minor:style",
    _ => "unknown",
};
```

Le classement `minor:style` pour un constat de nommage est le cœur de la leçon : une solution qui le bloquerait produirait le faux positif que l'on cherche à éviter.

## Contre-exemple et erreur fréquente

Le relecteur qui bloque sur la forme :

```text
BLOQUANT : renomme `x` en `count`, ce nom est trop court.
```

Le symptôme est une fusion saine retardée pour une préférence, et un auteur qui apprend à ignorer les blocages de ce relecteur. La correction distingue la suggestion du veto :

```text
Suggestion (non bloquant) : `count` serait plus parlant que `x`.
BLOQUANT : ligne 42, cette requête concatène l'entrée utilisateur — injection SQL possible.
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : pourquoi un blocage sur du style coûte-t-il plus cher que le temps qu'il fait perdre ?

:::quiz
id=senior-code-review-001-check
question=Comment doit être classé un constat de style qu'un relecteur a marqué comme bloquant ?
option=blocking:style, puisque le relecteur l'a jugé important
option=minor:style, car le style ne bloque jamais une fusion et le marquer bloquant est un faux positif qui use la confiance
option=unknown, car le style n'a pas de catégorie
correct=1
success=Exact : le style est toujours mineur ; le présenter comme bloquant est un faux positif qui retarde des fusions saines et érode la confiance.
retry=Repensez aux trois familles qui justifient un blocage, et à celle qui n'en fait jamais partie.
:::

## Exercice guidé

Ouvrez l'exercice `senior-review-triage-001` dans `/practice`, puis procédez ainsi.

1. Séparez la gravité de la catégorie pour chaque constat.
2. Rangez correction, sécurité et concurrence sous `blocking`, et le style sous `minor`.
3. Rendez `unknown` pour un identifiant que vous ne reconnaissez pas, plutôt que de trancher.
4. Vérifiez qu'un constat de style ne produit jamais `blocking`.

## Exercice autonome

Prenez un diff réel que vous avez écrit récemment. Listez cinq constats possibles, classez chacun par gravité et catégorie, et formulez le plus grave sous forme de fait vérifiable adressé à son auteur, sans jugement de valeur.

## Débogage

Un ticket indique : « Les relectures de notre équipe traînent : les autrices disent que tout est marqué urgent et qu'elles ne savent plus quoi corriger d'abord. »

1. **Symptôme** : file de relecture engorgée, priorités illisibles pour les autrices.
2. **Hypothèse** : des constats de style sont présentés comme bloquants, noyant les vrais bloquants.
3. **Preuve** : classer un échantillon de constats récents par gravité réelle et compter la part de style marquée bloquante.
4. **Prévention** : convenir en équipe que le style ne bloque pas, et réserver le veto à la correction, la sécurité et la concurrence.

## Entretien

Question posée à voix haute : *comment décidez-vous ce qui bloque une fusion, et comment le dites-vous à l'auteur ?*

Une réponse solide classe par gravité et catégorie, réserve le blocage aux trois familles qui le méritent, et formule le constat sur un fait. Elle reconnaît aussi, sans l'enjoliver, une limite : recevoir la revue d'un humain qui n'est pas d'accord, arbitrer un désaccord d'équipe sous pression, tenir une position contestée — cela ne s'apprend pas dans un exercice. Ce module entraîne le classement et le ton, pas la relation humaine qui les entoure ; celle-ci se vit face à de vraies personnes.

### Le nom en entretien

La revue se mène en anglais dans la plupart des outils : la demande de fusion se dit **pull
request** — ou merge request selon la plateforme —, l'opposition **changes requested**,
l'approbation périmée **stale approval**, et la carte des propriétaires **code owners** — le fichier
CODEOWNERS que les plateformes lisent pour convoquer les relecteurs. Les règles qui rendent tout
cela exécutoire se disent **branch protection rules**. Aucun outil précis n'est une dépendance de
cette semaine : ces noms sont ceux des conversations d'équipe et des entretiens, et savoir dire en
une phrase ce que chacun gouverne est la compétence visée.

## Résumé

- Un constat se classe sur deux axes : gravité et catégorie.
- Correction, sécurité et concurrence bloquent ; le style ne bloque jamais.
- Un faux positif de style retarde des fusions saines et érode la confiance dans la revue.
- Un blocage se formule sur un fait vérifiable, pas sur un jugement.
- Aucune revue simulée ne remplace un désaccord humain réel, à arbitrer sous pression.

## Cartes de révision

Question : quelles familles de constats justifient de bloquer une fusion ? Réponse attendue : la correction, la sécurité et la concurrence ; le style reste toujours un avis mineur.

Question : que remplace, et que ne remplace pas, un module de revue à défauts plantés ? Réponse attendue : il entraîne le classement et le ton d'une revue, mais ne remplace pas le désaccord d'un humain réel ni l'arbitrage d'une équipe sous pression.

## Test de maîtrise

Sans relire, classez cinq constats d'un diff par gravité et catégorie, justifiez chaque blocage par un fait, et nommez ce que cette compétence ne couvre pas de la revue en équipe.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
