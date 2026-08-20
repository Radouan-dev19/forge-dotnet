# Choisir entre rebase et fusion

Implémentez `Submission.IntegrationStrategy` avec la signature fournie.

Quatre façons d'intégrer une branche, et une seule est correcte selon la situation. La question n'est
pas de préférence : elle est de savoir ce qu'on a le **droit** de réécrire.

## Le contrat

```csharp
public static string IntegrationStrategy(bool branchIsShared, bool historyHasNoise, bool targetMovedOn)
```

| Situation | Stratégie rendue |
|---|---|
| la branche est partagée | `merge` |
| sinon, son histoire est bruitée | `squash` |
| sinon, la cible a avancé | `rebase` |
| sinon | `fast-forward` |

**L'ordre est la règle**, pas une commodité d'écriture. Le partage se teste en premier parce qu'il
**interdit** toute réécriture : une branche que quelqu'un a déjà récupérée ne se rebase ni ne
s'écrase, sinon les commits d'origine survivent chez lui en doublons et la prochaine synchronisation
produit un conflit que personne n'a introduit.

Une histoire « bruitée » est celle des commits de travail — `wip`, `fix typo`, `retry` — qui ne
racontent rien à qui relira. L'écrasement en un commit garde le résultat et jette le cheminement.

Une cible qui a avancé signifie que des commits sont arrivés sur la branche d'intégration depuis le
départ de la vôtre : le rebasage rejoue vos commits par-dessus, l'histoire reste linéaire.

Quand rien de tout cela ne s'applique, l'avance rapide suffit : aucun commit de fusion n'est
nécessaire.

## Avant d'écrire

Prédisez quatre cas : une branche partagée et bruitée, une branche privée et bruitée, une branche
privée propre avec cible avancée, une branche privée propre sur cible immobile. Nommez ce qui se
passe chez un collègue dont vous avez rebasé la branche.
