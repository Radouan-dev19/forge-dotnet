# Explication

Les bornes temporelles d'un jeton semblent être le contrôle le plus simple de la chaîne de
validation : deux comparaisons d'entiers. L'exercice existe parce que ces deux comparaisons
concentrent trois décisions que l'on rate facilement, et dont chacune a une conséquence
opérationnelle réelle.

La première décision est le statut de chaque borne. L'expiration est obligatoire : un jeton sans
`exp` est un jeton qui ne meurt jamais, et la leçon a montré pourquoi c'est inacceptable — un jeton
autoporté ne se révoque pas, sa durée de vie courte est le seul mécanisme qui limite les dégâts
d'un vol. Accepter l'absence d'expiration par défaut, comme le ferait un `TryGetProperty` dont on
ignore l'échec, transformerait la fuite d'un seul jeton en accès permanent. La prise d'effet, elle,
est facultative : la plupart des émetteurs ne la posent pas, et son absence signifie simplement
« valable dès l'émission ». Cette asymétrie — l'une refuse quand elle manque, l'autre laisse
passer — est le cœur du sujet, et c'est elle que les cas cachés éprouvent.

La deuxième décision est le sens de la tolérance. Elle existe pour absorber la dérive d'horloge
entre l'émetteur et le vérificateur, donc elle élargit toujours la fenêtre : l'expiration est
repoussée de la tolérance, la prise d'effet avancée d'autant. L'erreur classique consiste à
soustraire là où il faut additionner, ce qui rétrécit la fenêtre — les jetons frais sont rejetés,
les symptômes sont intermittents et localisés aux machines dont l'horloge dérive, exactement le
genre d'incident que le ticket de débogage de la leçon décrivait. Quant aux bornes elles-mêmes,
elles sont strictes du côté de l'expiration : à l'instant exact `exp + tolérance`, le jeton est
déjà refusé. Une inclusion large d'une seconde paraît anodine ; elle devient une différence de
comportement entre votre implémentation et celle d'en face, donc un bug de bord impossible à
reproduire.

La troisième décision est arithmétique. Les instants d'époque Unix tiennent aujourd'hui dans un
entier de 32 bits, mais leur somme avec une tolérance peut le déborder — et un débordement en C#
ne lève pas par défaut, il enroule. Le résultat serait une expiration devenue négative, donc un
jeton toujours périmé, ou l'inverse. Faire la comparaison en 64 bits coûte une conversion et
supprime la classe de bug entière ; c'est le réflexe à prendre chaque fois que l'on additionne des
valeurs dont on ne contrôle pas la plage.

Reste le régime d'erreurs, le même que pour tout vérificateur de jeton : l'illisible se refuse en
silence. Une exception sur un jeton malformé serait un canal d'erreur serveur offert à n'importe
quel client ; un refus est une réponse, pas un incident.
