# Formulaires contrôlés et validation côté client

## Objectif observable

À la fin de cette leçon, vous saurez distinguer un champ dont la valeur affichée est pilotée par
l'état d'un champ qui garde sa propre valeur, décrire la machine d'états d'interaction — pristine,
dirty, touched —, choisir le bon moment pour valider, et expliquer pourquoi la validation dans le
navigateur guide l'utilisateur sans jamais protéger le serveur.

## Prérequis

- Avoir lu `front-state-unidirectional-001` : la source unique de vérité et le flux descendant.
- Savoir qu'une saisie utilisateur est une intention à confirmer, pas une donnée déjà fiable.

## Intuition

Un champ contrôlé, c'est une marionnette dont vous tenez tous les fils : ce qui s'affiche n'est
jamais la frappe brute de l'utilisateur, mais ce que votre état dit d'afficher, une fois la frappe
passée par vous. Un champ non contrôlé, lui, vit sa vie : il retient ce qu'on y saisit, et vous ne
le consultez qu'à la validation. Le premier vous donne la main sur chaque caractère au prix d'un
aller-retour par frappe ; le second est plus léger mais vous prive de réagir en direct. Comprendre
les formulaires, c'est d'abord savoir qui, du composant ou de l'élément, détient la vérité saisie.

## Explication

**Le champ contrôlé fait de l'état la seule vérité.** Dans un champ contrôlé, la valeur affichée
est liée à une variable d'état, et chaque frappe déclenche un événement qui met cet état à jour ;
au rendu suivant, le champ réaffiche la valeur venue de l'état. La boucle est donc : l'utilisateur
tape, l'événement remonte, l'état change, la valeur redescend. C'est le flux unidirectionnel
appliqué à la saisie. L'avantage est que l'état reflète à tout instant le contenu du champ : on
peut le lire, le transformer, le valider ou l'empêcher, sans interroger l'élément.

**Le champ non contrôlé garde sa valeur pour lui.** Un champ non contrôlé conserve sa propre
valeur dans l'élément, sans la refléter dans l'état à chaque frappe ; on ne la lit qu'au moment
voulu, souvent à la soumission. C'est plus simple quand on n'a besoin de rien en direct — pas de
validation instantanée, pas de champ dépendant d'un autre. Le prix est qu'entre deux lectures,
l'état ignore ce que contient le champ ; il n'y a plus de source unique consultable à tout moment.

**L'interaction d'un champ est une petite machine d'états.** Un champ traverse des états qui n'ont
rien à voir avec sa valeur, mais avec l'histoire de l'interaction. Au départ il est pristine :
intact, jamais modifié depuis son chargement. Dès que l'utilisateur en change la valeur, il
devient dirty : son contenu diffère de la valeur initiale. Indépendamment, il devient touched
quand l'utilisateur l'a visité puis quitté — il a reçu puis perdu le focus. Ces états sont
distincts : un champ peut être touched sans être dirty — on l'a survolé sans rien taper — et,
plus rarement, dirty puis ramené à sa valeur d'origine. Suivre ces états est ce qui permet
d'afficher une erreur au bon moment plutôt que d'agresser l'utilisateur dès l'affichage.

**Le moment de la validation est une décision d'expérience.** Valider à chaque frappe signale
l'erreur trop tôt : l'utilisateur n'a pas fini de taper son adresse qu'on lui reproche déjà un
format invalide. Valider seulement à la soumission signale trop tard : il découvre d'un coup dix
erreurs. Le compromis courant valide un champ quand il devient touched — l'utilisateur l'a quitté,
donc a fini son intention — puis, après une première erreur, réévalue à chaque frappe pour
confirmer la correction. La soumission reste le filet : elle revalide tout, y compris les champs
restés pristine.

**La validation côté client est un confort, jamais une barrière.** Tout ce qui s'exécute dans le
navigateur est sous le contrôle de l'utilisateur : il peut modifier le code, désactiver les
vérifications, ou envoyer une requête directe qui contourne entièrement le formulaire. La
validation côté client sert donc uniquement l'expérience : réponse immédiate, message clair,
guidage. Elle ne garantit rien sur ce qui arrive au serveur. Le serveur doit revalider chaque
donnée reçue comme si aucun contrôle client n'avait eu lieu, car de son point de vue c'est le cas.
Croire que la validation du navigateur protège les données, c'est laisser la porte ouverte à
quiconque n'utilise pas le formulaire.

**Ceci n'est pas l'agrégation des violations côté serveur.** Ce que décrit cette leçon est le
retour local, immédiat, propre à un champ pendant la saisie. Le serveur, lui, effectue une
validation qui fait autorité et renvoie l'ensemble des violations constatées ; agréger et présenter
ces violations serveur est un autre sujet. Le client anticipe pour conforter l'utilisateur ; le
serveur tranche. Les deux coexistent, mais seul le second est une barrière de sécurité.

## Exemple commenté

Le coeur de la machine d'états d'un champ, en C# : dériver pristine, dirty et touched depuis une
séquence d'interactions.

```csharp
public record FieldState(bool Dirty, bool Touched)
{
    public bool Pristine => !Dirty;
}

public abstract record Interaction;
public sealed record Changed(string Value) : Interaction; // frappe
public sealed record Blurred() : Interaction;             // le champ perd le focus

public static FieldState Apply(FieldState s, string initial, Interaction i) => i switch
{
    Changed c => s with { Dirty = c.Value != initial }, // dirty = differe de la valeur initiale
    Blurred   => s with { Touched = true },             // touched = visite puis quitte
    _         => s
};
```

Le point à retenir : dirty compare la valeur à l'initiale, tandis que touched enregistre le départ
du focus ; les deux évoluent indépendamment et décident ensemble du moment d'afficher une erreur.

## Contre-exemple et erreur fréquente

L'erreur classique traite la validation client comme une garantie et affiche l'erreur dès le
premier rendu.

```csharp
// FAUTIF : on considere la donnee sure des que le client l'a validee,
// et on marque le champ en erreur avant meme toute interaction.
bool safeToStore = ClientValidate(input); // fausse garantie : le client est contournable
bool showError = !IsValid(input);         // erreur affichee sur un champ pristine
```

Symptôme double : le formulaire agresse l'utilisateur avec une erreur rouge avant qu'il n'ait rien
tapé, et une requête forgée hors du formulaire enregistre des données invalides que « le client
avait pourtant validées ». La correction repousse l'affichage au bon état d'interaction et confie
la garantie au serveur.

```csharp
// CORRIGE : erreur montree seulement si le champ est touched ; le serveur revalide tout.
bool showError = field.Touched && !IsValid(input);
// safeToStore n'existe pas cote client : la barriere est cote serveur.
```

## Vérification de compréhension

Avant le quiz, dites à voix haute : un champ jamais tapé mais survolé puis quitté est-il dirty,
touched, ou les deux ?

:::quiz
id=front-controlled-forms-001-check
question=Pourquoi la validation exécutée dans le navigateur ne peut-elle pas servir de barrière de sécurité ?
option=Parce qu'elle est trop lente pour vérifier des règles complexes avant l'envoi
option=Parce que tout ce qui s'exécute dans le navigateur est sous le contrôle de l'utilisateur, qui peut la désactiver ou envoyer une requête directe qui contourne le formulaire
option=Parce que le navigateur ne peut pas lire le contenu des champs de saisie
correct=1
success=Exact : le client est contournable, donc sa validation ne sert que le confort ; seul le serveur, qui revalide tout, constitue une barrière.
retry=Demandez-vous qui contrôle le code qui tourne dans le navigateur, et ce qu'une requête forgée hors du formulaire subit comme vérification.
:::

## Exercice guidé

Ouvrez l'exercice `front-form-field-state-001` dans `/practice`, puis procédez ainsi.

1. Notez la valeur initiale du champ, celle qui sert de référence pour l'état dirty.
2. Rejouez la séquence d'interactions et mettez à jour dirty à chaque frappe, touched à chaque perte de focus.
3. Décidez, pour chaque étape, si une erreur doit être affichée selon l'état touched du champ.
4. Vérifiez qu'aucune décision de sécurité ne dépend de cette validation : elle ne guide que l'affichage.

## Exercice autonome

Concevez un champ d'adresse électronique contrôlé. Décrivez sa boucle frappe-état-affichage, puis
définissez quand l'erreur de format apparaît selon pristine, dirty et touched. Enfin, écrivez la
phrase justifiant que le serveur revalide ce champ même si le client l'a déjà jugé valide.

## Débogage

Un ticket indique : « Le champ courriel affiche une erreur de format en rouge dès l'ouverture du
formulaire, avant que l'utilisateur n'ait cliqué dedans. »

1. **Symptôme** : une erreur de validation est visible sur un champ encore pristine, jamais touché.
2. **Hypothèse** : l'affichage de l'erreur dépend seulement de la validité de la valeur, sans
   tenir compte de l'état touched du champ.
3. **Preuve** : conditionner l'affichage à touched et vérifier que l'erreur n'apparaît qu'après
   que l'utilisateur a quitté le champ.
4. **Prévention** : lier tout message d'erreur à la machine d'états d'interaction, et réserver la
   validation à la soumission pour les champs restés pristine.

## Entretien

Question posée à voix haute : *quelle est la différence entre un champ contrôlé et un champ non
contrôlé, et pourquoi la validation côté client ne dispense-t-elle pas de valider côté serveur ?*

Une réponse solide décrit la boucle frappe-état-affichage du champ contrôlé, oppose le champ non
contrôlé qui garde sa valeur, puis situe pristine, dirty et touched comme la clé du bon moment
d'affichage. Elle affirme que le client est contournable, donc que sa validation est un confort, et
que le serveur revalide tout — sans confondre ce sujet avec l'agrégation des violations serveur.

## Résumé

- Un champ contrôlé fait de l'état la vérité affichée ; un champ non contrôlé garde sa propre valeur.
- L'interaction suit une machine d'états : pristine, dirty (diffère de l'initiale), touched (visité puis quitté).
- Valider à la perte de focus, réévaluer après une erreur, revalider tout à la soumission.
- La validation côté client sert le confort, pas la sécurité : le client est contournable.
- Le serveur revalide chaque donnée ; sujet distinct de l'agrégation des violations serveur.

## Cartes de révision

Question : quelle différence y a-t-il entre les états dirty et touched d'un champ ? Réponse
attendue : dirty signifie que la valeur diffère de sa valeur initiale ; touched signifie que
l'utilisateur a visité le champ puis l'a quitté. Ils évoluent indépendamment.

Question : à quoi sert exactement la validation exécutée dans le navigateur ? Réponse attendue :
à donner un retour immédiat et clair à l'utilisateur pendant la saisie ; elle ne protège rien, car
le client est contournable et le serveur doit revalider chaque donnée reçue.

## Test de maîtrise

Sans relire, décrivez la boucle d'un champ contrôlé, puis la machine d'états pristine, dirty et
touched avec un exemple par transition. Expliquez enfin pourquoi la validation côté client ne
remplace jamais celle du serveur, et en quoi ce sujet diffère de l'agrégation des violations
serveur.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
