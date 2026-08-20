# Cartes de révision

- **Quelle instruction d'un fichier de construction peut être invalidée par un fichier modifié ?**
  Uniquement une copie dont la portée contient le chemin : c'est le seul canal par lequel le contenu
  du dépôt entre dans l'empreinte d'une couche.
- **Que se reconstruit-il une fois une couche invalidée ?** La couche invalidée et toutes celles qui
  la suivent dans l'ordre du fichier, même celles qui n'ont rien vu du changement : la dérivation est
  en cascade.
