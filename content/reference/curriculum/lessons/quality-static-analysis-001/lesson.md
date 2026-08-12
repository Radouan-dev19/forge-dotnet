# Analyse statique et avertissements tenus

## Objectif observable

À la fin de cette leçon, vous saurez faire échouer une construction sur un avertissement plutôt que de
l'accumuler, justifier une suppression ponctuelle de règle par écrit, et mesurer une complexité qui
signale un besoin de découpage.

## Prérequis

- Avoir lu `quality-regression-refactoring-001` et savoir modifier du code sous filet.
- Avoir lu `csharp-exceptions-nullable-001` et savoir ce qu'annonce une annotation de nullité.

## Intuition

Un avertissement est une question posée par un outil qui a lu tout votre code. Ignoré, il devient du
bruit ; quatre cents avertissements équivalent à zéro, parce que plus personne ne les lit.

La seule position tenable est binaire : un avertissement est **traité** ou **justifié par écrit**. Il
n'y a pas de troisième état, et surtout pas « on verra plus tard ».

## Explication

**Le compilateur est le premier analyseur.** Il détecte la variable non utilisée, la conversion qui
perd de l'information, le champ jamais affecté, la comparaison toujours vraie. Ces messages sont
gratuits et précis. Les traiter comme des erreurs — en activant l'option qui transforme tout
avertissement en échec — est la décision structurante de cette leçon.

Elle ne coûte que si l'on part d'une base déjà encombrée. C'est le même raisonnement que le registre
de dette de ce dépôt : on fige le niveau existant, et on interdit qu'il remonte.

**Les analyseurs vont plus loin que le compilateur.** Types jetables non libérés, comparaison de
chaînes sans culture explicite, appel asynchrone dont le résultat est perdu, concaténation dans une
requête. Chaque famille correspond à un défaut réel déjà rencontré : `async-fundamentals-001` pour le
second, `api-pagination-filtering-sorting-001` pour le dernier.

**Le contexte de nullité change la nature du code.** Activé, il fait dire au type ce que la
documentation disait mal : cette valeur peut être absente, celle-ci non. Le compilateur signale alors
les déréférences non protégées. L'opérateur qui supprime l'avertissement — celui qui affirme « je sais
que ce n'est pas nul » — est un mensonge quand on ne le sait pas : il transforme un avertissement en
exception à l'exécution.

La bonne réponse est la garde explicite, comme dans `quality-null-guard-001` : traiter l'absence, la
chaîne vide et la chaîne de blancs avant toute déréférence.

**Une suppression se justifie sur place.** Il existe des cas où la règle a tort : un faux positif, une
contrainte externe, un compromis assumé. La suppression est alors locale — jamais globale — et porte
une justification écrite disant *pourquoi*, pas *quoi*. « Suppression de la règle X » n'apprend rien ;
« l'entrée est validée en amont par le point d'entrée, voir tel test » se relit dans deux ans.

Une suppression globale dans un fichier de configuration est le pire des deux mondes : elle éteint la
règle partout, y compris là où elle avait raison, et personne ne s'en souvient.

**Le format se vérifie aussi.** Un style homogène supprime une catégorie entière de discussions de
revue et rend les diffs lisibles — les changements de mise en forme ne masquent plus les changements
de logique. La vérification automatique du format dans la construction est la seule façon de tenir
cette règle sans négociation.

**Les métriques indiquent où regarder, elles ne jugent pas.** Une méthode à quinze branches ou à cinq
niveaux d'imbrication est difficile à tester, donc probablement mal découpée. Un budget explicite —
par exemple trois niveaux d'imbrication au plus — donne un critère objectif en revue, à condition de le
traiter comme un signal et non comme une note.

**Les dépendances vulnérables se détectent automatiquement.** C'est un contrôle d'analyse comme un
autre : la chaîne de construction interroge la liste des vulnérabilités connues et échoue sur une
correspondance. Le sujet est repris dans `ci-pipeline-build-test-001`.

## Exemple commenté

Le budget d'imbrication, exprimé comme une règle vérifiable :

```csharp
public static bool WithinNestingBudget(int nestingDepth)
{
    // Une mesure négative n'a pas de sens : c'est une faute d'appelant, pas un cas limite.
    if (nestingDepth < 0)
    {
        return false;
    }

    // Zéro à trois niveaux inclus. Au-delà, la méthode devient difficile à tester
    // exhaustivement : le nombre de chemins croît plus vite que la lecture ne suit.
    return nestingDepth <= 3;
}
```

La garde explicite, plutôt que l'affirmation de non-nullité :

```csharp
public static string NormalizeOptional(string? value)
{
    // Absence, chaîne vide et chaîne de blancs sont traitées ensemble, avant
    // toute déréférence. Aucun opérateur n'affirme ici ce qui n'est pas garanti.
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    return value.Trim();
}
```

Et la configuration qui rend la règle non négociable, avec une suppression justifiée :

```xml
<PropertyGroup>
  <!-- Tout avertissement devient une erreur : le niveau ne peut plus remonter. -->
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <Nullable>enable</Nullable>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <AnalysisLevel>latest-recommended</AnalysisLevel>
</PropertyGroup>
```

```csharp
// Suppression locale, et la justification dit pourquoi la règle a tort ici,
// pas ce qu'elle interdit. Elle se relit dans deux ans par quelqu'un d'autre.
[SuppressMessage(
    "Reliability",
    "CA2000:Supprimer les objets avant la mise hors de portée",
    Justification = "Le client HTTP est détenu par le conteneur : le libérer ici "
        + "invaliderait les autres consommateurs de la même instance.")]
public HttpResponseMessage Send(HttpRequestMessage request) => _client.Send(request);
```

## Contre-exemple et erreur fréquente

```xml
<PropertyGroup>
  <!-- Les avertissements gênaient : ils sont désactivés en bloc.
       Personne ne sait plus lesquels sont ignorés ni depuis quand. -->
  <NoWarn>$(NoWarn);CS8600;CS8602;CS8604;CA2000;CA1305;CA2100</NoWarn>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
</PropertyGroup>
```

```csharp
public string CustomerCity(Order? order)
{
    // L'opérateur affirme que rien n'est nul. Il ne le prouve pas :
    // il éteint l'avertissement et déplace le défaut à l'exécution.
    return order!.Customer!.Address!.City!;
}

#pragma warning disable
// Désactivation sans code de règle et sans réactivation : tout ce qui suit
// dans le fichier est muet, y compris le code écrit dans six mois.
public void Process(string query) => _db.Execute("SELECT * FROM T WHERE X='" + query + "'");
```

Quatre défauts qui se renforcent.

La liste de règles désactivées globalement éteint des contrôles qui ont raison partout ailleurs.
`CA2100` est justement celui qui aurait signalé la concaténation dans la requête, en bas de l'exemple.

Les opérateurs de non-nullité empilés remplacent quatre avertissements par une exception de
déréférence à l'exécution — avec un message qui ne dira pas lequel des quatre maillons était absent.

`#pragma warning disable` sans code de règle et sans réactivation transforme le reste du fichier en
zone sans contrôle. C'est cumulatif : chaque ligne ajoutée ensuite en hérite.

Et la concaténation dans la requête est exactement le défaut que tout ce dispositif aurait dû
attraper.

La correction : réactiver les règles, traiter les avertissements un par un sous filet de tests,
figer le reste dans un plafond qui ne peut que descendre, et remplacer chaque suppression globale par
une suppression locale justifiée.

## Vérification de compréhension

Vous héritez d'un projet affichant quatre cents avertissements. Décrivez la démarche qui permet
d'arriver à zéro sans bloquer l'équipe pendant des semaines.

:::quiz
id=quality-static-analysis-001-check
question=Pourquoi une suppression locale et justifiée vaut-elle mieux qu'une désactivation globale de la règle ?
option=Parce que la suppression locale s'exécute plus rapidement à la compilation
option=Parce que la désactivation globale éteint la règle partout, y compris là où elle a raison, et que plus personne ne se souvient pourquoi
option=Parce que les règles désactivées globalement réapparaissent à la version suivante de l'analyseur
correct=1
success=Correct : la suppression locale garde la règle active ailleurs, et sa justification écrite explique pourquoi elle a tort à cet endroit précis.
retry=Relisez le passage sur les suppressions, et demandez-vous ce qu'une désactivation globale fait au reste du code.
:::

## Exercice guidé

Ouvrez `quality-complexity-budget-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que doit produire une mesure négative et une mesure au-delà du budget.
2. Implémentez la règle en distinguant la mesure incohérente de la mesure simplement excessive.
3. Vérifiez la frontière exacte du budget, dans les deux sens.
4. Enchaînez avec `quality-null-guard-001`, qui remplace l'affirmation de non-nullité par une garde.

## Exercice autonome

Activez le traitement des avertissements en erreurs sur un projet existant.

Relevez le nombre d'avertissements, classez-les par famille, décidez lesquels sont traités
immédiatement, lesquels sont justifiés localement et lesquels sont figés dans un plafond. Écrivez la
justification d'au moins une suppression, en expliquant pourquoi la règle a tort à cet endroit.

## Débogage

Un ticket indique : « Une exception de déréférence en production, sur du code qui compile sans le
moindre avertissement. »

1. **Symptôme** : le compilateur était silencieux, l'exécution ne l'est pas.
2. **Hypothèse** : un opérateur de non-nullité affirme une garantie qui n'existe pas, ou une règle est
   désactivée sur le fichier.
3. **Preuve** : cherchez les affirmations de non-nullité et les désactivations sans code de règle sur
   le chemin fautif.
4. **Prévention** : remplacer l'affirmation par une garde explicite, et interdire en revue les
   désactivations non ciblées.

## Entretien

Question posée à voix haute : *que faites-vous des avertissements de compilation dans vos projets ?*

Une réponse solide pose la règle binaire — traité ou justifié —, explique comment atteindre zéro sur
une base encombrée sans tout arrêter, distingue suppression locale et désactivation globale, et sait
dire ce que le contexte de nullité change réellement.

## Résumé

- Un avertissement est traité ou justifié par écrit ; il n'y a pas de troisième état.
- Quatre cents avertissements valent zéro : plus personne ne les lit.
- L'affirmation de non-nullité déplace un avertissement vers une exception.
- Une suppression est locale et dit pourquoi la règle a tort ici.
- Les métriques désignent où regarder ; elles ne remplacent pas le jugement.

## Cartes de révision

Question : que fait une désactivation sans code de règle ni réactivation ? Réponse attendue : elle rend
muet tout le reste du fichier, y compris le code écrit plus tard.

Question : pourquoi vérifier le format dans la construction ? Réponse attendue : les changements de
mise en forme cessent de masquer les changements de logique dans les diffs.

## Test de maîtrise

Sans relire, décrivez la politique d'analyse statique complète d'un projet : ce qui est activé, ce qui
devient une erreur, la façon de traiter une base héritée sans bloquer l'équipe, la forme d'une
suppression acceptable, les métriques suivies et ce que vous en faites, et le contrôle de dépendances
vulnérables.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
