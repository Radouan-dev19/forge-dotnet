# Calculer un ETag fort et stable

Implémentez `Submission.ComputeETag(string representation)`.

L'ETag est l'empreinte d'une représentation : deux représentations identiques doivent produire le
même ETag, deux différentes un ETag différent. La méthode calcule un ETag *fort*.

Règles exactes :

- l'empreinte est le condensat SHA-256 des octets UTF-8 de la représentation reçue ;
- le condensat est mis en forme en hexadécimal *minuscule* ;
- l'ETag fort est ce texte encadré de guillemets doubles — par exemple `"a1b2..."` — sans le
  préfixe de faiblesse que porterait un ETag faible ;
- la représentation est prise telle quelle : la forme canonique est de la responsabilité de
  l'appelant, la méthode se contente de condenser fidèlement les octets reçus.

La chaîne vide a un ETag valide, celui du condensat d'une entrée vide. Écrivez avant le code :
deux représentations identiques qui doivent coïncider, et une qui diffère d'un caractère.

Exemple : entrée `["{\"id\":42,\"status\":\"paid\"}"]`, sortie `"\"<empreinte hexadécimale>\""`.
