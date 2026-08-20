# Entrées locales et premiers breakpoints

## Objectif observable

À la fin de cette leçon, vous saurez convertir une entrée externe sans faire planter le programme sur
une saisie invalide, et vous saurez placer un point d'arrêt conditionnel qui s'arrête exactement sur
la valeur qui reproduit un ticket.

## Prérequis

- Avoir lu `csharp-control-methods-001` et savoir écrire une clause de garde.
- Disposer d'un IDE capable de poser un point d'arrêt et d'inspecter une variable locale.

## Intuition

Tout ce qui vient de l'extérieur du programme est du texte non fiable : une saisie clavier, une ligne
de fichier, un paramètre de requête. Le programme ne contrôle ni son format, ni sa présence.

Le débogueur, lui, sert à observer, pas à deviner. La faute la plus coûteuse en débogage n'est pas de
lire le mauvais code : c'est de modifier du code avant d'avoir regardé une seule valeur réelle.

## Explication

**Convertir sans faire confiance.** `int.Parse("abc")` lève une `FormatException`. Sur une entrée
utilisateur, ce n'est pas un bug de l'utilisateur : c'est un cas prévu que le code n'a pas traité. Le
couple `TryParse` sépare proprement les deux questions — *la conversion a-t-elle réussi ?* et *quelle
est la valeur ?* :

```csharp
if (!int.TryParse(text, out int quantity)) { /* entrée invalide */ }
```

La règle pratique : `Parse` pour une constante que vous écrivez vous-même, `TryParse` pour tout ce
qui traverse une frontière.

**Nommer la culture quand elle compte.** `decimal.Parse("1.5")` réussit ou échoue selon la culture de
la machine : en français, le séparateur décimal est la virgule. Un même programme donne alors deux
résultats sur deux postes. Pour une donnée technique — un fichier, une API, un identifiant — utilisez
`CultureInfo.InvariantCulture`. Pour une saisie humaine, utilisez explicitement la culture de
l'utilisateur. Ce qui est interdit, c'est de ne pas choisir.

**Le débogueur répond à une question précise.** Un point d'arrêt posé au hasard fait perdre du temps.
La méthode utile tient en quatre étapes : reproduire le symptôme avec des valeurs connues, formuler
une hypothèse falsifiable, poser le point d'arrêt à l'endroit où l'hypothèse serait réfutée, puis
observer sans rien modifier.

Sur un traitement de mille lignes, s'arrêter à chaque tour est inutilisable. Un **point d'arrêt
conditionnel** ne suspend l'exécution que si une condition est vraie — par exemple `line == 842` ou
`quantity < 0`. C'est la différence entre parcourir un fichier et ouvrir la bonne page.

Deux réflexes complètent l'outil. La fenêtre des variables locales affiche l'état réel : lisez-la
avant de formuler une deuxième hypothèse. Et le pas à pas *au-dessus* (`step over`) suffit tant que
la divergence n'est pas localisée ; entrer dans chaque appel dès le début fait perdre le fil.

**Observer ne veut pas dire modifier.** Changer la valeur d'une variable dans le débogueur pour
« voir » fabrique un état qui n'existe dans aucune exécution réelle. Le symptôme peut disparaître
sans que la cause soit comprise. Notez ce que vous voyez, sortez, puis écrivez un test qui échoue.

## Exemple commenté

Lecture d'une quantité saisie, avec conversion sûre et message actionnable :

```csharp
public static string DescribeQuantity(string? input)
{
    // L'absence de saisie est un cas prévu, pas une exception.
    if (string.IsNullOrWhiteSpace(input))
    {
        return "Quantité absente : saisissez un nombre entier positif.";
    }

    // TryParse répond d'abord « est-ce convertible ? », puis fournit la valeur.
    if (!int.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity))
    {
        return $"Quantité illisible : « {input} » n'est pas un entier.";
    }

    // La règle métier n'est atteinte qu'avec une valeur déjà convertie.
    return quantity <= 0
        ? "Quantité invalide : la valeur doit être strictement positive."
        : $"Quantité retenue : {quantity}.";
}
```

Trois messages distincts pour trois causes distinctes. Un utilisateur qui lit « illisible » sait
qu'il a fait une faute de frappe ; un utilisateur qui lit « invalide » sait que son nombre est refusé
par la règle.

## Contre-exemple et erreur fréquente

```csharp
public static int ReadQuantity()
{
    Console.Write("Quantité : ");
    return int.Parse(Console.ReadLine()!);   // Deux façons de planter sur la même ligne.
}
```

Deux défaillances cohabitent. Le `!` affirme au compilateur que `ReadLine()` ne retournera jamais
`null`, ce qui est faux dès que l'entrée est redirigée depuis un fichier vide : on obtient une
`NullReferenceException`. Et `Parse` lève une `FormatException` sur `"douze"` ou sur une chaîne vide.

Dans les deux cas, l'utilisateur voit une trace d'exception au lieu d'un message, et le journal
n'indique pas quelle valeur a échoué. Le réflexe d'entourer l'appel d'un `try/catch (Exception)` qui
retourne `0` aggrave la situation : la saisie fautive devient une quantité nulle silencieuse, et le
problème réapparaît trois étapes plus loin, sans lien visible avec sa cause.

## Vérification de compréhension

Nommez la différence entre une entrée absente, une entrée illisible et une entrée refusée par la
règle métier. Donnez un message distinct pour chacune.

:::quiz
id=csharp-io-debugger-001-check
question=Quand faut-il préférer TryParse à Parse ?
option=Toujours, car Parse est déprécié depuis .NET 6
option=Dès que la valeur traverse une frontière du programme : saisie, fichier, réseau, configuration
option=Uniquement pour les nombres décimaux, les entiers étant sans risque
correct=1
success=Correct : le programme ne contrôle pas le format d'une donnée externe, donc l'échec de conversion est un cas prévu à traiter, pas une exception à laisser remonter.
retry=Relisez le passage sur les frontières : la question n'est pas le type converti, mais l'origine de la donnée.
:::

## Exercice guidé

Ouvrez `csharp-input-normalize-001` dans `/practice`, puis procédez ainsi.

1. Écrivez les trois cas d'entrée fautive avant tout code : absente, illisible, hors règle.
2. Implémentez la conversion avec `TryParse` et une culture explicite.
3. Posez un point d'arrêt sur la ligne de conversion, exécutez avec une entrée fautive et lisez la
   valeur du booléen retourné dans la fenêtre des locales.
4. Notez ce que vous avez observé avant de modifier quoi que ce soit.

## Exercice autonome

Un fichier CSV contient une date par ligne, au format `2026-03-14`. Écrivez une méthode qui retourne
le nombre de dates valides et la première ligne fautive.

Décidez avant de coder : que faire d'une ligne vide, d'une date au format `14/03/2026`, et d'une date
valide mais située dans le futur. Justifiez le choix de la culture utilisée pour la conversion.

## Débogage

Un ticket indique : « L'import échoue sur un fichier de 3 000 lignes, sans dire laquelle. »

1. **Symptôme** : l'exception ne nomme ni la ligne ni la valeur.
2. **Hypothèse** : une seule ligne porte une valeur non convertible.
3. **Preuve** : posez un point d'arrêt conditionnel sur le corps de boucle, avec la condition
   `!int.TryParse(cell, out _)`. L'exécution s'arrête sur la ligne fautive et sur elle seule.
4. **Prévention** : enrichissez le message d'erreur avec le numéro de ligne et la valeur refusée,
   puis ajoutez un test portant sur un fichier contenant une ligne fautive.

## Entretien

Question posée à voix haute : *décrivez la dernière fois où vous avez utilisé un débogueur plutôt
qu'un affichage de trace. Qu'est-ce que cela vous a permis de voir ?*

Une réponse solide donne une hypothèse, l'endroit choisi pour le point d'arrêt et **pourquoi cet
endroit** réfutait l'hypothèse. Une réponse faible décrit l'outil sans jamais nommer la question à
laquelle il répondait.

## Résumé

- Toute donnée franchissant une frontière est du texte non fiable, y compris un nombre.
- `TryParse` transforme un échec de conversion en cas traité, pas en exception subie.
- La culture est un choix explicite ; ne pas choisir revient à dépendre de la machine.
- Un point d'arrêt conditionnel remplace mille arrêts inutiles.
- On observe d'abord, on modifie ensuite.

## Cartes de révision

Question : que masque un `catch (Exception)` qui retourne une valeur par défaut ? Réponse attendue :
la cause réelle, remplacée par une donnée valide en apparence qui échouera plus loin.

Question : quelle condition rend un point d'arrêt utile sur une boucle longue ? Réponse attendue :
une condition qui décrit exactement l'état fautif recherché.

## Test de maîtrise

Sans relire, écrivez une méthode qui lit un montant depuis une chaîne et retourne soit le montant,
soit un message d'erreur distinguant absence, format et règle métier. Posez ensuite un point d'arrêt
conditionnel qui ne s'arrête que sur un montant négatif, et expliquez ce que vous iriez lire dans la
fenêtre des variables locales.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
