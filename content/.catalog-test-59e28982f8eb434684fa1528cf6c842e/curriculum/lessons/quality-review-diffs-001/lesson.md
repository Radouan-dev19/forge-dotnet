# Revue de code centrée sur le diff

## Objectif observable

À la fin de cette leçon, vous saurez classer une remarque de revue par gravité, formuler un
commentaire qui obtient une correction plutôt qu'une justification, et reconnaître les diffs qui
demandent une attention disproportionnée à leur taille.

## Prérequis

- Avoir lu `quality-static-analysis-001` et savoir ce qu'un outil traite déjà.
- Avoir lu `security-owasp-api-001` et savoir reconnaître un risque de sécurité.

## Intuition

Une revue n'est pas un examen de la personne, ni une relecture de tout le projet. C'est une question
simple posée sur un ensemble de modifications : *ce changement peut-il causer un problème, et si oui
lequel ?*

Ce qu'un outil vérifie mieux que vous — format, avertissements, couverture — ne doit pas occuper votre
attention. Ce qu'aucun outil ne voit — une règle métier fausse, une autorisation manquante, un nom qui
ment — est exactement ce que la revue apporte.

## Explication

**Trois niveaux de gravité, dans cet ordre.** *Bloquant* : le changement introduit un risque de
sécurité, une perte de données ou un défaut de correction. *Important* : le code fonctionne mais pose
un problème de maintenabilité, de lisibilité ou de performance qui coûtera. *Suggestion* : une
préférence, explicitement marquée comme non bloquante.

L'ordre compte : un risque de sécurité prime sur un défaut de correction, qui prime sur une
suggestion. Sans cette hiérarchie annoncée, l'auteur ne sait pas ce qu'il doit corriger avant de
fusionner, et les vingt remarques de style noient la seule qui comptait.

**Le risque ne se déduit pas du volume.** Un diff de trois cents lignes ajoutant des tests est à faible
risque. Un diff de trois lignes modifiant une condition d'autorisation est à risque élevé. La règle
pratique : tout changement touchant l'authentification, l'autorisation, les données personnelles, la
migration de schéma ou la gestion d'argent mérite une attention maximale quelle que soit sa taille.

**Ce que la revue doit chercher.** Une règle métier fausse ou déplacée. Une entrée non validée. Une
autorisation absente sur la ressource. Un cas d'erreur non traité. Un nom qui décrit autre chose que
ce que fait le code. Un test absent sur le comportement ajouté. Une modification de contrat non
signalée, au sens de `api-openapi-contracts-001`.

**Ce qu'elle ne doit pas chercher.** Le format, traité par l'outil. Les avertissements, traités par la
construction. Les préférences personnelles présentées comme des règles. Une réécriture complète de
l'approche : si l'approche pose problème, la discussion aurait dû avoir lieu avant l'implémentation,
et c'est un signal d'organisation, pas de revue.

**Un commentaire efficace a trois parties.** Ce qui pose problème, pourquoi, et une proposition. « Ce
n'est pas correct » ne produit qu'une justification défensive. « Si la liste est vide, cette ligne
lève une exception ; un appel avec un panier vide est possible depuis le point d'entrée de création —
`FirstOrDefault` avec traitement de l'absence conviendrait » produit une correction.

Poser une question sincère fonctionne aussi bien : « que se passe-t-il si deux requêtes arrivent en
même temps ici ? » invite à raisonner plutôt qu'à se défendre.

**Un diff illisible est un problème de l'auteur.** Mille lignes mêlant renommage, restructuration et
nouvelle fonctionnalité ne sont pas relisibles : le relecteur approuvera sans avoir lu, ce qui est
pire que ne pas relire. La bonne réponse est de demander le découpage, et c'est la raison pratique de
la séparation exigée par `quality-regression-refactoring-001`.

**La revue est bidirectionnelle.** L'auteur prépare : description de l'intention, découpage,
auto-relecture du diff avant de le soumettre. Le relecteur répond dans un délai court — une revue qui
attend trois jours coûte plus cher que le défaut qu'elle évite, parce que l'auteur a déjà changé de
sujet.

## Exemple commenté

La gravité, ramenée à sa règle de priorité :

```csharp
public static string ReviewSeverity(bool securityRisk, bool correctnessDefect)
{
    // La sécurité prime : elle bloque la fusion quelle que soit la taille du diff.
    if (securityRisk)
    {
        return "blocker";
    }

    // Puis la correction. Le reste est explicitement non bloquant, et le dire
    // évite que l'auteur traite vingt préférences avant le seul vrai défaut.
    return correctnessDefect ? "major" : "suggestion";
}
```

Le risque, qui dépend de la nature du changement avant son volume :

```csharp
public static string DiffRisk(int changedLines, bool touchesAuthorization)
{
    // Trois lignes touchant l'autorisation valent plus d'attention que
    // trois cents lignes de tests : la nature prime sur le volume.
    if (touchesAuthorization)
    {
        return "high";
    }

    return changedLines > 200 ? "medium" : "low";
}
```

Et un commentaire de revue qui obtient une correction :

```text
Bloquant — /orders/{id} : GET charge la commande sans vérifier son propriétaire.

Un appelant authentifié peut lire les commandes d'autrui en changeant l'identifiant.
Proposition : après le chargement, comparer OwnerId à l'identité courante et
retourner NotFound si elles diffèrent, comme dans DeleteAsync juste au-dessus.
Un test appelant la commande d'un autre utilisateur figerait la règle.
```

## Contre-exemple et erreur fréquente

```text
Relecture de la demande #418 (1 240 lignes modifiées) :

- ligne 12 : préfère une boucle for ici
- ligne 47 : espace manquant avant l'accolade
- ligne 88 : ce nom ne me plaît pas
- ligne 103 : pourquoi tu n'as pas utilisé LINQ ?
- ligne 156 : c'est faux
- ligne 202 : à revoir
- ligne 260 : j'aurais fait autrement, il faudrait tout reprendre avec un médiateur

Approuvé sous réserve.
```

Six défauts dans une seule revue.

Les remarques de format et de préférence occupent quatre des sept lignes. Elles auraient dû être
traitées par la vérification automatique de `quality-static-analysis-001`, et leur volume noie ce qui
compte.

« C'est faux » et « à revoir » n'apprennent rien. L'auteur ne sait ni ce qui est faux, ni dans quelles
conditions, ni ce qui est attendu — la réponse sera une justification, pas une correction.

« Pourquoi tu n'as pas… » vise la personne plutôt que le code. La formulation neutre — « quel est
l'effet de X ici ? » — obtient le même résultat sans mettre l'auteur en défense.

La proposition de tout reprendre avec un autre motif de conception arrive après l'implémentation :
c'est une discussion d'approche, qui devait avoir lieu avant.

Aucune gravité n'est annoncée : l'auteur ne peut pas savoir ce qui bloque la fusion.

Enfin, « approuvé sous réserve » sur mille deux cent quarante lignes signifie que la revue n'a pas eu
lieu. Le bon geste était de demander un découpage.

## Vérification de compréhension

Classez ces trois remarques par gravité et justifiez : un nom de variable peu clair, une requête
concaténant un paramètre reçu, une méthode qui retourne une valeur par défaut au lieu de signaler une
absence.

:::quiz
id=quality-review-diffs-001-check
question=Pourquoi annoncer la gravité de chaque remarque de revue ?
option=Parce que les outils de revue exigent une étiquette sur chaque commentaire
option=Parce que sans hiérarchie l'auteur ne sait pas ce qui bloque la fusion, et les remarques de préférence noient le seul défaut réel
option=Parce que les remarques bloquantes doivent être traitées après les suggestions
correct=1
success=Correct : sécurité, puis correction, puis suggestion explicitement non bloquante. C'est ce classement qui rend la revue actionnable.
retry=Relisez le passage sur les trois niveaux, et demandez-vous ce que fait un auteur devant vingt remarques non hiérarchisées.
:::

## Exercice guidé

Ouvrez `quality-review-severity-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, l'ordre de priorité entre les critères et sa justification.
2. Implémentez la décision en respectant cet ordre.
3. Vérifiez le cas où les deux indicateurs sont vrais simultanément.
4. Enchaînez avec `quality-diff-risk-001`, qui fait primer la nature du changement sur son volume.

## Exercice autonome

Relisez un diff réel — un de vos commits, ou le laboratoire `content/labs/git-review/`.

Produisez une revue complète : chaque remarque avec sa gravité, le problème, sa conséquence concrète
et une proposition. Terminez par la décision — fusionnable en l'état, fusionnable après corrections,
ou à découper — et justifiez-la.

## Débogage

Un ticket indique : « Un défaut de sécurité est passé en production alors que la demande avait été
relue par deux personnes. »

1. **Symptôme** : la revue n'a pas rempli sa fonction malgré deux relecteurs.
2. **Hypothèse** : le diff était trop volumineux ou mêlait plusieurs intentions, et la relecture est
   devenue une approbation.
3. **Preuve** : mesurez la taille du diff et le nombre d'intentions qu'il portait, puis relevez le
   délai entre soumission et approbation.
4. **Prévention** : exiger le découpage au-delà d'un seuil, et une attention systématique aux
   changements touchant l'autorisation quelle que soit leur taille.

## Entretien

Question posée à voix haute : *que regardez-vous en priorité dans une revue de code ?*

Une réponse solide écarte d'abord ce que les outils traitent, place la sécurité et la correction en
tête, sait dire que le risque ne se déduit pas du volume, et décrit la forme d'un commentaire qui
obtient une correction plutôt qu'une justification.

## Résumé

- Trois gravités annoncées : bloquant, important, suggestion non bloquante.
- La nature du changement prime sur le nombre de lignes modifiées.
- Ce qu'un outil vérifie ne doit pas occuper la revue.
- Un commentaire dit le problème, sa conséquence et une proposition.
- Un diff illisible se découpe ; l'approuver est pire que ne pas le lire.

## Cartes de révision

Question : quels changements méritent une attention maximale quelle que soit leur taille ? Réponse
attendue : authentification, autorisation, données personnelles, migration de schéma, gestion
d'argent.

Question : pourquoi une revue tardive coûte-t-elle cher ? Réponse attendue : l'auteur a changé de
sujet, et reprendre le contexte coûte plus que le défaut évité.

## Test de maîtrise

Sans relire, rédigez la grille de revue complète d'une équipe : ce qui est délégué aux outils, ce que
le relecteur cherche, les niveaux de gravité et leur ordre, la forme d'un commentaire actionnable, les
critères de découpage d'un diff, le délai de réponse attendu, et la règle qui s'applique aux
changements d'autorisation.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
