# Vue.js 3 et TypeScript : lire et écrire un composant

Vue est un framework front-end qui organise une page en composants réutilisables, chacun
responsable d'un fragment d'interface et de son propre état. Depuis la version 3, l'API de
composition (Composition API) est la façon recommandée d'écrire un composant, et elle s'associe
naturellement à TypeScript : chaque variable réactive, chaque prop et chaque événement porte un
type explicite, vérifié avant même d'ouvrir le navigateur.

## Un composant, trois blocs dans un seul fichier

Un composant Vue vit dans un fichier `.vue` qui regroupe trois blocs : `<template>` pour le
balisage, `<script setup lang="ts">` pour la logique typée, `<style>` pour le style local au
composant. L'attribut `setup` évite d'écrire un objet `export default { ... }` : tout ce qui est
déclaré au niveau supérieur du bloc `<script setup>` est automatiquement exposé au template.

```vue
<script setup lang="ts">
import { ref } from 'vue'

const compteur = ref(0)
</script>

<template>
  <button @click="compteur++">Cliqué {{ compteur }} fois</button>
</template>
```

## Réactivité : `ref` pour une valeur, `reactive` pour un objet

`ref` enveloppe une valeur primitive (nombre, chaîne, booléen) dans un objet réactif : on lit et
on écrit sa valeur via `.value` dans le script, mais le template la déballe automatiquement.
`reactive` fait la même chose pour un objet entier, sans `.value` : ses propriétés sont
directement réactives.

```ts
import { reactive, ref } from 'vue'

const utilisateur = reactive({ nom: 'Ada', connecte: false })
const chargement = ref(false)

utilisateur.connecte = true   // déclenche une mise à jour du template
chargement.value = true       // idem, via .value
```

Le piège classique : **déstructurer un objet `reactive` casse la réactivité**, car cela copie la
valeur du moment plutôt que de garder le lien vers l'objet réactif.

```ts
const { nom } = utilisateur   // `nom` est figé, il ne suivra plus les changements
```

Pour déstructurer sans perdre la réactivité, Vue fournit `toRefs`, qui transforme chaque
propriété d'un objet réactif en son propre `ref` lié à l'original.

## Calculs dérivés : `computed`

`computed` déclare une valeur calculée à partir d'autres valeurs réactives, mise en cache et
recalculée seulement quand une de ses dépendances change — contrairement à une simple fonction,
rappelée à chaque rendu.

```ts
import { computed, ref } from 'vue'

const prixHT = ref(100)
const tauxTva = ref(0.2)
const prixTTC = computed(() => prixHT.value * (1 + tauxTva.value))
```

## Props et emits typés

Un composant reçoit des données par ses **props** et communique vers son parent par des
**emits**. Typer les deux avec une interface donne une vérification à la compilation : passer
une prop du mauvais type, ou émettre un événement avec le mauvais payload, devient une erreur
visible avant l'exécution.

```vue
<script setup lang="ts">
const props = defineProps<{ titre: string; quantite: number }>()
const emit = defineEmits<{ (evenement: 'quantite-changee', valeur: number): void }>()

function incrementer() {
  emit('quantite-changee', props.quantite + 1)
}
</script>
```

## Composables : réutiliser une logique entre composants

Un **composable** est une fonction qui commence par `use` et qui regroupe un morceau d'état et
de comportement réutilisable — l'équivalent, côté logique, de ce qu'un composant est côté
affichage. Un composable `useCompteur` peut être appelé depuis plusieurs composants, chacun
recevant sa propre instance indépendante de l'état.

```ts
import { ref } from 'vue'

export function useCompteur(depart = 0) {
  const valeur = ref(depart)
  const incrementer = () => valeur.value++
  return { valeur, incrementer }
}
```

## Pièges fréquents

Muter directement une prop depuis le composant enfant est interdit par convention — les données
descendent du parent vers l'enfant, jamais l'inverse ; pour proposer un changement, l'enfant émet
un événement et laisse le parent décider. Dans une boucle `v-for`, oublier l'attribut `:key`
empêche Vue de suivre correctement quel élément du DOM correspond à quel élément de la liste lors
d'une mise à jour, ce qui produit des affichages incohérents après un tri ou une suppression.
Enfin, confondre `ref` et `reactive` sur un objet — utiliser `reactive` puis réaffecter la
variable entière (`utilisateur = { ... }`) casse la réactivité, car cela remplace la référence
que Vue observait ; `ref` supporte cette réaffectation, `reactive` non.

## Ce qu'il faut retenir

Un composant Vue 3 typé s'écrit avec `<script setup lang="ts">` ; `ref` porte une valeur unique,
`reactive` porte un objet, et déstructurer ce dernier sans `toRefs` perd la réactivité. Les props
descendent, les émissions remontent, et les deux se typent explicitement. Un composable extrait
une logique réutilisable de la même façon qu'un composant extrait un affichage réutilisable.
