# Chaînes, dates et culture explicite

## Objectif observable

À la fin de cette leçon, vous saurez choisir entre `DateOnly`, `DateTime` et `DateTimeOffset` pour
une donnée métier donnée, et vous saurez écrire une comparaison de chaînes qui produit le même
résultat sur toutes les machines.

## Prérequis

- Avoir lu `collections-arrays-001` et savoir choisir un type de collection.
- Savoir concaténer et comparer deux chaînes en C#.

## Intuition

Une chaîne en .NET est immuable : toute opération qui semble la modifier en fabrique une nouvelle.
Une date, elle, n'est presque jamais un simple nombre : elle dépend d'un fuseau, d'un calendrier et
d'une convention d'affichage.

Le fil conducteur des deux sujets est le même : ce qui paraît évident sur votre poste dépend en
réalité de réglages qui changent ailleurs. Rendre ces réglages explicites, c'est rendre le programme
reproductible.

## Explication

**L'immuabilité des chaînes a un coût mesurable.** `total += ligne;` dans une boucle de mille tours
alloue mille chaînes intermédiaires. `StringBuilder` conserve un tampon et ne matérialise le résultat
qu'à l'appel de `ToString()`. La bascule vaut à partir de quelques dizaines de concaténations dans
une boucle ; en dehors d'une boucle, la concaténation directe reste plus lisible.

**Comparer n'est pas une opération unique.** `string.Equals(a, b, StringComparison.Ordinal)` compare
les unités de code : c'est rapide, déterministe, et c'est le bon choix pour un identifiant, une clé
ou un nom de fichier. `StringComparison.OrdinalIgnoreCase` ajoute une insensibilité à la casse sans
dépendre de la culture. En revanche, `ToLower()` sans argument utilise la culture courante : en turc,
la minuscule de `I` n'est pas `i`, et une comparaison qui marchait à Paris échoue à Istanbul. Pour un
tri destiné à être **lu par un humain**, utilisez au contraire une comparaison culturelle explicite,
via `StringComparer.Create(culture, ignoreCase)`.

**Trois types de date, trois questions différentes.** `DateOnly` représente un jour de calendrier
sans heure : une date de naissance, une date d'échéance, un jour férié. C'est le bon type dès que
l'heure n'a pas de sens métier, et il supprime d'un coup toute la classe de bugs liée aux fuseaux.
`DateTime` porte un jour et une heure, avec un `Kind` qui vaut `Utc`, `Local` ou — le plus
dangereux — `Unspecified`. `DateTimeOffset` porte un instant absolu avec son décalage : c'est le bon
type pour horodater un événement qui s'est produit quelque part dans le monde.

La règle pratique : stockez et transportez en UTC, convertissez au dernier moment pour l'affichage,
et n'utilisez `DateOnly` que lorsque l'heure ne fait pas partie de la donnée.

**Le formatage aller-retour doit être stable.** `ToString()` sans argument produit un texte dépendant
de la culture : `14/03/2026` en France, `3/14/2026` aux États-Unis. Pour un fichier, une API ou un
identifiant, utilisez le format aller-retour `"O"` ou un format explicite avec
`CultureInfo.InvariantCulture`. Le critère est simple : si un autre programme relit la valeur, le
format est technique ; si un humain la lit, le format est culturel.

**Les durées ne s'additionnent pas naïvement.** Ajouter un mois à une date n'est pas ajouter trente
jours : `AddMonths(1)` sur le 31 janvier donne le 28 ou le 29 février. Ajouter vingt-quatre heures
n'est pas ajouter un jour lorsqu'un changement d'heure intervient. Nommez la sémantique voulue avant
de choisir l'opération.

## Exemple commenté

Calcul d'une échéance en jours ouvrés, sans heure ni fuseau :

```csharp
public static DateOnly AddBusinessDays(DateOnly start, int businessDays)
{
    ArgumentOutOfRangeException.ThrowIfNegative(businessDays);

    DateOnly current = start;
    int remaining = businessDays;
    while (remaining > 0)
    {
        current = current.AddDays(1);
        // Le week-end ne consomme pas de jour ouvré ; les jours fériés restent hors périmètre ici.
        if (current.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
        {
            remaining--;
        }
    }

    return current;
}
```

`DateOnly` rend le fuseau non pertinent : le résultat est identique à Paris, à Tokyo et sur un
serveur d'intégration réglé en UTC. Le commentaire dit explicitement ce qui n'est **pas** traité, ce
qui évite qu'un lecteur suppose une gestion des jours fériés.

## Contre-exemple et erreur fréquente

```csharp
public static bool IsSameDay(DateTime a, DateTime b)
{
    return a.ToString("d") == b.ToString("d");   // Comparaison de deux textes culturels.
}

public static bool IsAdmin(string role)
{
    return role.ToLower() == "admin";            // Dépend de la culture de la machine.
}
```

`IsSameDay` compare deux chaînes formatées selon la culture courante. Le test passe en local et peut
échouer sur un serveur configuré autrement ; pire, deux instants séparés de plusieurs heures peuvent
produire le même texte alors que leur `Kind` diffère. La comparaison correcte porte sur les valeurs :
`DateOnly.FromDateTime(a) == DateOnly.FromDateTime(b)`, après avoir normalisé les deux dates dans le
même référentiel.

`IsAdmin` illustre le piège turc décrit plus haut. La forme correcte est
`string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)` : un rôle est un identifiant
technique, pas un texte destiné à la lecture.

## Vérification de compréhension

Pour une date d'expiration d'abonnement, nommez : le type retenu, la raison, et ce qui se passerait
si le serveur changeait de fuseau horaire.

:::quiz
id=strings-dates-001-check
question=Quelle comparaison convient pour vérifier un code de rôle technique reçu d'une API ?
option=role.ToLower() == "admin", car la casse ne doit pas compter
option=string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase), qui ignore la casse sans dépendre de la culture
option=role.CompareTo("admin") == 0, qui applique les règles de tri de l'utilisateur
correct=1
success=Correct : une comparaison ordinale insensible à la casse est déterministe partout, alors que ToLower sans argument dépend de la culture de la machine.
retry=Relisez le passage sur la comparaison ordinale et culturelle, et l'exemple de la lettre I en turc.
:::

## Exercice guidé

Ouvrez `csharp-date-span-001` dans `/practice`, puis procédez ainsi.

1. Choisissez le type de date et écrivez en une phrase pourquoi l'heure est ou n'est pas pertinente.
2. Listez les cas : même jour, ordre inversé, intervalle traversant un week-end, intervalle nul.
3. Implémentez, puis exécutez le même test avec une culture différente pour vérifier la stabilité.
4. Notez tout écart entre votre prédiction et le résultat.

## Exercice autonome

Écrivez une méthode qui reçoit une date de commande et un délai contractuel en jours ouvrés, et
retourne la date limite d'expédition.

Décidez avant de coder : le type de date, le traitement des jours fériés, le comportement pour un
délai nul, et le format retenu si la valeur doit être écrite dans un fichier relu par un autre
programme.

## Débogage

Un ticket indique : « Les échéances calculées la nuit sont décalées d'un jour. »

1. **Symptôme** : l'écart n'apparaît que pour les exécutions proches de minuit.
2. **Hypothèse** : le calcul utilise l'heure locale du serveur alors que la donnée est un jour de
   calendrier.
3. **Preuve** : inspectez la propriété `Kind` de la valeur au point d'entrée. Un `Unspecified` ou un
   `Local` sur une donnée censée être un jour confirme l'hypothèse.
4. **Prévention** : basculez la donnée en `DateOnly`, et ajoutez un test qui fixe une horloge
   déterministe à 23 h 55 puis à 00 h 05 et vérifie l'égalité des résultats.

## Entretien

Question posée à voix haute : *quelle différence faites-vous entre `DateTime` et `DateTimeOffset`, et
laquelle stockez-vous en base ?*

Une réponse solide oppose l'instant absolu au couple date-heure ambigu, mentionne le piège du `Kind`
`Unspecified`, et explique le choix retenu en fonction de ce que la donnée représente — un événement
horodaté ou un jour de calendrier.

## Résumé

- Une chaîne est immuable : `StringBuilder` sert dans une boucle, pas ailleurs.
- Une comparaison technique est ordinale ; une comparaison lue par un humain est culturelle.
- `DateOnly` supprime la classe de bugs liée aux fuseaux quand l'heure n'a pas de sens.
- Le format aller-retour sert aux machines, le format culturel aux humains.
- Ajouter un mois ou vingt-quatre heures ne signifie pas ce qu'on croit.

## Cartes de révision

Question : pourquoi `ToLower()` sans argument est-il risqué dans une comparaison ? Réponse attendue :
il applique la culture courante, dont les règles de casse diffèrent selon la machine.

Question : quel type choisir pour une date d'échéance sans heure ? Réponse attendue : `DateOnly`, qui
rend le fuseau non pertinent.

## Test de maîtrise

Sans relire, écrivez la signature d'une méthode qui vérifie si un abonnement est expiré à une date
donnée. Justifiez le type de chaque paramètre, écrivez trois cas dont un sur la date exacte
d'expiration, et expliquez comment votre test resterait vert sur un serveur réglé sur un autre
fuseau.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
