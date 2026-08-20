# Décider si un changement d'API est cassant

Implémentez `Submission.IsBreakingChange(string changeKind)`.

Vous préparez une évolution d'API et devez décider si elle exige une nouvelle version. La méthode
reçoit une étiquette décrivant le changement et rend vrai s'il est cassant.

Sont **sûrs** (non cassants) exactement ces trois cas :

- `add-optional-input` : ajouter un champ d'entrée facultatif ;
- `add-output-field` : ajouter un champ à une réponse ;
- `add-endpoint` : ajouter un point d'accès.

Tout le reste est **cassant** : retirer un champ, retirer un point d'accès, rendre un champ
obligatoire, restreindre un type ou un domaine, renommer, changer un sens — et *toute étiquette
inconnue*, par présomption de danger.

Règles exactes : l'étiquette se normalise avant décision — rognée, casse aplanie en minuscules
invariantes ; une étiquette absente ou blanche est cassante.

Écrivez avant le code : un cas sûr de chaque sorte, un retrait, un champ rendu obligatoire, et une
étiquette inconnue.

Exemple : entrée `["add-optional-input"]`, sortie `false`.
