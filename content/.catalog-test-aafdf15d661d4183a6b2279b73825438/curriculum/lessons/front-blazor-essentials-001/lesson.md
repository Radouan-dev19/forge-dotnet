# Blazor : Server ou WebAssembly, paramètres, interop et état d'authentification

## Objectif observable

À la fin de cette leçon, vous saurez choisir entre Blazor Server et WebAssembly en pesant leurs
conséquences concrètes — latence, circuit, téléchargement, hors ligne —, passer des paramètres et
des paramètres cascadés, déclencher du JavaScript par interop, situer les étapes du cycle de rendu,
et lire l'état d'authentification par le fournisseur dédié.

## Prérequis

- Avoir lu `front-controlled-forms-001` : le champ contrôlé et l'état de saisie tenu par le modèle.
- Avoir lu `front-client-server-contract-001` : le contrat entre le client et le serveur.

## Intuition

Blazor écrit l'interface en C#, mais la question qui structure tout est *où le composant s'exécute*.
En Server, le composant vit sur le serveur et l'interaction voyage sur un circuit permanent : chaque
clic fait un aller-retour réseau. En WebAssembly, le composant descend dans le navigateur et
s'exécute sur la machine de l'utilisateur, au prix d'un téléchargement initial du runtime. Le même
code, deux exécutions radicalement différentes. Comprendre Blazor, c'est mesurer ce que ce choix
impose avant d'écrire un seul composant. `PracticePage.razor`, dans ce dépôt, en est l'exemple réel
que nous citons tout du long.

## Explication

**Server et WebAssembly ne diffèrent pas par le code mais par ses conséquences.** En Blazor Server,
l'état vit sur le serveur, relié au navigateur par un circuit — une connexion persistante. Le
téléchargement initial est minime, le code d'accès aux données reste côté serveur, mais chaque
interaction subit la latence du réseau et le serveur porte l'état de chaque utilisateur connecté ;
une coupure rompt le circuit. En Blazor WebAssembly, le runtime .NET est téléchargé une fois puis
tout s'exécute dans le navigateur : après ce coût de démarrage, les interactions sont locales, le
fonctionnement hors ligne devient possible, mais tout appel de données passe par une API réseau, car
il n'y a plus de serveur dans la boucle de rendu. Le choix se pose ainsi : latence par interaction
contre poids du démarrage, état serveur contre exécution cliente, dépendance au circuit contre
capacité hors ligne.

**Les paramètres descendent, les paramètres cascadés traversent.** Un `[Parameter]` est une entrée
qu'un composant parent fixe explicitement — `PracticePage` reçoit ainsi son `ExerciseId` depuis
l'URL. Un paramètre cascadé, lui, est fourni haut dans l'arbre et lu par n'importe quel descendant
sans être passé de main en main : c'est le canal des données transverses comme l'état
d'authentification. La règle de flux reste celle du socle des formulaires contrôlés : les données
descendent, les événements remontent.

**Le cycle de rendu a des points d'accroche nommés.** Quand un paramètre change, Blazor appelle
`OnParametersSetAsync` ; c'est là qu'on recharge ce qui dépend de l'entrée. `PracticePage` s'en sert
exactement pour cela.

```razor
@page "/practice/{ExerciseId}"
@inject PracticeService Practice
@inject IJSRuntime JavaScript
@implements IDisposable

@code {
    [Parameter]
    public string ExerciseId { get; set; } = string.Empty;

    protected override async Task OnParametersSetAsync() => await LoadAsync();
}
```

La directive `@page` déclare la route et capture le segment `ExerciseId` ; `@inject` fournit les
dépendances par injection, comme en Angular mais avec la syntaxe Razor ; `OnParametersSetAsync`
recharge l'activité chaque fois que le paramètre de route change.

**L'interop JavaScript franchit la frontière vers le navigateur.** Certaines actions n'existent que
côté navigateur — déclencher un téléchargement de fichier, par exemple. Blazor les atteint par
`IJSRuntime`. `PracticePage` exporte un ZIP en passant un flux .NET au JavaScript sans le copier en
mémoire, grâce à `DotNetStreamReference`.

```csharp
using var stream = new MemoryStream(package.Content.ToArray(), writable: false);
using var streamReference = new DotNetStreamReference(stream);
await JavaScript.InvokeVoidAsync(
    "forgeDownloads.downloadFileFromStream",
    package.FileName,
    streamReference);
```

**Le formulaire s'appuie sur `EditForm` et la liaison bidirectionnelle.** `PracticePage` lie chaque
champ au modèle par `@bind`, ce qui applique côté Blazor le champ contrôlé du socle : la valeur vit
dans le modèle, le champ la reflète.

```razor
<EditForm Model="_reflection" OnSubmit="SaveReflectionAsync" FormName="practice-reflection">
    <PracticeTextArea Id="reflection-reformulation" Label="Reformulation du problème"
                      @bind-Value="_reflection.Reformulation" />
    <button class="btn btn-primary" type="submit" disabled="@_busy">Enregistrer</button>
</EditForm>
```

**Le composant possède un cycle de vie, donc doit se nettoyer.** `PracticePage` implémente
`IDisposable` : un appel de runner en cours est annulable par un `CancellationTokenSource`, annulé
et libéré à la destruction du composant. C'est l'équivalent Blazor de la discipline de désinscription
d'Angular : toute ressource ouverte doit être fermée dans `Dispose`.

**L'état d'authentification se lit par un fournisseur dédié.** `AuthenticationStateProvider` expose,
en cascade, l'utilisateur courant à tout l'arbre ; les composants n'interrogent pas un service
d'authentification à la main, ils lisent l'état cascadé et réagissent à ses changements.

## Exemple commenté

Le noyau transposable est la machine à états d'un champ de formulaire — la logique que `@bind` et la
validation d'`EditForm` mettent en oeuvre, isolée telle que le runner de Forge la fait pratiquer.

```csharp
// Machine à états d'un champ contrôlé : la transition dépend de l'état courant et de l'événement.
public static FieldState Next(FieldState state, FieldEvent input) => (state, input) switch
{
    (FieldState.Pristine, FieldEvent.Changed) => FieldState.Editing,
    (FieldState.Editing,  FieldEvent.Blurred) => FieldState.Validating,
    (FieldState.Validating, FieldEvent.ValidationPassed) => FieldState.Valid,
    (FieldState.Validating, FieldEvent.ValidationFailed) => FieldState.Invalid,
    (FieldState.Invalid,  FieldEvent.Changed) => FieldState.Editing,
    _ => state, // tout autre couple laisse l'état inchangé
};
```

La valeur reste dans le modèle et l'état suit les transitions ; le champ n'est qu'un reflet, jamais
la source.

## Contre-exemple et erreur fréquente

L'erreur classique est de lancer un appel annulable sans jamais le libérer, laissant fuir le circuit
et le jeton d'annulation.

```csharp
// FAUTIF : appel annulable sans Dispose ; à la destruction du composant, rien n'est nettoyé.
private CancellationTokenSource _cts = new();

private async Task RunAsync()
{
    _cts = new CancellationTokenSource();      // l'ancien CTS fuit
    await Runner.ExecuteAsync(command, _cts.Token);
}
// aucun IDisposable : l'appel en cours n'est jamais annulé quand la page change
```

La correction suit `PracticePage` : implémenter `IDisposable`, annuler et libérer le jeton.

```csharp
// CORRIGÉ : le CTS est libéré dans le finally, et Dispose annule tout appel encore en vol.
public void Dispose()
{
    _runnerCancellation?.Cancel();
    _runnerCancellation?.Dispose();
}
```

## Vérification de compréhension

Avant le quiz, dites à voix haute ce qui change entre Server et WebAssembly pour un utilisateur sur
un réseau à forte latence qui clique sur beaucoup de boutons.

:::quiz
id=front-blazor-essentials-001-check
question=Quelle conséquence distingue le mieux Blazor Server de Blazor WebAssembly ?
option=WebAssembly n'utilise pas de composants Razor, contrairement à Server
option=En Server chaque interaction fait un aller-retour réseau via le circuit, alors qu'en WebAssembly les interactions s'exécutent localement après un téléchargement initial du runtime
option=Server fonctionne hors ligne alors que WebAssembly exige une connexion permanente
correct=1
success=Exact : Server relie le navigateur au serveur par un circuit, donc chaque interaction subit la latence réseau ; WebAssembly télécharge le runtime puis exécute localement, ce qui autorise le hors ligne.
retry=Repensez à l'endroit où le composant s'exécute dans chaque modèle, et à ce que cela coûte par interaction.
:::

## Exercice guidé

Ouvrez l'exercice `front-form-field-state-001` dans `/practice`, puis procédez ainsi.

1. Énumérez les états d'un champ contrôlé : vierge, en édition, en validation, valide, invalide.
2. Écrivez la transition comme une fonction pure prenant l'état courant et l'événement.
3. Laissez inchangé tout couple état/événement non prévu, au lieu de lever une erreur.
4. Prédisez l'état final d'une séquence d'événements appliquée depuis l'état vierge.

## Exercice autonome

Pour une application imaginaire, tranchez le choix Server contre WebAssembly en listant, pour chaque
option, la latence par interaction, le poids du démarrage, la dépendance au réseau et le besoin hors
ligne. Décrivez ensuite comment un paramètre de route et un paramètre cascadé d'authentification
circuleraient dans votre arbre de composants.

## Débogage

Un ticket indique : « En quittant la page de pratique pendant une exécution longue, l'appel continue
côté serveur et parfois un second appel démarre par-dessus. »

1. **Symptôme** : un appel de runner survit à la navigation ; des appels se chevauchent.
2. **Hypothèse** : le `CancellationTokenSource` n'est ni annulé ni libéré à la destruction, faute
   d'implémenter `IDisposable` correctement.
3. **Preuve** : vérifier que le composant implémente `IDisposable`, que `Dispose` appelle `Cancel`
   puis `Dispose` sur le jeton, et que le `finally` libère bien le jeton après chaque exécution,
   comme dans `PracticePage`.
4. **Prévention** : toute ressource annulable ouverte dans un composant doit être fermée dans
   `Dispose` ; le vérifier en revue au même titre qu'une désinscription.

## Entretien

Question posée à voix haute : *entre Blazor Server et WebAssembly, comment choisissez-vous, et
qu'est-ce que ce choix vous impose ensuite ?*

Une réponse solide oppose latence par interaction et poids du démarrage, état serveur et exécution
cliente, dépendance au circuit et capacité hors ligne. Elle montre que le code des composants est le
même mais que l'accès aux données, la robustesse à la coupure et le nettoyage des ressources
diffèrent — en citant `Dispose` et l'annulation d'un appel en vol comme dans `PracticePage`.

## Résumé

- Le choix Server/WebAssembly porte sur les conséquences : latence, circuit, téléchargement, hors
  ligne — pas sur le code des composants.
- `[Parameter]` descend une entrée ; un paramètre cascadé traverse l'arbre, canal de l'authentification.
- `OnParametersSetAsync` recharge ce qui dépend d'un paramètre modifié, comme dans `PracticePage`.
- L'interop `IJSRuntime` franchit la frontière navigateur ; `DotNetStreamReference` passe un flux sans copie.
- Un composant qui ouvre une ressource annulable la ferme dans `Dispose`.

## Cartes de révision

Question : quand `OnParametersSetAsync` est-il appelé et à quoi sert-il dans `PracticePage` ?
Réponse attendue : à chaque changement de paramètre, ici `ExerciseId` ; il y recharge l'activité
correspondante.

Question : pourquoi `PracticePage` implémente-t-il `IDisposable` ? Réponse attendue : pour annuler et
libérer le `CancellationTokenSource` d'un appel de runner encore en vol quand le composant est
détruit, évitant une fuite et des appels orphelins.

## Test de maîtrise

Sans relire, opposez Server et WebAssembly sur quatre conséquences concrètes, puis décrivez le trajet
d'un paramètre de route jusqu'à `OnParametersSetAsync` et celui d'un appel interop jusqu'au
JavaScript. Terminez en expliquant pourquoi le nettoyage dans `Dispose` est indispensable.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
