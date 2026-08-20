# Chaîne d'intégration reproductible

## Objectif observable

À la fin de cette leçon, vous saurez décrire une chaîne qui restaure, construit et teste de façon
reproductible, faire échouer le travail sur le premier signal rouge, et expliquer pourquoi une chaîne
plus lente que quelques minutes cesse d'être utilisée.

## Prérequis

- Avoir lu `docker-compose-networks-volumes-001` et savoir décrire un environnement reproductible.
- Avoir lu `quality-static-analysis-001` et savoir ce qu'un contrôle automatique refuse.

## Intuition

L'intégration continue répond à une question précise : *si je fusionne ce travail maintenant, est-ce
que quelque chose casse ?* La réponse doit arriver vite, et surtout être **la même** pour tout le
monde.

« Ça marche chez moi » est exactement ce que la chaîne supprime. Elle part d'une machine propre, avec
des versions fixées, et refait tout depuis la source.

## Explication

**Trois étapes, dans cet ordre.** Restaurer les dépendances, construire, tester. Chacune échoue vite
et bruyamment. Une construction lancée sans restauration réussie ne dit rien ; des tests lancés sur une
construction échouée non plus.

**La reproductibilité vient des versions fixées.** La version de l'environnement d'exécution, celle
des dépendances, celle des outils. Un fichier de version d'outillage versionné dans le dépôt garantit
que la machine de construction utilise la même que vous. Un verrou de dépendances garantit que
« restaurer » signifie la même chose demain qu'aujourd'hui.

Sans ces deux éléments, la chaîne échoue un jour sans qu'aucun code n'ait changé, et ce type d'échec
détruit la confiance plus sûrement qu'un défaut réel.

**La restauration doit être vérifiable, pas seulement réussie.** Restaurer en mode strict — refuser
toute résolution qui ne correspond pas au verrou — transforme une dérive silencieuse en échec. C'est
la même logique que le registre de dette de ce dépôt : figer, puis interdire la remontée.

**Ce qui doit tourner à chaque intégration.** La construction avec les avertissements traités en
erreurs. Les tests unitaires. La vérification de format. L'analyse statique. Le contrôle des
dépendances vulnérables. Ces cinq contrôles sont rapides, et chacun attrape une famille de défauts
qu'aucun relecteur ne devrait avoir à chercher — c'est ce que `quality-review-diffs-001` délègue
explicitement aux outils.

Les tests d'intégration et les tests HTTP suivent, plus lents, éventuellement dans un travail séparé.

**Le temps de retour est une propriété de conception.** Au-delà d'une dizaine de minutes, les gens
cessent d'attendre le résultat, fusionnent sans le lire, et la chaîne devient décorative. Les leviers
sont connus : mise en cache des dépendances, exécution parallèle des travaux indépendants, et surtout
tests rapides — c'est le retour sur investissement des règles pures de `tests-domain-rules-001`.

**Le cache accélère, il ne décide pas.** Un cache de dépendances doit être **clé sur le contenu du
verrou** : une clé trop large réutilise un état périmé, et la chaîne valide alors autre chose que ce
qui sera livré. Un cache qui influence le résultat est un défaut, pas une optimisation.

**Un échec doit désigner sa cause.** Le rapport indique quel travail, quelle étape, quel test, et avec
quel message. C'est le retour sur investissement des noms de test de `tests-xunit-aaa-001` : dans un
rapport de cent lignes, un nom qui énonce la règle transforme une enquête en lecture.

**La chaîne ne contourne rien.** Pas de test ignoré « temporairement », pas d'étape rendue non
bloquante pour livrer plus vite. Une étape qui n'échoue jamais ne protège rien, et son existence
donne une fausse assurance.

## Exemple commenté

Le résultat d'un travail, réduit à sa conjonction :

```csharp
public static string JobResult(bool buildSucceeded, bool testsSucceeded)
{
    // Les deux doivent réussir. Un travail « réussi » avec des tests rouges
    // donnerait une assurance fausse, ce qui est pire qu'aucune assurance.
    return buildSucceeded && testsSucceeded ? "success" : "failure";
}
```

Les étapes, dans l'ordre et avec leurs versions fixées :

```text
# global.json versionné : la machine de construction utilise exactement
# la même version d'outillage que les postes de développement.
{
  "sdk": { "version": "10.0.100", "rollForward": "disable" }
}
```

```text
# 1. Restaurer en mode strict : toute résolution non conforme au verrou échoue.
dotnet restore Forge.sln --locked-mode

# 2. Construire sans restaurer à nouveau : les avertissements sont des erreurs.
dotnet build Forge.sln --no-restore --configuration Release

# 3. Tester sans reconstruire : l'artefact testé est celui qui vient d'être produit.
dotnet test tests/Forge.UnitTests --no-build --configuration Release

# 4. Contrôles rapides, chacun bloquant.
dotnet format --verify-no-changes
dotnet list package --vulnerable --include-transitive
```

Et la clé de cache, calculée sur ce qui détermine réellement le contenu :

```csharp
public static string DependencyCacheKey(string runtimeIdentifier, string lockFileHash)
{
    // La clé inclut l'empreinte du verrou : un verrou modifié donne une clé
    // différente, donc un cache neuf. Une clé sur le seul nom de branche
    // réutiliserait un état périmé et validerait autre chose que la livraison.
    if (string.IsNullOrWhiteSpace(lockFileHash))
    {
        throw new ArgumentException("L'empreinte du verrou est requise.", nameof(lockFileHash));
    }

    return $"nuget-{runtimeIdentifier}-{lockFileHash}";
}
```

## Contre-exemple et erreur fréquente

```text
# Chaîne « rapide » : chaque décision échange une garantie contre du temps.

- name: Restaurer
  run: dotnet restore            # sans verrou : les versions dérivent avec le temps
  continue-on-error: true        # un échec de restauration n'arrête rien

- name: Construire
  run: dotnet build /p:TreatWarningsAsErrors=false

- name: Tester
  run: dotnet test --filter "Category!=Lent"   # les tests lents ne tournent jamais
  continue-on-error: true

- name: Cache
  uses: actions/cache
  with:
    key: dependances               # clé fixe : le cache n'est jamais renouvelé

- name: Publier
  run: ./deploy.sh production      # publication même si tout ce qui précède a échoué
```

Cinq décisions, cinq garanties perdues.

`continue-on-error` sur la restauration et sur les tests rend ces étapes décoratives. Elles
s'affichent en rouge, la chaîne continue, et la publication a lieu quand même : c'est exactement
l'assurance fausse dont parlait la leçon.

L'absence de verrou fait dériver les versions résolues. Un jour, une dépendance transitive change et la
chaîne échoue — ou pire, réussit avec un comportement différent.

Les avertissements désactivés annulent `quality-static-analysis-001` au seul endroit où le contrôle
était systématique.

Le filtre excluant les tests lents signifie qu'ils ne tournent jamais nulle part. Un test qui ne
s'exécute pas est un test qui n'existe pas.

La clé de cache fixe fait réutiliser indéfiniment le même état de dépendances : la chaîne valide un
ensemble qui n'est plus celui du dépôt.

## Vérification de compréhension

Votre chaîne est verte depuis trois semaines et un défaut évident vient d'atteindre la production.
Donnez trois causes possibles internes à la chaîne, et le contrôle qui aurait détecté chacune.

:::quiz
id=ci-pipeline-build-test-001-check
question=Pourquoi une clé de cache de dépendances doit-elle dépendre du contenu du verrou ?
option=Parce qu'une clé longue accélère la recherche dans le cache
option=Parce qu'une clé trop large réutilise un état de dépendances périmé : la chaîne valide alors autre chose que ce qui sera livré
option=Parce que le cache expire automatiquement lorsqu'il contient une empreinte
correct=1
success=Correct : un cache qui influence le résultat de la validation est un défaut, pas une optimisation.
retry=Relisez le passage sur le cache, et demandez-vous ce que valide la chaîne si les dépendances restaurées ne sont plus celles déclarées.
:::

## Exercice guidé

Ouvrez `ci-job-result-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, la table complète des quatre combinaisons possibles.
2. Implémentez la règle comme une conjonction stricte.
3. Vérifiez les deux cas mixtes, où un seul des deux signaux est vert.
4. Ouvrez ensuite `content/labs/ci-delivery/` et repérez l'ordre réel des étapes.

## Exercice autonome

Écrivez la chaîne d'intégration d'un projet à trois assemblages et deux suites de tests.

Décidez avant d'écrire : l'ordre des étapes, ce qui est bloquant, les versions fixées et où elles le
sont, la stratégie de cache et sa clé, ce qui s'exécute en parallèle, le temps de retour visé, et ce
que le rapport doit contenir pour qu'un échec désigne sa cause.

## Débogage

Un ticket indique : « La chaîne échoue depuis ce matin alors que personne n'a rien poussé. »

1. **Symptôme** : un échec sans modification du dépôt.
2. **Hypothèse** : une version non fixée a dérivé — outillage, image de base, ou dépendance
   transitive.
3. **Preuve** : comparez les versions résolues du dernier travail vert et du premier rouge.
4. **Prévention** : fixer la version d'outillage, restaurer en mode strict, et épingler les images de
   base par empreinte.

## Entretien

Question posée à voix haute : *que fait votre chaîne d'intégration, et en combien de temps ?*

Une réponse solide énumère les étapes dans l'ordre, explique ce qui est bloquant et pourquoi, cite la
reproductibilité par versions fixées, et sait dire qu'au-delà d'un certain temps de retour une chaîne
cesse d'être lue.

## Résumé

- Restaurer, construire, tester : chaque étape échoue vite et bloque la suite.
- La reproductibilité vient des versions fixées et du verrou de dépendances.
- Cinq contrôles rapides valent mieux qu'une revue qui cherche ce qu'un outil trouve.
- Une clé de cache trop large fait valider autre chose que la livraison.
- Une étape non bloquante ne protège rien et donne une assurance fausse.

## Cartes de révision

Question : que vaut un test exclu par un filtre permanent ? Réponse attendue : rien — un test qui ne
s'exécute jamais n'existe pas.

Question : pourquoi le temps de retour est-il une propriété de conception ? Réponse attendue :
au-delà de quelques minutes, les gens fusionnent sans attendre le résultat.

## Test de maîtrise

Sans relire, décrivez la chaîne complète d'un service : étapes et ordre, caractère bloquant de
chacune, versions fixées et leur emplacement, stratégie de cache, parallélisme, temps de retour visé,
contenu du rapport d'échec, et les trois contrôles dont l'absence vous paraîtrait la plus grave.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
