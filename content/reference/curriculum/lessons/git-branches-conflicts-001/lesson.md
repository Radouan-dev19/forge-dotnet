# Branches et conflits compris

## Objectif observable

À la fin de cette leçon, vous saurez choisir une stratégie d'intégration en connaissant son effet sur
l'historique, lire un conflit pour comprendre les deux intentions qui s'affrontent, et détecter un
marqueur oublié avant qu'il n'atteigne la construction.

## Prérequis

- Avoir lu `git-commits-history-001` et savoir produire un historique lisible.
- Savoir créer une branche et fusionner.

## Intuition

Une branche est un fil de travail parallèle. Un conflit n'est pas une panne : c'est le système qui
vous dit *« deux personnes ont modifié la même chose, je ne sais pas laquelle a raison »*. C'est
exactement la question qu'un outil ne peut pas trancher.

Le réflexe utile n'est donc pas de choisir vite, mais de comprendre les deux intentions avant de
décider. Choisir « la mienne » par défaut supprime silencieusement le travail de quelqu'un.

## Explication

**Une branche courte évite le conflit.** Plus une branche vit longtemps, plus elle diverge, et plus la
réintégration est douloureuse. Une branche de deux jours produit un conflit trivial ; une branche de
trois semaines produit un conflit dont personne ne se souvient des intentions. La meilleure stratégie
de résolution est de réduire la surface où le conflit peut naître.

**Deux façons d'intégrer, deux historiques.** *La fusion* crée un commit qui a deux parents et
conserve la chronologie réelle : on voit que le travail s'est fait en parallèle. *Le rebasage* rejoue
les commits de la branche au-dessus de la cible, produisant un historique linéaire, plus facile à
lire, mais qui réécrit les commits — ils changent d'identifiant.

D'où la règle de `git-commits-history-001` : rebaser une branche que personne n'a récupérée est sain ;
rebaser une branche partagée casse la copie des autres.

**Un conflit se lit en trois parties.** Le marqueur d'ouverture, votre version ; le séparateur ; la
version entrante ; le marqueur de fermeture avec son étiquette. Ces marqueurs sont du texte inséré
dans le fichier : ils ne compilent pas, et c'est volontaire — le système préfère un fichier cassé à
une fusion silencieusement fausse.

**Résoudre, c'est comprendre deux intentions.** Prendre systématiquement l'une des deux versions n'est
une résolution que par accident. La bonne démarche : lire les deux commits d'origine et leurs messages
— c'est là que le *pourquoi* est écrit —, décider ce que le code doit faire, puis écrire cette
version, qui peut n'être ni l'une ni l'autre.

Après résolution, la suite de tests doit être exécutée. Un conflit résolu qui compile n'est pas un
conflit résolu correctement : deux intentions peuvent être syntaxiquement compatibles et
fonctionnellement contradictoires.

**Le pire conflit est celui qui n'en est pas un.** Deux personnes modifient des lignes différentes du
même fichier : le système fusionne sans rien signaler. Si l'une a renommé un champ et l'autre ajouté
un appel à ce champ, le résultat ne compile pas — ou pire, compile et se comporte mal. Aucun outil ne
peut le détecter ; seule la construction après fusion le peut.

**Un marqueur oublié doit échouer tôt.** Un fichier commis avec ses marqueurs de conflit atteint la
branche principale et casse la construction pour tout le monde. La détection est mécanique et coûte
une seconde : chercher les trois marqueurs en début de ligne, avant de commettre. C'est le contrôle
que l'exercice de cette leçon demande d'écrire.

**Ne pas confondre branche et environnement.** Une branche est un fil de modifications, pas un serveur.
Le nom d'une branche ne déploie rien : c'est la chaîne de livraison qui décide, sujet de
`ci-deployment-gates-001`.

## Exemple commenté

La détection des trois marqueurs, avant compilation ou fusion :

```csharp
public static bool HasConflictMarkers(string? content)
{
    if (string.IsNullOrEmpty(content))
    {
        return false;
    }

    // Les trois marqueurs sont recherchés en début de ligne : une ligne de code
    // ou de documentation peut légitimement contenir ces caractères ailleurs.
    string[] markers = ["<<<<<<<", "=======", ">>>>>>>"];

    return content
        .Split('\n')
        .Any(line => markers.Any(marker => line.StartsWith(marker, StringComparison.Ordinal)));
}
```

Un conflit réel, tel qu'il apparaît dans le fichier :

```text
public decimal Fee(Order order)
{
<<<<<<< HEAD
    // Notre branche : le seuil a été porté à 50 euros par la demande commerciale.
    return order.Total >= 50m ? 0m : 4.9m;
=======
    // Branche entrante : le mode express a été ajouté, seuil inchangé à 100.
    return order.Express ? 9.9m : (order.Total >= 100m ? 0m : 4.9m);
>>>>>>> feature/livraison-express
}
```

La résolution correcte n'est ni l'une ni l'autre : les deux intentions sont valides et se composent.

```csharp
public decimal Fee(Order order)
{
    // Les deux intentions retenues : le nouveau seuil commercial de 50 euros,
    // et le mode express qui reste payant quel que soit le montant.
    // Prendre « la mienne » aurait supprimé l'express ; prendre « la sienne »
    // aurait annulé la décision commerciale.
    if (order.Express)
    {
        return 9.9m;
    }

    return order.Total >= 50m ? 0m : 4.9m;
}
```

## Contre-exemple et erreur fréquente

```text
# Branche ouverte il y a sept semaines, jamais synchronisée avec la principale.
$ git merge main
Auto-merging src/Billing/ShippingRules.cs
CONFLICT (content): Merge conflict in src/Billing/ShippingRules.cs
CONFLICT (content): Merge conflict in src/Billing/InvoiceService.cs
CONFLICT (content): Merge conflict in src/Api/OrdersController.cs
... 47 fichiers en conflit

# Résolution : tout prendre de son côté, sans lire.
$ git checkout --ours .
$ git add -A
$ git commit -m "merge"
$ git push
```

Quatre défauts qui se cumulent.

Sept semaines sans synchronisation garantissent quarante-sept conflits. La cause n'est pas la fusion,
c'est la durée de vie de la branche : synchroniser chaque jour aurait produit des conflits triviaux,
résolus au fil de l'eau.

`--ours` sur l'ensemble supprime, sans les lire, toutes les modifications faites sur la branche
principale pendant sept semaines. Le travail de plusieurs personnes disparaît sans laisser de trace
d'erreur — la construction sera verte, et le défaut se manifestera en production.

Aucun test n'est exécuté avant la publication. Même une résolution attentive doit être vérifiée : deux
intentions peuvent compiler ensemble et se contredire.

Le message « merge » ne dit rien des arbitrages rendus sur quarante-sept fichiers. Le corps du message
d'un commit de fusion conflictuelle est précisément l'endroit où justifier les choix non évidents.

## Vérification de compréhension

Deux branches modifient la même méthode : l'une change le seuil, l'autre ajoute un paramètre. Décrivez
votre démarche de résolution, et dites ce que vous faites avant de publier.

:::quiz
id=git-branches-conflicts-001-check
question=Pourquoi un conflit non signalé peut-il être plus dangereux qu'un conflit signalé ?
option=Parce qu'il produit toujours plus de lignes à relire
option=Parce que deux modifications sur des lignes différentes fusionnent silencieusement : un renommage d'un côté et un nouvel appel de l'autre passent sans avertissement
option=Parce que le système refuse alors de construire le résultat de la fusion
correct=1
success=Correct : seule la construction et la suite de tests après fusion peuvent révéler cette incompatibilité — aucun outil de fusion textuelle ne la voit.
retry=Relisez le passage sur le conflit qui n'en est pas un, et demandez-vous ce que voit un outil qui compare ligne par ligne.
:::

## Exercice guidé

Ouvrez `git-conflict-marker-001` dans `/practice`, puis procédez ainsi.

1. Listez, avant tout code, les trois marqueurs et la raison de les chercher en début de ligne.
2. Implémentez la détection en traitant chaque ligne séparément.
3. Vérifiez qu'un contenu vide ne déclenche rien, et qu'un marqueur en milieu de ligne n'est pas un
   faux positif.
4. Ajoutez cette vérification à votre routine avant commit, comme un contrôle mécanique.

## Exercice autonome

Provoquez volontairement un conflit dans le laboratoire `content/labs/git-review/`.

Créez deux branches modifiant la même méthode avec deux intentions différentes, fusionnez, puis
résolvez en composant les deux intentions plutôt qu'en choisissant un côté. Écrivez le message de
fusion en justifiant l'arbitrage, et exécutez la suite avant de conclure.

## Débogage

Un ticket indique : « La branche principale ne compile plus depuis la dernière fusion. »

1. **Symptôme** : la construction échoue sur la branche partagée, immédiatement après une intégration.
2. **Hypothèse** : des marqueurs de conflit ont été commis, ou une fusion silencieuse a cassé une
   référence.
3. **Preuve** : cherchez les marqueurs en début de ligne dans les fichiers du diff de fusion, puis
   lisez les erreurs de compilation restantes.
4. **Prévention** : détecter les marqueurs avant commit, et exiger une construction verte après
   résolution, jamais avant.

## Entretien

Question posée à voix haute : *comment gérez-vous un conflit de fusion ?*

Une réponse solide commence par réduire la durée de vie des branches, décrit la lecture des deux
intentions avant tout choix, refuse le choix systématique d'un côté, et sait dire que le conflit
silencieux est le plus dangereux.

## Résumé

- Une branche courte transforme un conflit douloureux en conflit trivial.
- Fusion et rebasage produisent deux historiques ; le rebasage réécrit.
- Un conflit se lit : deux intentions, dont la résolution peut n'être ni l'une ni l'autre.
- Le conflit non signalé est le plus dangereux ; seule la construction le révèle.
- Après résolution, la suite de tests est exécutée avant de publier.

## Cartes de révision

Question : pourquoi les marqueurs de conflit ne compilent-ils pas ? Réponse attendue : le système
préfère un fichier cassé à une fusion silencieusement fausse.

Question : quand le rebasage est-il interdit ? Réponse attendue : dès que la branche a été récupérée
par quelqu'un d'autre.

## Test de maîtrise

Sans relire, décrivez la stratégie d'intégration complète d'une équipe : durée de vie des branches,
fréquence de synchronisation, choix entre fusion et rebasage avec sa justification, démarche de
résolution d'un conflit, contrôles avant commit, vérifications après fusion, et contenu du message de
fusion.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
