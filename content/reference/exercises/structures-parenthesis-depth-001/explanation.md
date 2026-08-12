# Explication

Retourner le code d'erreur pour toute structure déséquilibrée, sinon la profondeur maximale.

Deux informations sortent du même parcours, et l'une conditionne l'autre : la profondeur maximale n'a de sens que si la structure est équilibrée. Une chaîne qui n'a que des ouvertures atteint une profondeur élevée et ne vaut rien — d'où la vérification finale, en plus du refus immédiat sur une fermeture excédentaire.

Le maximum se relève au moment de l'ouverture, pas après : la profondeur atteinte est celle qui suit l'incrément. Une chaîne vide a une profondeur nulle et une structure équilibrée, ce qui est une réponse valide, distincte du code d'erreur. Le parcours est linéaire et deux compteurs suffisent.
