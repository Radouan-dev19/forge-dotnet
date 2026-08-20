# Portes de déploiement et retour arrière

## Objectif observable

À la fin de cette leçon, vous saurez énoncer les conditions qui doivent tenir ensemble avant un
déploiement, préparer un retour arrière avant d'en avoir besoin, et reconnaître les portes qui donnent
une assurance sans rien vérifier.

## Prérequis

- Avoir lu `ci-artifacts-variables-secrets-001` et savoir promouvoir un artefact.
- Avoir lu `docker-runtime-security-001` et savoir ce qu'une conjonction de conditions garantit.

## Intuition

Une porte est une condition qui doit tenir pour qu'un déploiement ait lieu. Sa valeur ne vient pas de
son existence mais de sa capacité à **refuser**. Une porte qui n'a jamais bloqué personne est
probablement décorative.

Le second principe : la question n'est pas *comment éviter tout incident* — c'est impossible — mais
*combien de temps pour revenir en arrière*. Un déploiement dont le retour arrière n'a pas été préparé
n'est pas un déploiement, c'est un pari.

## Explication

**Trois conditions, simultanées.** Les preuves automatiques sont vertes — construction, tests,
analyse. L'environnement cible est protégé, c'est-à-dire qu'on n'y déploie pas par accident depuis une
branche quelconque. Et l'approbation humaine requise a été donnée.

Aucune ne suffit seule. Des preuves vertes sans protection d'environnement permettent de déployer
depuis n'importe où ; une approbation sans preuves ne fait que déplacer la responsabilité.

**L'approbation humaine a un rôle précis.** Elle ne relit pas le code — c'est le travail de
`quality-review-diffs-001`. Elle décide du **moment** : le trafic, la disponibilité de l'équipe, la
coïncidence avec un autre changement. Une approbation qui se contente de valider ce que la machine a
déjà validé n'ajoute qu'un délai.

**Le retour arrière se prépare avant.** Trois questions à répondre avant de déployer : par quoi
remplace-t-on la version en cours, en combien de temps, et que fait-on des données déjà écrites par la
nouvelle version. La troisième est celle qu'on oublie, et c'est celle qui rend un retour arrière
impossible.

**Une migration de schéma limite le retour arrière.** Ajouter une colonne facultative est réversible ;
supprimer une colonne ou changer un type ne l'est pas sans perte. La pratique qui rend les deux
compatibles consiste à découpler : d'abord un changement de schéma compatible avec l'ancien code, puis
le déploiement applicatif, puis seulement le nettoyage du schéma. Trois livraisons au lieu d'une, et
un retour arrière possible à chaque étape.

**Un déploiement progressif limite l'exposition.** Basculer une petite fraction du trafic d'abord,
observer, puis élargir. L'intérêt n'est pas d'éviter le défaut : c'est de le rencontrer sur une
fraction des utilisateurs, avec des signaux qui permettent de décider. Cela suppose des indicateurs
en place — c'est ce que `observability-alerts-costs-001` rend possible.

**Découpler le déploiement de l'activation.** Livrer du code inactif, puis l'activer par un indicateur,
sépare deux risques : celui de la mise en production et celui du changement fonctionnel. Désactiver un
indicateur est instantané et ne demande aucun déploiement — c'est souvent le retour arrière le plus
rapide.

Le coût est réel : chaque indicateur ajoute un chemin à tester et doit être retiré une fois la décision
prise. Un indicateur oublié devient une branche morte que personne n'ose supprimer.

**Ce qui doit être vérifié après.** Un déploiement se termine par un contrôle : le service répond, les
indicateurs essentiels sont normaux, aucun signal d'erreur ne monte. Sans cette vérification, le
déploiement est déclaré réussi par le seul fait qu'aucune commande n'a échoué.

## Exemple commenté

La porte, exprimée comme une conjonction stricte :

```csharp
public static bool CanDeploy(bool checksPassed, bool environmentProtected, bool approvalGranted)
{
    // Les trois ensemble. Preuves vertes sans protection : on déploie depuis
    // n'importe quelle branche. Approbation sans preuves : on ne fait que
    // déplacer la responsabilité d'un défaut que personne n'a cherché.
    return checksPassed && environmentProtected && approvalGranted;
}
```

Le découplage d'une migration destructrice en trois livraisons réversibles :

```text
Livraison 1 : ajouter la colonne « email_normalise », facultative, alimentée en écriture
              par l'ancien et le nouveau code. Retour arrière : la colonne reste, inutilisée.

Livraison 2 : déployer le code qui lit « email_normalise ». Retour arrière : revenir au
              code précédent, qui lit encore l'ancienne colonne, toujours présente.

Livraison 3 : supprimer l'ancienne colonne, une fois la livraison 2 stable et observée.
              Cette étape seule n'est pas réversible : elle attend une preuve, pas un délai.
```

Et la vérification qui clôt un déploiement :

```text
# Le déploiement n'est pas « réussi » parce qu'aucune commande n'a échoué.
# Il l'est quand le service répond et que les indicateurs sont normaux.

1. Contrôle de santé sur l'instance déployée      -> réponse 200 attendue
2. Version exposée par le service                 -> doit être 1.4.8, pas la précédente
3. Taux d'erreur sur 5 minutes                    -> comparé au niveau d'avant déploiement
4. Latence au 95e centile                         -> comparée au budget annoncé

Si l'un des quatre s'écarte : retour arrière immédiat, analyse ensuite.
```

## Contre-exemple et erreur fréquente

```text
- name: Déployer en production
  # Aucune condition : tout ce qui atteint la branche principale part en production.
  if: always()
  run: |
    # Migration destructrice appliquée en même temps que le code.
    dotnet ef database update            # supprime deux colonnes
    ./deploy.sh production --force

    # Aucune vérification après : le déploiement est « réussi »
    # parce que le script s'est terminé sans code d'erreur.
```

Et la procédure de retour arrière, telle qu'elle est documentée :

```text
En cas de problème : redéployer la version précédente.
```

Cinq défauts.

`if: always()` déclenche le déploiement même si les étapes précédentes ont échoué. La porte n'existe
pas : la chaîne se contente d'afficher du rouge avant de livrer.

La migration destructrice est appliquée avec le code. Redéployer la version précédente ne restaurera
pas les colonnes supprimées : le retour arrière annoncé est impossible, et personne ne s'en apercevra
avant d'en avoir besoin.

`--force` supprime les protections que le mécanisme de déploiement offrait, y compris celles qui
auraient refusé une cible incorrecte.

L'absence de vérification après déploiement laisse un service cassé être déclaré sain. Le premier
signal viendra d'un utilisateur.

Enfin, « redéployer la version précédente » n'est pas une procédure : elle ne dit ni en combien de
temps, ni qui décide, ni ce qu'on fait des données écrites par la nouvelle version.

## Vérification de compréhension

Vous devez renommer une colonne utilisée par un service en production. Décrivez le découpage en
livraisons, ce qui est réversible à chaque étape, et à quel moment le retour arrière devient
impossible.

:::quiz
id=ci-deployment-gates-001-check
question=Pourquoi une migration destructrice appliquée en même temps que le code rend-elle le retour arrière illusoire ?
option=Parce que le mécanisme de déploiement refuse de revenir en arrière après une migration
option=Parce que redéployer la version précédente ne restaure pas les données supprimées : le code revient, l'état du schéma non
option=Parce que la migration doit toujours être exécutée après le déploiement du code
correct=1
success=Correct : d'où le découplage en trois livraisons, où chaque étape reste réversible et où seule la dernière, isolée, ne l'est pas.
retry=Relisez le passage sur les migrations, et demandez-vous ce que retrouve le code ancien après une suppression de colonne.
:::

## Exercice guidé

Ouvrez `ci-deploy-gate-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que laisse passer chaque combinaison où une seule condition manque.
2. Implémentez la porte comme une conjonction stricte, sans exception.
3. Vérifiez les trois cas à une condition manquante, puis le cas complet.
4. Ouvrez ensuite `content/labs/ci-delivery/` et relevez les conditions réellement posées.

## Exercice autonome

Concevez la procédure de déploiement d'un service disposant d'une base et d'une interface.

Décidez avant d'écrire : les conditions de la porte, qui approuve et sur quel critère, le découpage
d'une migration destructrice, la stratégie d'exposition progressive, les contrôles exécutés après
déploiement et leurs seuils, la procédure de retour arrière avec son délai cible, et le sort des
données écrites par la version fautive.

## Débogage

Un ticket indique : « Le retour arrière a été lancé mais le service reste cassé. »

1. **Symptôme** : le code précédent est en place et le défaut persiste.
2. **Hypothèse** : un changement non réversible accompagne le déploiement — schéma, format de message,
   contenu de cache.
3. **Preuve** : comparez l'état du schéma et des données au moment du déploiement et maintenant.
4. **Prévention** : découpler les migrations en étapes compatibles, et vérifier le retour arrière lors
   d'une répétition avant d'en avoir besoin.

## Entretien

Question posée à voix haute : *comment déployez-vous, et que faites-vous si ça se passe mal ?*

Une réponse solide énonce les conditions de la porte, distingue le rôle de l'approbation humaine de
celui de la revue, décrit le découplage des migrations, donne un délai de retour arrière, et sait dire
qu'un déploiement se termine par une vérification.

## Résumé

- Preuves vertes, environnement protégé, approbation : les trois ensemble.
- L'approbation humaine décide du moment, pas de la qualité du code.
- Le retour arrière se prépare avant, données comprises.
- Une migration destructrice se découpe en trois livraisons réversibles.
- Un déploiement se termine par une vérification, pas par l'absence d'erreur.

## Cartes de révision

Question : quel est souvent le retour arrière le plus rapide ? Réponse attendue : désactiver un
indicateur, qui ne demande aucun déploiement.

Question : que signale une porte qui n'a jamais bloqué personne ? Réponse attendue : qu'elle ne vérifie
probablement rien.

## Test de maîtrise

Sans relire, décrivez la procédure de déploiement complète d'un service : conditions de la porte, rôle
de l'approbation, découpage d'une migration destructrice, exposition progressive et ses seuils,
contrôles après déploiement, procédure et délai de retour arrière, traitement des données écrites par
la version fautive, et gestion du cycle de vie des indicateurs d'activation.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
