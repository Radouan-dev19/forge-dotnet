# Explication

Le contrat de sortie reçoit un libellé normalisé sans exposer l'objet de persistance.

Ce qui traverse la frontière publique est décidé champ par champ. Retourner l'entité entière publierait tout ce qu'elle porte, y compris ce qu'on ajoutera demain ; produire un libellé explicite garde le contrôle et rend la réponse stable même si le stockage change.

Le repli mérite une attention particulière : il est visible par l'appelant, donc il ne doit ni divulguer un état interne, ni ressembler à une donnée réelle. Trois entrées se ramènent à lui — absente, vide, composée de blancs — ce qui évite au client d'avoir à distinguer trois formes d'absence. Le coût est linéaire dans la longueur du nom.
