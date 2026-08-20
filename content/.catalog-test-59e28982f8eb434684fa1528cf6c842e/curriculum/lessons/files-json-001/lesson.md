# Fichiers et JSON local robustes

## Objectif observable

À la fin de cette leçon, vous saurez lire et écrire un fichier JSON local sans corrompre le fichier
existant en cas d'interruption, et vous saurez distinguer un fichier absent, illisible et mal formé
dans trois messages distincts.

## Prérequis

- Avoir lu `linq-lambdas-001` et savoir maîtriser le moment d'exécution d'une requête.
- Savoir écrire une classe avec des propriétés publiques.

## Intuition

Un fichier est une frontière : il peut être absent, verrouillé par un autre processus, tronqué par
une coupure, ou contenir autre chose que ce que vous attendez. Aucune de ces situations n'est un cas
exotique — ce sont les quatre premières à traiter.

Le contenu JSON pose une seconde question : le texte est syntaxiquement valide, mais correspond-il au
contrat que votre code suppose ? Les deux vérifications sont distinctes.

## Explication

**Nommer les trois défaillances.** *Absent* — le fichier n'existe pas, ce qui est souvent normal au
premier lancement. *Illisible* — droits insuffisants, verrou d'un autre processus, disque en erreur.
*Mal formé* — le texte n'est pas du JSON valide, ou ne correspond pas au type attendu. Ces trois cas
appellent trois réactions et trois messages différents ; les fondre dans un seul `catch` rend le
diagnostic impossible.

`File.Exists` avant lecture ne supprime pas le besoin de gérer l'échec : entre le test et l'ouverture,
le fichier peut disparaître. Traitez `FileNotFoundException` malgré la vérification.

**L'écriture atomique.** Écrire directement dans le fichier de destination est dangereux : une
coupure au milieu laisse un fichier tronqué, donc illisible, et l'ancien contenu est perdu. Le motif
robuste écrit d'abord dans un fichier temporaire du **même répertoire**, puis remplace la cible par un
déplacement. Sur la plupart des systèmes, ce remplacement est atomique : à tout instant, la cible est
soit l'ancienne version complète, soit la nouvelle.

`File.Replace` va plus loin en conservant une sauvegarde de l'ancien contenu. Le même répertoire
importe : un déplacement entre deux volumes est une copie suivie d'une suppression, et perd
l'atomicité.

**Sérialiser avec des options explicites.** `System.Text.Json` est sensible à la casse par défaut et
n'accepte ni commentaires ni virgule finale. Ces réglages doivent être choisis, pas subis :

```csharp
private static readonly JsonSerializerOptions Options = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    AllowTrailingCommas = false,
};
```

Déclarez l'instance `static readonly` : construire un `JsonSerializerOptions` à chaque appel recrée le
cache de métadonnées et coûte cher.

**La désérialisation peut retourner `null`.** `JsonSerializer.Deserialize<T>("null")` retourne `null`
sans lever. Un `T` non nullable en retour ne vous protège donc pas : vérifiez explicitement et
transformez ce cas en erreur nommée plutôt qu'en `NullReferenceException` plus loin.

**Valider après désérialisation.** Le sérialiseur vérifie la forme, pas le sens. Un JSON qui parse
correctement peut contenir une quantité négative ou une date incohérente. La désérialisation produit
un objet de transport ; la validation métier vient ensuite, avant de construire les objets du domaine
avec leurs invariants — c'est le lien direct avec `oop-encapsulation-001`.

**Encodage et fins de ligne.** Écrivez en UTF-8. Sous Windows, la classe `UTF8Encoding` écrit par
défaut une marque d'ordre des octets qui perturbe certains lecteurs : `new UTF8Encoding(false)`
l'évite. Ces détails ne se voient pas sur votre poste et apparaissent en intégration.

**Libérer les ressources.** Un flux non libéré garde un verrou : le fichier ne peut plus être
remplacé, et le symptôme se manifeste ailleurs. `using` garantit la libération, y compris en cas
d'exception.

## Exemple commenté

Écriture atomique et lecture qui distingue les trois défaillances :

```csharp
public static void SaveAtomic<T>(string path, T value)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(path);

    // Le fichier temporaire est dans le même répertoire : le remplacement reste atomique.
    string temporary = path + ".tmp";
    string json = JsonSerializer.Serialize(value, Options);
    File.WriteAllText(temporary, json, new UTF8Encoding(false));

    // À tout instant, la cible contient l'ancienne version complète ou la nouvelle.
    File.Move(temporary, path, overwrite: true);
}

public static bool TryLoad<T>(string path, out T? value, out string? error)
{
    value = default;
    error = null;

    if (!File.Exists(path))
    {
        error = $"Fichier absent : {path}.";
        return false;
    }

    try
    {
        string json = File.ReadAllText(path, new UTF8Encoding(false));
        value = JsonSerializer.Deserialize<T>(json, Options);
        if (value is null)
        {
            error = "Le document JSON est vide ou vaut null.";
            return false;
        }

        return true;
    }
    catch (JsonException exception)
    {
        // Mal formé : la position renseignée par l'exception rend le message actionnable.
        error = $"JSON invalide ({exception.LineNumber}) : {exception.Message}";
        return false;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        // Illisible : verrou, droits, disque. Distinct d'un contenu fautif.
        error = $"Lecture impossible : {exception.Message}";
        return false;
    }
}
```

Le filtre `when` sur le second `catch` est ce qui évite d'avaler les exceptions étrangères : une
`OutOfMemoryException` ou un bug de code remonte normalement.

## Contre-exemple et erreur fréquente

```csharp
public static Settings Load(string path)
{
    try
    {
        return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path))!;
    }
    catch
    {
        return new Settings();   // Absent, verrouillé, corrompu : même réponse silencieuse.
    }
}

public static void Save(string path, Settings settings)
{
    File.WriteAllText(path, JsonSerializer.Serialize(settings));   // Écriture directe.
}
```

`Load` confond les trois défaillances. Un fichier corrompu par une coupure produit exactement le même
résultat qu'une première exécution : des réglages par défaut, sans avertissement. L'utilisateur perd
sa configuration et personne n'en saura rien. Le `!` masque en outre le cas où le document vaut
`null`.

`Save` est la cause probable de la corruption : une interruption pendant l'écriture laisse un fichier
tronqué, et l'ancien contenu a déjà disparu. Les deux méthodes forment un cycle où l'écriture détruit
et où la lecture cache la destruction.

## Vérification de compréhension

Pour un fichier de progression local, dites ce que doit faire le programme dans chacun des trois cas —
absent, illisible, mal formé — et pourquoi les réponses diffèrent.

:::quiz
id=files-json-001-check
question=Pourquoi écrire dans un fichier temporaire du même répertoire avant de remplacer la cible ?
option=Pour accélérer l'écriture en évitant la fragmentation du disque
option=Parce qu'une interruption laisse alors la cible intacte : elle contient l'ancienne version complète ou la nouvelle, jamais un contenu tronqué
option=Parce que System.Text.Json refuse d'écrire dans un fichier existant
correct=1
success=Correct : le remplacement est atomique, ce qui garantit qu'aucune coupure ne peut laisser un fichier à moitié écrit.
retry=Relisez le passage sur l'écriture atomique, et notez pourquoi le fichier temporaire doit être dans le même répertoire.
:::

## Exercice guidé

Ouvrez `csharp-json-number-count-001` dans `/practice`, puis procédez ainsi.

1. Listez les trois défaillances et le message attendu pour chacune, avant tout code.
2. Déclarez les options de sérialisation en `static readonly`.
3. Implémentez la lecture, en traitant explicitement le retour `null` de la désérialisation.
4. Vérifiez qu'aucun `catch` général n'attrape autre chose que les cas nommés.

## Exercice autonome

Écrivez un couple de méthodes qui sauvegarde et recharge une liste de commandes dans un fichier JSON
local.

Décidez avant de coder : le comportement au premier lancement, la stratégie en cas de fichier
corrompu, l'encodage, et si une sauvegarde de l'ancien contenu doit être conservée. Justifiez le choix
entre lever une exception et retourner un résultat pour chaque défaillance.

## Débogage

Un ticket indique : « Après un arrêt brutal, l'application démarre avec des réglages vides. »

1. **Symptôme** : la configuration est perdue, sans message d'erreur.
2. **Hypothèse** : l'écriture n'est pas atomique et la lecture masque la corruption par un repli
   silencieux.
3. **Preuve** : ouvrez le fichier après l'arrêt brutal et constatez sa troncature ; ajoutez
   temporairement une trace dans le bloc de repli pour confirmer qu'il est atteint.
4. **Prévention** : passez à l'écriture atomique, distinguez les trois défaillances à la lecture, et
   ajoutez un test qui charge un fichier volontairement tronqué et vérifie le message obtenu.

## Entretien

Question posée à voix haute : *comment vous assurez-vous qu'une écriture de fichier ne corrompt pas
les données existantes ?*

Une réponse solide décrit le motif temporaire puis remplacement, explique pourquoi le même répertoire
importe, et distingue les défaillances côté lecture. Elle reconnaît aussi les limites : le motif ne
protège pas d'un disque défaillant ni d'un accès concurrent non coordonné.

## Résumé

- Absent, illisible et mal formé sont trois défaillances distinctes, avec trois messages.
- Écrire dans un temporaire du même répertoire, puis remplacer : la cible n'est jamais tronquée.
- Les options de sérialisation se déclarent explicitement et se partagent en `static readonly`.
- La désérialisation peut retourner `null` malgré un type non nullable en retour.
- Le sérialiseur valide la forme ; la validation métier vient ensuite.

## Cartes de révision

Question : pourquoi un `catch` sans filtre est-il dangereux autour d'une lecture de fichier ? Réponse
attendue : il confond droit, verrou, absence et contenu fautif, et masque aussi les défaillances sans
rapport.

Question : que garantit `File.Move(temporary, path, overwrite: true)` dans le même répertoire ?
Réponse attendue : le remplacement atomique, donc l'absence de fichier cible partiellement écrit.

## Test de maîtrise

Sans relire, écrivez la signature d'une sauvegarde atomique et d'un chargement tolérant pour un
document local. Nommez les trois défaillances traitées, précisez l'encodage retenu, et décrivez le
test qui prouve qu'un fichier corrompu ne détruit pas la sauvegarde précédente.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
