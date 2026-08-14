# Laboratoire — Guichet d'autorisation local, en processus

Ce laboratoire monte un serveur d'autorisation minimal *dans le processus de test* : aucun
fournisseur réel, aucune dépendance réseau, aucune bibliothèque de jetons. On y voit la mécanique
que les leçons OAuth de la semaine décrivent — le code, la preuve d'échange, le state, le nonce,
les deux jetons — écrite en bibliothèque standard, à l'échelle où elle se lit d'un trait.

## Ce que contient le dossier

- `src/ForgeOAuthLab/Program.cs` — le guichet : `/authorize` émet un code à usage unique lié à
  l'empreinte PKCE déposée, `/token` sert les deux flux — code d'autorisation avec preuve, et
  identifiants client pour la machine confidentielle.
- `src/ForgeOAuthLab/Identity/` — la fabrique de jetons (HMAC, trois segments, `at_hash` en
  moitié gauche de condensat) et le registre des codes, consommés par retrait atomique.
- `src/ForgeOAuthLab/Client/` — la part du *client* : le registre de `state` à usage unique et
  l'inspecteur de jeton d'identité — audience, nonce, partie autorisée, empreinte d'accès.
- `tests/ForgeOAuthLab.Tests/` — les deux flux de bout en bout et les trois refus qui font la
  sécurité : `code_verifier` faux refusé à l'échange, `state` rejoué refusé par le client, jeton
  d'accès présenté comme jeton d'identité refusé dès l'audience.

## Lancer

```powershell
dotnet test content/labs/oauth-local-idp/tests/ForgeOAuthLab.Tests/ForgeOAuthLab.Tests.csproj
```

## Ce qu'il faut regarder en premier

Le trajet d'un flux complet dans `PkceFlowDeliversLinkedIdentityAndAccess` : la fabrication du
`code_verifier` et de son empreinte, la redirection qui rend code et `state`, la consommation du
`state`, l'échange, puis l'inspection du jeton d'identité — chaque étape des leçons a sa ligne.
Comparez ensuite les deux réponses de `/token` : le flux machine n'émet *pas* de jeton
d'identité, et le test le vérifie — il n'y a personne dont attester l'identité.

La distinction des audiences est l'autre point clé : le jeton d'accès vise l'API, le jeton
d'identité vise le client, et c'est ce qui fait échouer la substitution dans
`AccessTokenPresentedAsIdTokenIsRefused` — avant même la question du nonce.

## Ce que ce laboratoire ne montre pas

Pas d'écran de connexion ni de consentement — l'utilisateur factice est auto-approuvé, car le
sujet est le protocole, pas l'interface. Pas de signature asymétrique ni de publication de clés :
le HMAC local suffit à la mécanique et évite toute dépendance. Pas de jeton de rafraîchissement
en flux réel : sa rotation se pratique dans l'exercice dédié de la semaine. Les clés et secrets
sont factices et le resteraient même en les copiant : aucun compte réel n'existe derrière.
