# Explication

Un jalon est révisable uniquement si tests, revue de sécurité et retour arrière documenté sont tous présents.

Les trois conditions couvrent trois familles de défauts distinctes. Les tests attrapent les régressions fonctionnelles ; la revue de sécurité attrape ce qu'aucun test fonctionnel ne cherche — une autorisation manquante, une entrée non bornée, une donnée exposée ; et le retour arrière documenté traite ce qui échappe aux deux, c'est-à-dire l'imprévu.

La troisième est la moins spectaculaire et la plus souvent absente. Un jalon sans plan de repli n'est pas déployable sereinement : la question n'est pas d'éviter tout incident, mais de savoir en combien de temps on revient en arrière. La décision est en temps constant.
