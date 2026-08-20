# Choisir le flux depuis un profil de client

Implémentez `Submission.ChooseFlow(string clientProfile)`.

Le profil décrit un client OAuth par des étiquettes séparées par des virgules, une par axe :

- axe de présence : `user-present` — un humain est dans la boucle — ou `machine-only` ;
- axe de confidentialité : `confidential` — le client sait garder un secret — ou `public`.

Règles exactes :

- les étiquettes se normalisent avant analyse : segments rognés, casse aplanie en minuscules
  invariantes, ordre libre, segments vides ignorés ;
- le profil doit porter exactement une étiquette par axe ; toute étiquette inconnue, tout axe
  manquant, doublé ou contradictoire rend `"invalid-profile"` — de même qu'un profil absent ou
  blanc ;
- avec `user-present`, rendez `"authorization-code-pkce"` — quel que soit l'axe de
  confidentialité ;
- avec `machine-only` et `confidential`, rendez `"client-credentials"` ;
- avec `machine-only` et `public`, rendez `"refused"` : aucun flux légitime ne couvre une
  machine incapable de garder un secret.

Écrivez avant le code : un profil valide de chaque verdict, un profil aux étiquettes désordonnées
et en casse mélangée, et un profil contradictoire.

Exemple : entrée `["user-present,public"]`, sortie `"authorization-code-pkce"`.
