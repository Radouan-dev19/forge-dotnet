# Détecter la première régression dans les lectures d'un client

Implémentez `Submission.FirstRegression` avec la signature fournie. Sur un système répliqué, un
client peut lire une donnée à la version 7, puis la relire à la version 6 : sa seconde lecture a été
servie par une réplique en retard. Rien n'est perdu — la donnée finira par converger — mais le client
a vu le temps reculer, et c'est précisément la garantie dite des **lectures monotones** qui vient de
casser. Votre fonction détecte cette cassure dans le journal des lectures d'un client.

## Le format du journal

Les versions lues, séparées par des points-virgules, dans l'ordre des lectures du client :
`"5;7;7;6"`.

## Ce qu'il faut produire

L'indice — à partir de zéro — de la **première lecture qui recule** strictement par rapport à la
précédente ; `-1` si le journal est monotone. Deux lectures égales consécutives ne reculent pas : la
donnée n'a simplement pas bougé entre les deux.

```text
FirstRegression("5;7;7;6")  →  3
FirstRegression("1;2;3")    →  -1
FirstRegression("4;4;4")    →  -1
```

L'indice rendu désigne la lecture fautive — celle que la réplique en retard a servie — parce que
c'est elle que l'enquête va corréler : quelle réplique, quel retard de réplication à cet instant. La
comparaison se fait de voisine à voisine, pas contre le maximum global : la question des lectures
monotones est « ai-je reculé depuis ma dernière lecture », pas « suis-je revenu sous mon record ».

## Les refus

`ArgumentException` pour un journal vide ou blanc, une version illisible ou négative — les versions
sont des compteurs, elles ne remontent pas le temps par elles-mêmes.

## Avant d'écrire

Prédisez l'indice pour un journal qui recule deux fois, et dites pourquoi la première cassure suffit
au rapport. Puis nommez les deux parades classiques côté système : coller le client à sa réplique, ou
transmettre la version lue pour exiger au moins elle.
