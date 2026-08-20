# Relever les écarts de durcissement d'une configuration de conteneur

Implémentez `Submission.HardeningGaps` avec la signature fournie. Savoir qu'une configuration
« n'est pas durcie » ne suffit à personne : l'équipe a besoin du relevé des écarts, chacun avec sa
gravité, pour décider quoi corriger ce soir et quoi planifier. Votre fonction audite une
configuration décrite et produit ce relevé.

## Le format de la configuration

Cinq réglages en paires clé-valeur séparées par des points-virgules, dans un ordre quelconque :

- `user` — `app` (identité dédiée) ou `root` ;
- `escalation` — `blocked` (élévation de privilèges interdite) ou `allowed` ;
- `network` — `bridge` (réseau isolé) ou `host` (pile réseau de l'hôte) ;
- `capabilities` — `dropped` (capacités abandonnées) ou `default` (jeu par défaut) ;
- `filesystem` — `read-only` ou `writable`.

## Le référentiel des écarts

Chaque réglage non durci produit un écart `nom=gravité`, dans cet ordre fixe — gravité décroissante,
puis ordre du référentiel :

| Écart | Gravité |
|---|---|
| `root-user` | `critical` |
| `privilege-escalation` | `critical` |
| `host-network` | `high` |
| `default-capabilities` | `high` |
| `writable-filesystem` | `medium` |

Rendez les écarts joints par des points-virgules ; une configuration entièrement durcie rend
`compliant`.

```text
HardeningGaps("user=app;filesystem=writable;escalation=allowed;capabilities=dropped;network=bridge")
  →  "privilege-escalation=critical;writable-filesystem=medium"
```

## Les refus

`ArgumentException` pour une paire illisible, un réglage répété, un réglage manquant ou une valeur
hors vocabulaire. Un réglage absent n'est jamais « durci par défaut » : ce que l'audit n'a pas vu,
il ne le certifie pas.

## Avant d'écrire

Prédisez le relevé de la configuration entièrement laxiste, dans l'ordre exact. Puis dites pourquoi
l'identité racine et l'élévation autorisée partagent la gravité maximale alors que le système de
fichiers inscriptible n'a que la gravité moyenne — qu'est-ce que chacun donne à un attaquant qui a
déjà un pied dans le conteneur ?
