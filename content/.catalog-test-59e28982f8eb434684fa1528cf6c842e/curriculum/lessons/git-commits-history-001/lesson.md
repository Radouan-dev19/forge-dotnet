# Commits atomiques et historique lisible

## Objectif observable

À la fin de cette leçon, vous saurez découper un travail en commits qui se tiennent seuls, écrire un
message qui explique le pourquoi, et vous servir de l'historique comme d'un outil de diagnostic plutôt
que comme d'un journal d'activité.

## Prérequis

- Avoir lu `quality-review-diffs-001` et savoir ce qui rend un diff relisible.
- Savoir enregistrer une modification dans un dépôt.

## Intuition

L'historique n'est pas une sauvegarde : c'est une **explication**. Quelqu'un — souvent vous, dans six
mois — cherchera pourquoi cette ligne existe. La réponse est dans le message du commit qui l'a
introduite, ou nulle part.

D'où la règle qui commande tout le reste : un commit contient **une intention**, et son message dit
laquelle.

## Explication

**Un commit atomique se tient seul.** Il compile, ses tests passent, et il peut être annulé sans en
casser un autre. C'est cette propriété qui rend possibles les outils réellement utiles : annuler
proprement une modification, ou trouver par dichotomie le commit qui a introduit un défaut.

Un commit qui ne compile pas casse la recherche par dichotomie exactement là où elle serait utile.

**Le message a une forme.** Une ligne de sujet courte — soixante-douze caractères au plus, sans point
final, à l'impératif — puis une ligne vide, puis un corps qui explique le pourquoi. La contrainte de
longueur n'est pas décorative : les outils affichent cette ligne seule dans les listes, et une ligne
tronquée devient illisible.

**Le sujet dit ce que fait le commit ; le corps dit pourquoi.** « Corrige le calcul des frais de
port » décrit l'action. Le corps explique le contexte : quel comportement était faux, dans quelles
conditions, pourquoi cette solution plutôt qu'une autre. Le *quoi* est déjà lisible dans le diff ; le
*pourquoi* n'existe nulle part ailleurs.

Un message qui n'apporte rien — « correctif », « mise à jour », « wip » — supprime la seule trace de
l'intention.

**Un commit, une intention.** Restructuration et correction ne partagent pas un commit — c'est ce
qu'exige `quality-regression-refactoring-001`. Ni une correction et une nouvelle fonctionnalité. Ni
un renommage massif et un changement de règle : le renommage noie le reste dans le diff, et la revue
de `quality-review-diffs-001` devient impossible.

**Ne jamais commettre un secret.** Une clé, un mot de passe, un jeton commis puis retiré **reste dans
l'historique** : le retirer dans un commit suivant ne le supprime pas, il faut réécrire l'historique
et considérer le secret comme compromis. La prévention est un fichier d'exclusion tenu à jour et une
relecture du diff avant chaque commit.

C'est le prolongement direct de `api-configuration-secrets-errors-001` : le dépôt est une surface de
fuite au même titre que le journal.

**L'historique se lit.** Retrouver qui a modifié une ligne et dans quel commit répond à « pourquoi
cette condition existe-t-elle ». Chercher par dichotomie le commit qui a introduit une régression
transforme une enquête en quelques essais. Ces deux outils ne valent que si les commits sont atomiques
et leurs messages informatifs — c'est le retour sur investissement de la discipline.

**Réécrire l'historique local, pas l'historique partagé.** Regrouper ses commits avant de publier est
sain : cela transforme une série d'essais en une histoire lisible. Réécrire ce que d'autres ont déjà
récupéré casse leur copie. La ligne est simple : avant publication, tout est permis ; après, plus
rien.

## Exemple commenté

La règle de validité d'un sujet de commit :

```csharp
public static bool IsCommitSubjectValid(string? subject)
{
    if (string.IsNullOrWhiteSpace(subject))
    {
        return false;
    }

    string trimmed = subject.Trim();

    // Soixante-douze caractères au plus : au-delà, les outils tronquent
    // la ligne dans les listes et le sujet devient illisible.
    if (trimmed.Length > 72)
    {
        return false;
    }

    // Pas de point final : le sujet est un titre, pas une phrase.
    return !trimmed.EndsWith('.');
}
```

Un message complet, sujet puis corps :

```text
Corrige la frontière du seuil de livraison gratuite

La comparaison était stricte alors que la règle commerciale annonce
« à partir de 50 euros ». Une commande à exactement 50 euros payait
donc les frais, ce que trois clients ont signalé cette semaine.

Le seuil est désormais large, et la théorie couvre les trois valeurs
autour de la frontière pour empêcher la régression de revenir.
```

Le diff correspondant tient en une ligne de code et quelques lignes de test : c'est ce qui rend
l'annulation sûre et la recherche par dichotomie utile.

Et le découpage d'un même travail en trois commits qui se tiennent chacun :

```text
1. Ajoute les tests de frontière du seuil de livraison
   (filet posé sur le comportement actuel : la suite est verte)

2. Extrait la règle de frais dans ShippingRules
   (refactoring pur : aucun test modifié, tous verts)

3. Corrige la frontière du seuil de livraison gratuite
   (le seul commit qui change un comportement, isolé et annulable seul)
```

## Contre-exemple et erreur fréquente

```text
$ git log --oneline

a91f3c2 fix
7d20e18 wip
3ce8841 maj
f0b7a55 ça marche
9e41d02 correctifs suite retours
b5c8f31 gros commit de la semaine
```

Et le contenu du dernier commit :

```text
 1 247 fichiers modifiés, 38 902 insertions, 21 447 suppressions

 - renommage global de 400 classes
 - nouvelle fonctionnalité de facturation
 - correction du calcul de TVA
 - mise à jour de 30 dépendances
 - appsettings.Production.json ajouté, avec la chaîne de connexion réelle
```

Quatre conséquences, toutes coûteuses.

Aucun message n'apprend rien. Pour savoir pourquoi une ligne existe, il faut retrouver la personne et
espérer qu'elle s'en souvienne.

Le commit unique mêle cinq intentions. Annuler la correction de TVA est impossible sans annuler aussi
le renommage et la nouvelle fonctionnalité. La recherche par dichotomie désignera ce commit et
n'apprendra rien : le défaut peut venir de n'importe laquelle des cinq parties.

Le renommage de quatre cents classes noie tout le reste dans le diff. Une revue est hors de portée :
elle sera approuvée sans lecture réelle.

Le fichier de configuration contenant la chaîne de connexion réelle est le défaut le plus grave. Le
retirer au commit suivant ne le supprimera pas de l'historique : il faudra réécrire l'historique
partagé et considérer le secret comme compromis, donc le changer.

## Vérification de compréhension

Vous avez, dans votre copie de travail, un renommage, une correction de défaut et deux nouveaux tests.
Décrivez le découpage en commits, l'ordre, et le sujet de chacun.

:::quiz
id=git-commits-history-001-check
question=Pourquoi un commit doit-il compiler et passer ses tests, même au milieu d'une série ?
option=Parce que le dépôt refuse d'enregistrer un commit qui ne compile pas
option=Parce que la recherche par dichotomie s'appuie sur la capacité à construire n'importe quel commit : un commit cassé rend le diagnostic impossible là où il servirait
option=Parce que les commits intermédiaires sont automatiquement déployés
correct=1
success=Correct : c'est ce qui permet aussi d'annuler un commit isolément sans en casser un autre.
retry=Relisez le passage sur le commit atomique, et demandez-vous ce que fait un outil qui construit chaque commit pour trouver une régression.
:::

## Exercice guidé

Ouvrez `git-commit-subject-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce qui rend un sujet invalide : vide, blancs seuls, trop long, point
   final.
2. Implémentez la validation en normalisant les blancs de bordure avant de mesurer.
3. Vérifiez la longueur exactement à la limite, dans les deux sens.
4. Relisez ensuite les messages de vos trois derniers commits et récrivez-les selon cette règle.

## Exercice autonome

Reprenez une modification en cours, ou le laboratoire `content/labs/git-review/`.

Découpez le travail en commits atomiques : listez les intentions, l'ordre, le sujet de chacun, et le
corps du seul commit qui change un comportement. Vérifiez que chaque commit compile et que sa suite
est verte.

## Débogage

Un ticket indique : « Une régression est apparue quelque part dans les deux cents commits du mois. »

1. **Symptôme** : le défaut est présent aujourd'hui, absent au début du mois.
2. **Hypothèse** : un commit précis l'a introduit et la recherche par dichotomie peut le désigner.
3. **Preuve** : construire et tester le commit médian, puis répéter. Huit essais suffisent pour deux
   cents commits, si chacun se construit.
4. **Prévention** : commits atomiques et messages informatifs, sans quoi la dichotomie désigne un
   commit fourre-tout qui n'explique rien.

## Entretien

Question posée à voix haute : *comment organisez-vous vos commits ?*

Une réponse solide définit l'atomicité par « compile, tests verts, annulable seul », distingue le sujet
du corps par quoi et pourquoi, cite la recherche par dichotomie comme bénéfice concret, et sait où
passe la ligne entre historique local et historique partagé.

## Résumé

- Un commit porte une intention et se tient seul.
- Sujet court à l'impératif, corps qui explique le pourquoi.
- Le *quoi* est dans le diff ; le *pourquoi* n'existe nulle part ailleurs.
- Un secret commis reste dans l'historique : il est compromis.
- Réécrire avant publication est sain ; après, cela casse les copies des autres.

## Cartes de révision

Question : que casse un commit qui ne compile pas ? Réponse attendue : la recherche par dichotomie,
exactement quand elle serait utile.

Question : pourquoi le corps du message est-il irremplaçable ? Réponse attendue : le diff montre déjà
ce qui a changé, jamais pourquoi.

## Test de maîtrise

Sans relire, décrivez votre méthode complète de découpage : la définition de l'atomicité, l'ordre des
commits pour une correction sous filet, la forme exacte du message, ce que vous vérifiez avant chaque
commit, la conduite à tenir si un secret a été commis, et la limite de la réécriture d'historique.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
