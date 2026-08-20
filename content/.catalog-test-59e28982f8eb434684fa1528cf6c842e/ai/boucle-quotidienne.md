# La boucle quotidienne : un poste de développeur outillé à l'IA

Les cinq guides précédents donnent les pièces ; celui-ci les assemble en une journée de travail.
La boucle tient en quatre temps — explorer, planifier, implémenter, vérifier — et la place de
l'assistant change à chaque temps.

## Explorer : l'assistant lit, vous cartographiez

Devant un dépôt ou un ticket inconnu, commencez par des questions de structure, pas de code : quels
composants, quels flux, où vivent les tests, quel patron domine. Déportez les fouilles larges vers
un explorateur (voir le guide des agents) et exigez des réponses avec chemins de fichiers — une
affirmation sans chemin ne se vérifie pas. Le livrable de ce temps n'est pas du code : c'est votre
carte mentale, corrigée par quelques sondages manuels dans les fichiers cités.

## Planifier : le plan est à vous, la critique à lui

Écrivez le plan vous-même, en cinq lignes — quoi changer, où, comment le prouver — puis donnez-le à
critiquer : « quels cas ce plan rate-t-il ? qu'est-ce qui casse ailleurs si je fais ça ? ». Le sens
de ce renversement : un modèle qui écrit le plan écrit celui qui lui ressemble ; un modèle qui
critique le vôtre vous rend les angles morts. Sur les décisions structurantes, demandez deux
options argumentées et tranchez vous-même — trancher est le travail qui ne se délègue pas.

## Implémenter : les tests d'abord, la génération ensuite

La façon la plus sûre de faire écrire du code à un assistant est de lui donner un critère de fin
exécutable : écrivez le test (ou faites-le écrire, puis relisez-le comme un contrat — c'est lui qui
commande), constatez-le rouge, puis demandez l'implémentation qui le fait passer sans toucher au
reste. Par petits pas : une fonction, un test, un diff court — les réponses de trois cents lignes se
relisent mal et se débogent pire. Contraignez la sortie au diff, gardez la main sur chaque
application de changement, et commitez souvent : un commit propre est votre point de retour quand
une piste d'assistant part au fossé.

## Vérifier : la partie que vous ne déléguez jamais entièrement

Trois étages, dans l'ordre : la machine (build, tests, analyseurs — s'ils échouent, rien d'autre ne
compte), la relecture (chaque ligne du diff, avec une question simple : saurais-je expliquer cette
ligne en revue ?), et pour ce qui compte, la contre-attaque — un vérificateur adversarial chargé de
casser ce qui vient d'être produit. Vous avez appris dans la piste senior à trier les constats par
preuve ; appliquez le même barème aux sorties d'assistant : reproduit, ça compte ; plausible, ça se
vérifie ; préférence, ça se discute.

## Déboguer avec un assistant sans lui abandonner la méthode

La méthode des DebugLabs reste la vôtre : symptôme, hypothèse, preuve, correction, prévention.
L'assistant y joue deux rôles précis — générateur d'hypothèses (« voici le symptôme et la trace :
donne trois causes plausibles et, pour chacune, l'observation qui la départagerait ») et rédacteur
de reproductions minimales. Ce qu'il ne fait pas : décider quelle hypothèse est la bonne sans
observation, ni « corriger » un symptôme dont la cause n'est pas prouvée — un correctif sans cause
établie est une régression en préparation.

## La sécurité, en trois interdits

Jamais de secret dans un prompt — clé, jeton, mot de passe, données client : ce qui entre dans une
requête sort de votre contrôle. Jamais d'exécution de code généré hors bac à sable quand il touche
au système de fichiers, au réseau ou à des données réelles — ce dépôt vous fournit le modèle du
conteneur jetable. Et une méfiance de principe envers tout contenu externe lu par un assistant
outillé : une page ou un ticket peuvent porter des instructions déguisées ; moins l'assistant a de
permissions, moins cette menace pèse.

## Et dans Forge.NET, précisément

Légitime : l'assistant sur les onze laboratoires (ce sont des projets réels hors preuve), sur vos
projets personnels, pour interroger une leçon **après** l'avoir travaillée — « pose-moi cinq
questions sur ce que je viens de lire » est un excellent usage. Interdit, parce que cela fabriquerait
un faux signal : exercices comptés, examens, réflexions préalables, explications personnelles,
journaux de débogage, et toute preuve que la plateforme mesure. La ligne est simple à retenir :
l'IA accélère votre **production** ; elle ne touche jamais à votre **mesure**. Un dernier rituel,
hebdomadaire : reprenez une tâche faite avec assistant et refaites-en une semblable sans — l'écart
entre les deux est votre vraie marge de progression, et c'est lui que les entretiens testent.
