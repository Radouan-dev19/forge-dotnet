# Cartes de révision

- **Quel critère sépare une assertion robuste d'une assertion fragile au remaniement ?**
  L'accessibilité de ce qu'elle observe : valeur rendue, exception promise ou état relisible
  survivent ; appels internes, ordre, champs privés et durée cassent avec la mécanique.
- **Que faire d'une suite dont la plupart des assertions épient la mécanique ?** La réécrire avant le
  remaniement : elle ne protège pas le comportement, elle cimente l'implémentation qu'on veut
  justement changer.
