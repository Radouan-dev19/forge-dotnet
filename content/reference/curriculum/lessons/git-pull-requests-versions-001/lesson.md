# Demandes de tirage et versionnage

## Objectif observable

À la fin de cette leçon, vous saurez préparer une demande d'intégration qu'un relecteur peut traiter
sans vous poser de question, choisir le composant de version à incrémenter selon la nature du
changement, et publier une version dont on peut retrouver le contenu exact.

## Prérequis

- Avoir lu `git-branches-conflicts-001` et savoir intégrer une branche.
- Avoir lu `api-openapi-contracts-001` et savoir classer un changement en compatible ou rupture.

## Intuition

Une demande d'intégration est un **acte de communication**, pas un bouton. Son coût réel n'est pas
d'écrire le code : c'est le temps qu'un collègue passera à comprendre ce que vous avez fait et
pourquoi.

Le numéro de version est le même acte, adressé à vos utilisateurs : il leur dit, avant même de lire
les notes, s'ils peuvent mettre à jour sans rien changer chez eux.

## Explication

**La description répond à trois questions.** *Pourquoi* ce changement existe — le problème, pas la
solution. *Quoi* a changé, en quelques lignes, pour orienter la lecture du diff. *Comment vérifier* :
ce que le relecteur peut exécuter pour se convaincre, et ce qui est couvert par des tests.

Un lien vers le ticket ne remplace pas la description : le relecteur devrait pouvoir décider sans
ouvrir un autre outil.

**L'auteur relit son propre diff avant de le soumettre.** C'est le geste au meilleur rendement de
toute la leçon. On y trouve les traces de mise au point oubliées, les fichiers ajoutés par accident,
les commentaires qui ne servent plus, et parfois un vrai défaut. Soumettre sans se relire fait payer à
quelqu'un d'autre le temps qu'on n'a pas voulu passer.

**La taille est une décision, pas une fatalité.** Au-delà de quelques centaines de lignes utiles, la
qualité de la revue s'effondre — c'est le constat de `quality-review-diffs-001`. Un travail volumineux
se découpe en plusieurs demandes successives, chacune fusionnable seule.

**Ce qui doit être vert avant la revue.** La construction, les tests, l'analyse statique, le format.
Demander une revue sur une branche rouge fait perdre du temps au relecteur sur des défauts qu'une
machine a déjà signalés. C'est la raison d'être des contrôles automatiques décrits dans
`ci-pipeline-build-test-001`.

**Le versionnage sémantique répond à une seule question.** Trois nombres. Le premier change quand la
mise à jour **casse** un appelant existant. Le deuxième quand une fonctionnalité est ajoutée sans rien
casser. Le troisième quand seule une correction interne a lieu.

Le critère est celui de `api-openapi-contracts-001` : un appelant écrit avant ce changement
continue-t-il de fonctionner sans être modifié ? Non, c'est le premier nombre. Oui, avec quelque chose
en plus, c'est le deuxième. Oui, sans rien de plus, c'est le troisième.

L'incrémentation remet à zéro les composants de droite : après `1.4.7`, une correction donne `1.4.8`,
une fonctionnalité donne `1.5.0`, une rupture donne `2.0.0`.

**Une version publiée est immuable.** Republier un contenu différent sous le même numéro rend toute
reproduction impossible : deux machines installant « la 1.4.7 » n'obtiennent pas la même chose. Une
correction, même minime, prend un nouveau numéro.

**Les notes de version s'adressent à l'utilisateur.** Elles disent ce qui change pour lui, pas la liste
des commits. Les ruptures y figurent en tête, avec ce qu'il faut faire pour migrer. Un historique de
commits propre rend leur rédaction rapide, ce qui est le bénéfice différé de
`git-commits-history-001`.

## Exemple commenté

L'incrémentation du composant de correction, avec validation des trois nombres :

```csharp
public static string NextPatch(string? version)
{
    if (string.IsNullOrWhiteSpace(version))
    {
        throw new ArgumentException("La version est requise.", nameof(version));
    }

    string[] parts = version.Trim().Split('.');
    if (parts.Length != 3)
    {
        throw new FormatException("La version attendue comporte trois composants.");
    }

    int[] numbers = new int[3];
    for (int index = 0; index < 3; index++)
    {
        // Un composant négatif ou non numérique n'est pas une version :
        // échouer ici évite de produire un numéro qui ne désigne rien.
        if (!int.TryParse(parts[index], out numbers[index]) || numbers[index] < 0)
        {
            throw new FormatException("Chaque composant doit être un entier non négatif.");
        }
    }

    // Seul le troisième composant change : une correction n'ajoute rien et ne casse rien.
    return $"{numbers[0]}.{numbers[1]}.{numbers[2] + 1}";
}
```

Une description de demande qu'un relecteur peut traiter seul :

```text
Corrige la frontière du seuil de livraison gratuite

Pourquoi
  La règle commerciale annonce « à partir de 50 euros ». La comparaison était
  stricte : une commande à exactement 50 euros payait les frais. Trois clients
  l'ont signalé cette semaine.

Quoi
  - ShippingRules.Fee : comparaison stricte devenue large
  - Théorie de test étendue aux trois valeurs autour du seuil

Comment vérifier
  dotnet test tests/Billing.UnitTests --filter FullyQualifiedName~Shipping
  Les cas 49,99 / 50,00 / 50,01 sont couverts et échouaient avant le correctif.

Impact de version
  Correction de comportement sans changement de contrat : 1.4.7 vers 1.4.8.
```

Et le classement d'un changement, ramené à sa règle :

```text
Ajouter un point d'entrée              -> composant du milieu   (1.4.7 -> 1.5.0)
Ajouter un champ optionnel en entrée   -> composant du milieu   (1.4.7 -> 1.5.0)
Corriger un calcul interne             -> composant de droite   (1.4.7 -> 1.4.8)
Renommer un champ de réponse           -> composant de gauche   (1.4.7 -> 2.0.0)
Rendre obligatoire un champ optionnel  -> composant de gauche   (1.4.7 -> 2.0.0)
```

## Contre-exemple et erreur fréquente

```text
Titre : maj
Description : (vide)

47 fichiers modifiés, 3 120 insertions
Construction : en échec (12 tests rouges)
Analyse statique : 38 nouveaux avertissements

Commentaire de l'auteur : « c'est urgent, mergez et je corrige après »
```

Et, une fois fusionné :

```text
$ git tag -f v1.4.7        # la version 1.4.7 existait déjà, elle est écrasée
$ git push --force --tags
```

Cinq défauts.

Le titre et la description vides transfèrent tout le travail de compréhension au relecteur. Il devra
reconstituer l'intention à partir de trois mille lignes.

La construction rouge signifie que la revue portera sur du code dont on ne sait pas s'il fonctionne.
Les douze tests en échec sont peut-être des régressions réelles, peut-être des tests à mettre à jour :
personne ne le sait, et le relecteur n'a pas à trancher cela.

Les trente-huit avertissements sont exactement ce que `quality-static-analysis-001` interdit
d'accumuler.

« Mergez et je corrige après » supprime la fonction de la revue. Si l'urgence est réelle, la réponse
est un correctif minimal et isolé, pas la fusion d'un travail non terminé.

Le déplacement forcé de l'étiquette est le plus grave : deux machines installant « la 1.4.7 »
n'obtiennent plus le même contenu. Toute reproduction devient impossible, et le diagnostic d'un
incident aussi.

## Vérification de compréhension

Vous ajoutez un paramètre de filtre optionnel à un point d'entrée existant, et vous corrigez au
passage un calcul interne faux. Dites quel numéro de version vous publiez depuis `2.3.9`, et pourquoi.

:::quiz
id=git-pull-requests-versions-001-check
question=Pourquoi une version publiée ne doit-elle jamais être republiée avec un contenu différent ?
option=Parce que les outils de publication refusent techniquement toute republication
option=Parce que deux installations du même numéro donneraient des contenus différents : la reproduction d'un environnement et le diagnostic d'un incident deviennent impossibles
option=Parce que le numéro de version doit toujours croître d'exactement une unité
correct=1
success=Correct : une version est un identifiant immuable. Même une correction minime prend un nouveau numéro.
retry=Relisez le passage sur l'immuabilité, et demandez-vous ce que signifie « la 1.4.7 » si son contenu a changé.
:::

## Exercice guidé

Ouvrez `git-version-patch-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce qui rend une version invalide : nombre de composants, valeur négative,
   composant non numérique.
2. Implémentez l'incrémentation en validant les trois composants avant de produire le résultat.
3. Vérifiez qu'aucun autre composant ne bouge, y compris lorsque le troisième passe de neuf à dix.
4. Classez ensuite trois changements récents de votre code selon le composant à incrémenter.

## Exercice autonome

Préparez une demande d'intégration complète pour une modification de votre choix.

Rédigez la description en trois parties, relisez votre propre diff et notez ce que vous y corrigez,
vérifiez que tout est vert, décidez du numéro de version et justifiez le composant retenu, puis
rédigez les notes destinées aux utilisateurs.

## Débogage

Un ticket indique : « Le correctif fonctionne chez nous et pas chez le client, qui affirme utiliser la
même version. »

1. **Symptôme** : le comportement diffère à numéro de version identique.
2. **Hypothèse** : l'étiquette a été déplacée, ou l'artefact publié ne correspond pas au code
   étiqueté.
3. **Preuve** : comparez l'empreinte de l'artefact installé à celle publiée, et l'historique de
   l'étiquette.
4. **Prévention** : rendre les versions immuables, interdire le déplacement d'étiquette, et publier une
   correction sous un nouveau numéro.

## Entretien

Question posée à voix haute : *comment décidez-vous du numéro de version d'une livraison ?*

Une réponse solide part du critère de compatibilité — un appelant existant continue-t-il de
fonctionner —, donne un exemple pour chacun des trois composants, mentionne l'immuabilité, et fait le
lien avec les notes destinées aux utilisateurs.

## Résumé

- Une description répond au pourquoi, au quoi et au comment vérifier.
- L'auteur relit son propre diff avant de le soumettre.
- Tout est vert avant la revue : la machine d'abord, l'humain ensuite.
- Rupture, ajout, correction : trois composants, un seul critère.
- Une version publiée est immuable ; une correction prend un nouveau numéro.

## Cartes de révision

Question : que fait l'incrémentation d'un composant aux composants de droite ? Réponse attendue : elle
les remet à zéro.

Question : à qui s'adressent les notes de version ? Réponse attendue : à l'utilisateur — ce qui change
pour lui, ruptures en tête, pas la liste des commits.

## Test de maîtrise

Sans relire, décrivez le processus complet d'une livraison : préparation de la demande, contenu de sa
description, contrôles exigés avant revue, critères de découpage, choix du numéro de version avec
justification, contenu des notes, et les règles qui garantissent qu'une version reste reproductible.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
