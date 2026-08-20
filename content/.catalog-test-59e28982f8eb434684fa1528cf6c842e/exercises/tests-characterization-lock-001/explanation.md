# Explication

Remanier un code sans test, c'est changer un système dont personne ne connaît plus le contrat. La
tentation naturelle est d'écrire d'abord « les bons tests » — ceux de la règle documentée — puis de
remanier. Cette démarche échoue sur les codes hérités pour une raison précise : la documentation et le
code ont divergé depuis des années, et c'est le code qui a raison, au sens où c'est lui que les
clients, les factures et les systèmes en aval ont intégré. La caractérisation renverse la démarche :
on photographie d'abord le comportement réel, on décide ensuite, séparément, ce qu'on veut en changer.

**Pourquoi figer la bizarrerie au lieu de la corriger.** Le seuil strict — gratuit au-delà de cent,
payant à cent tout rond — est manifestement un écart entre l'intention et le code. Mais le corriger
pendant la caractérisation mélangerait deux opérations qui doivent rester distinctes : établir le
référentiel, et faire évoluer le comportement. Si le référentiel « corrige » silencieusement, le
remaniement qui suit ne peut plus distinguer ses propres régressions des écarts volontaires du
référentiel ; et le jour où l'équipe décide vraiment de corriger le seuil, elle découvre que la
correction est déjà à moitié faite, sans trace, sans annonce aux clients, sans mise à jour des
factures prévisionnelles. Une bizarrerie figée et nommée est un choix réversible ; une bizarrerie
corrigée en douce est une dette de plus.

**Pourquoi les refus font partie du comportement.** Le système historique refuse un panier sans
article et un sous-total négatif, avec un type d'exception précis. Ces refus sont du contrat au même
titre que les montants : un appelant peut parfaitement s'appuyer sur le type d'exception pour router
son traitement d'erreur. Remplacer un refus par une valeur de repli — zéro, par exemple — changerait
le comportement observable tout en faisant passer tous les tests de montants, ce qui est la définition
d'une régression discrète.

**Pourquoi l'échelle des montants compte.** La facturation affiche deux décimales, et les systèmes en
aval comparent parfois des chaînes. Un montant de `5.4` est mathématiquement égal à `5.40` et
opérationnellement différent : la caractérisation reproduit l'échelle observée, parce que « ce que le
système fait » inclut la forme sous laquelle il le fait. C'est le genre de détail qui ne coûte rien à
préserver et des heures à déboguer une fois perdu.

**La composition des frais illustre le piège de l'excédent.** Le supplément s'applique aux articles
au-delà du cinquième, pas à tous : six articles coûtent un supplément, pas six. Borner l'excédent à
zéro évite qu'un petit panier reçoive une remise que personne n'a jamais observée.

La transposition professionnelle : devant tout code sans test qu'il faut modifier, la première
livraison n'est pas la modification — c'est le relevé exécutable de ce que le code fait aujourd'hui.
