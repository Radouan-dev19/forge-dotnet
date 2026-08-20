# Fusionner des quantités de stock

Implémente la méthode publique statique `MergeStock` de `Submission`. Elle reçoit deux dictionnaires associant une chaîne à un entier et retourne un nouveau dictionnaire du même type. La signature exacte se trouve dans le starter.

Fusionne les quantités sans modifier les entrées. Les références sont comparées sans tenir compte de la casse et les clés retournées sont en minuscules invariantes. Une clé présente dans les deux sources reçoit la somme. `null` provoque `ArgumentNullException` ; toute quantité négative provoque `ArgumentOutOfRangeException`.

Les clés des entrées sont non vides. Décris comment tu empêches une validation tardive de produire un résultat partiel.
