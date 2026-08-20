# Refuser une étiquette mouvante en production

Implémentez `Submission.ResolveTag` avec la signature fournie.

Une étiquette d'image peut être **déplacée** : celle qui désignait une image ce matin peut en
désigner une autre ce soir. C'est commode en développement, et c'est une faute en production — deux
serveurs qui démarrent la même référence à une heure d'écart exécutent alors deux codes différents,
et rien dans les journaux ne le dit.

## Le contrat

```csharp
public static string ResolveTag(string reference, bool isProduction)
```

L'étiquette est ce qui suit le **dernier** deux-points de la référence. Chercher le premier serait
une erreur : un registre privé s'écrit souvent avec un port, `registry.local:5000/app:1.4.2`.

Une référence **sans deux-points** ne porte pas d'étiquette explicite ; elle porte implicitement
l'étiquette mouvante `latest`. Le piège est là : `app` et `app:latest` désignent la même chose, et
une implémentation qui n'examine que les références étiquetées laisse passer la première.

## La règle

En production, une étiquette mouvante lève `ArgumentException`. Partout ailleurs, la référence est
rendue telle quelle, détourée de ses blancs.

La comparaison de l'étiquette ignore la casse : `LATEST` déplace tout autant.

Une référence vide ou faite de blancs lève `ArgumentException`, quel que soit l'environnement.

## Avant d'écrire

Prédisez quatre cas : une version explicite en production, une référence sans étiquette en
production, la même en développement, un registre avec port suivi d'une version. Nommez ce qu'on
perd quand deux serveurs exécutent deux images sous la même référence.
