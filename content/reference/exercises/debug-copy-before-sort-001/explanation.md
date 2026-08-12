# Explication

Trier uniquement une copie afin que l'observation et l'appelant conservent l'état initial.

Le défaut visé est propre au débogage : trier en place pour inspecter une valeur détruit l'ordre d'origine, donc l'information que l'on cherchait. Au pas suivant, la fenêtre d'inspection montre un état qui n'a jamais existé dans le programme, et le diagnostic part dans une mauvaise direction.

Le chemin où le tableau est déjà trié est le plus dangereux : retourner la référence reçue paraît gratuit et rompt la garantie pour tout appelant qui modifiera ensuite le résultat. La copie est allouée dans tous les cas. Le tri domine le temps, et l'espace correspond à la copie.
