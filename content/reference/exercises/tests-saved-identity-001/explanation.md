# Explication

Une intégration réussie produit un identifiant strictement positif observable.

L'identifiant attribué par la base est le signe le plus simple qu'une écriture a réellement eu lieu : il ne peut pas être produit par le code applicatif seul. La valeur nulle est celle d'un objet non encore persisté, donc l'accepter reviendrait à déclarer réussie une écriture qui n'a pas eu lieu.

L'absence d'exception ne prouve rien : une opération peut être différée, mise en tampon, ou annulée par une transaction englobante. C'est aussi pourquoi une relecture doit passer par un contexte neuf — lire depuis celui qui a écrit peut retourner l'objet encore suivi en mémoire, et le test passerait sans que rien n'ait atteint la base. La décision est en temps constant.
