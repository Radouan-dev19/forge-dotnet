# Cartes de révision

- **Pourquoi un consommateur ne peut-il pas se contenter de sauter tout identifiant connu ?** Parce
  qu'il avalerait l'échec antérieur — perdu à jamais — et le contenu recyclé sous un identifiant
  connu : le statut et l'empreinte du registre séparent ces deux familles.
- **Pourquoi la charge d'un message rejoué se vérifie-t-elle avant le statut ?** Parce qu'un
  identifiant recyclé après un échec serait sinon « retenté » avec un contenu divergent : la
  divergence se rejette bruyamment, aucun statut ne la rachète.
