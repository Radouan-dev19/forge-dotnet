# Explication

Valider les durées puis retenir le budget le plus contraignant.

Un budget de temps se compose toujours par le minimum : celui du client et celui du serveur s'appliquent tous les deux, et le premier atteint arrête l'opération. Retenir le maximum reviendrait à laisser un appelant desserrer une limite que le serveur avait posée, ce qui vide la protection de son sens.

Une durée nulle ou négative ne décrit aucun budget : c'est une faute d'appelant, et l'absorber en repli masquerait une configuration fautive. Le refus explicite fait apparaître l'erreur là où elle a été commise. La décision est en temps constant.
