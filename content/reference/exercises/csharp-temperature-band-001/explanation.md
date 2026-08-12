# Explication

Traiter les bornes zéro et vingt comme des décisions explicites.

Trois bandes, deux frontières. Les comparaisons sont strictes, donc zéro n'appartient pas au gel et vingt n'appartient pas au frais : les valeurs de frontière tombent dans la bande supérieure. Ce choix doit être décidé une fois et testé, sinon il est pris par accident au moment d'écrire la condition.

L'ordre des branches porte du sens : chacune s'appuie sur l'échec des précédentes, ce qui permet à la dernière de ne rien tester. Écrire les mêmes conditions dans l'ordre inverse rend une branche inatteignable sans qu'aucun avertissement ne le signale. La décision est en temps constant.
