# Recommander un hébergement à partir du profil d'une charge

Implémentez `Submission.HostingRecommendation` avec la signature fournie. Le choix d'un hébergement
géré ne se fait ni par mode ni par habitude : il se déduit du profil de la charge. Votre fonction lit
ce profil et rend la recommandation motivée.

## Le format du profil

Le profil arrive en trois paires clé-valeur ; le séparateur est le point-virgule et l'ordre est
libre :

- `artifact` — ce que l'équipe livre : `code` (l'artefact applicatif, la plateforme fournit
  l'exécution) ou `container` (une image déjà construite) ;
- `scale` — le rythme de la charge : `steady` (continu), `bursty` (continu avec pointes) ou
  `event-driven` (réveillée par des événements, endormie sinon) ;
- `delivery` — le mode de livraison : `single-revision` (une version active à la fois) ou
  `multi-revision` (plusieurs versions actives avec répartition de trafic).

## La recommandation

Rendez `hébergement|raison`, selon ces règles :

1. `scale=event-driven` tranche d'abord : `functions|per-event-billing` pour du code — la
   facturation à l'événement épouse une charge qui dort — et `container-apps|scale-to-zero` pour un
   conteneur, qui se met à zéro entre deux réveils ;
2. pour les rythmes continus (`steady` ou `bursty`), le croisement artefact-livraison décide :
   `container` + `multi-revision` → `container-apps|revision-traffic` ; `container` +
   `single-revision` → `app-service|single-container` ; `code` + `multi-revision` →
   `app-service|deployment-slots` ; `code` + `single-revision` → `app-service|managed-runtime`.

```text
HostingRecommendation("artifact=container;scale=steady;delivery=multi-revision")
  →  "container-apps|revision-traffic"
HostingRecommendation("artifact=code;scale=event-driven;delivery=single-revision")
  →  "functions|per-event-billing"
```

## Les refus

`ArgumentException` pour toute description défaillante — paire illisible, clé en double, attribut
oublié ou valeur inconnue du vocabulaire.

## Avant d'écrire

Prédisez la recommandation d'un conteneur événementiel livré en multi-révisions, et dites quelle
règle a tranché avant l'autre. Puis nommez ce qu'une capacité de révisions « prévue pour plus tard »
coûte dès aujourd'hui si elle dicte l'hébergement d'une charge à version unique.
