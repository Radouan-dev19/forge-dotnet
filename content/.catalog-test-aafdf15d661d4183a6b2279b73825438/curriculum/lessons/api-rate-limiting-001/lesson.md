# Limiter le débit : fenêtres, seau de jetons et en-têtes honnêtes

## Objectif observable

À la fin de cette leçon, vous saurez comparer la fenêtre fixe, la fenêtre glissante et le seau de
jetons par leur comportement aux frontières, exposer une limite au client par les en-têtes
appropriés — `Retry-After`, la famille `RateLimit-*` — et justifier pourquoi une limite s'attache
à une identité plutôt qu'à une adresse réseau.

## Prérequis

- Avoir lu `api-http-semantics-001` : le statut 429 et la sémantique des en-têtes de réponse.
- Savoir raisonner sur des compteurs et des instants, comme dans les exercices de fenêtre
  glissante.

## Intuition

Limiter le débit, c'est répondre à « combien d'appels, sur combien de temps » — et transformer un
« trop » en refus poli plutôt qu'en effondrement du service. La question difficile n'est pas de
compter : c'est de choisir la *forme* du compteur, car chaque forme a un comportement de bord
différent, et c'est aux bords que les incidents se produisent.

## Explication

**La fenêtre fixe est simple et a un défaut de bord connu.** On compte les appels par tranche de
temps alignée — par minute d'horloge, par exemple — et on refuse au-delà du quota. Facile à
implémenter, facile à expliquer. Son défaut est la *rafale de bord* : un client peut consommer
tout son quota à la dernière seconde d'une tranche, puis tout son quota à la première seconde de
la suivante — deux fois le quota en un instant, à cheval sur la frontière. Pour beaucoup de
services, c'est acceptable ; pour ceux qui protègent une ressource fragile, c'est un trou.

**La fenêtre glissante lisse ce bord.** Au lieu d'une tranche alignée, on considère l'intervalle
des N dernières unités de temps à chaque instant. La rafale de bord disparaît, puisqu'il n'y a
plus de frontière à chevaucher. Le coût est la mémoire : suivre l'horodatage des appels récents,
ou approximer par une pondération entre la tranche courante et la précédente. C'est le même
raisonnement que les sommes de fenêtres glissantes vues côté algorithmes — un agrégat qui suit un
intervalle mobile.

**Le seau de jetons découple le débit moyen de la rafale tolérée.** On imagine un seau qui se
remplit d'un jeton à cadence fixe, jusqu'à une capacité ; chaque appel consomme un jeton, et un
appel sans jeton est refusé. Le débit moyen est fixé par la cadence de remplissage, mais la
capacité du seau autorise une rafale contrôlée quand il est plein — ce qui colle à l'usage réel,
où les clients envoient par à-coups. C'est le modèle le plus répandu dans les passerelles, parce
qu'il exprime deux paramètres indépendants : « en moyenne tant par seconde, avec des pointes
jusqu'à tant ».

**Exposer la limite honnêtement fait partie du contrat.** Un client bien élevé ralentit si on lui
dit comment. Le statut **429 Trop de requêtes** dit le refus ; l'en-tête `Retry-After` dit *quand*
retenter — en secondes ou à une date —, et la famille `RateLimit-*` annonce la limite, le reste
disponible et l'instant de réinitialisation *avant même* le refus, si bien qu'un client soigné
n'atteint jamais le mur. Une limite muette — 429 sans indication — pousse au réessai agressif, qui
aggrave la surcharge : l'honnêteté de l'en-tête est aussi une protection du service.

**Limiter par identité, pas par adresse.** L'adresse réseau est un mauvais identifiant : derrière
une même adresse peuvent vivre des milliers d'utilisateurs légitimes — un réseau d'entreprise, un
relais mobile — qu'une limite par adresse pénalise collectivement, tandis qu'un attaquant
distribué change d'adresse à volonté. Attacher la limite à l'*identité authentifiée* — la clé
d'API, le sujet du jeton — vise le vrai consommateur : chacun a son quota, l'abus d'un compte ne
punit pas les autres, et changer d'adresse ne réinitialise pas le compteur. Pour le trafic non
authentifié, l'adresse reste un pis-aller assumé, aux quotas plus larges et plus prudents.

## Exemple commenté

Décider l'admission avec un seau de jetons, forme la plus expressive — noyau d'un des exercices :

```csharp
// Le seau s'est rempli depuis le dernier appel ; on plafonne à sa capacité, puis on consomme.
public static int TokensAfterRequest(int tokensBefore, int capacity, int refilled)
{
    // Recharge bornée par la capacité : le seau ne déborde pas.
    int available = Math.Min(capacity, tokensBefore + refilled);

    // Un jeton disponible : l'appel passe et le consomme. Sinon, il est refusé.
    return available > 0 ? available - 1 : available;
}
```

Le plafonnement à la capacité est la subtilité : sans lui, un client inactif accumulerait un
crédit illimité et pourrait ensuite tout dépenser d'un coup, ce que le seau existe précisément
pour borner.

## Contre-exemple et erreur fréquente

Le code fautif renvoie un 429 nu et limite par adresse :

```csharp
// FAUTIF : refus muet, et compteur attaché à l'adresse réseau.
if (CountForAddress(remoteAddress) > quota)
{
    return StatusCode(429);   // Ni Retry-After, ni RateLimit-*.
}
```

Deux symptômes. Le refus muet pousse les clients à retenter immédiatement, en boucle, ce qui
transforme la limite en amplificateur de charge. Et la limite par adresse punit des bureaux
entiers derrière un relais partagé tout en laissant filer un attaquant qui tourne ses adresses.
La correction attache la limite à l'identité et parle au client :

```csharp
// CORRIGÉ : limite par identité authentifiée, et refus qui dit quand retenter.
if (CountForIdentity(callerSubject) > quota)
{
    Response.Headers["Retry-After"] = secondsUntilReset.ToString();
    return StatusCode(429);
}
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : entre fenêtre fixe et seau de jetons, lequel autorise une
rafale contrôlée par conception, et lequel en subit une par accident de bord ?

:::quiz
id=api-rate-limiting-001-check
question=Pourquoi préfère-t-on généralement limiter le débit par identité authentifiée plutôt que par adresse réseau ?
option=L'adresse est plus coûteuse à lire dans la requête
option=Une adresse regroupe souvent de nombreux utilisateurs légitimes et se change à volonté : elle punit les innocents et laisse filer l'attaquant distribué
option=La spécification HTTP interdit de compter par adresse
correct=1
success=Exact : l'identité vise le vrai consommateur — quota par compte, abus isolé, adresse changeante sans effet. L'adresse pénalise les réseaux partagés et n'arrête pas un attaquant qui tourne ses adresses.
retry=Demandez-vous qui se cache derrière une même adresse, et ce qu'un attaquant fait de son adresse.
:::

## Exercice guidé

Ouvrez l'exercice `api-token-bucket-001` dans `/practice`, puis procédez ainsi.

1. Écrivez l'invariant du seau : ce que représente le nombre de jetons avant et après un appel.
2. Rechargez d'abord, plafonnez à la capacité, puis décidez de l'admission.
3. Traitez le seau vide comme un refus sans consommation, jamais comme un solde négatif.
4. Prédisez l'état du seau après chaque cas visible, dont le seau plein et le seau vide.

## Exercice autonome

Choisissez un point d'accès sensible et dimensionnez sa limite : quelle forme de compteur, quel
débit moyen, quelle rafale tolérée, quelle clé d'identité ? Écrivez les en-têtes exacts que le
client verrait, en régime normal et au moment du refus.

## Débogage

Un ticket indique : « Toutes les requêtes d'un client partenaire sont refusées en 429 depuis midi,
alors qu'il n'a pas augmenté son trafic ; d'autres clients sur le même hébergeur sont touchés
aussi. »

1. **Symptôme** : refus généralisé, plusieurs clients distincts, corrélés à un hébergeur commun.
2. **Hypothèse** : la limite est comptée par adresse réseau, et ces clients partagent la même
   adresse de sortie — l'un d'eux a saturé le quota collectif.
3. **Preuve** : vérifier la clé du compteur ; si c'est l'adresse et non l'identité, comparer les
   identités authentifiées derrière l'adresse saturée.
4. **Prévention** : compter par identité authentifiée, et réserver le comptage par adresse au seul
   trafic anonyme, avec un quota séparé.

## Entretien

Question posée à voix haute : *quelles stratégies de limitation de débit connaissez-vous, et
laquelle choisiriez-vous ?*

Une réponse solide compare les trois formes par leur comportement de bord — rafale accidentelle de
la fenêtre fixe, lissage de la glissante, rafale maîtrisée du seau — plutôt que d'en réciter une.
Elle décrit les en-têtes qui rendent la limite honnête et explique pourquoi l'identité bat
l'adresse comme clé de comptage. Le lien avec les fenêtres glissantes algorithmiques est un bon
signe de recul.

## Résumé

- Fenêtre fixe : simple, mais rafale possible à cheval sur la frontière des tranches.
- Fenêtre glissante : pas de frontière à chevaucher, au prix d'un suivi plus coûteux.
- Seau de jetons : débit moyen et rafale tolérée réglés séparément, plafonnés par la capacité.
- 429 accompagné de `Retry-After` et de `RateLimit-*` : une limite honnête protège le service du
  réessai agressif.
- Limiter par identité authentifiée ; l'adresse n'est qu'un pis-aller pour l'anonyme.

## Cartes de révision

Question : quel défaut de bord la fenêtre fixe présente-t-elle, et laquelle des autres formes le
supprime par conception ? Réponse attendue : la rafale à cheval sur la frontière de deux tranches,
jusqu'à deux fois le quota en un instant ; la fenêtre glissante la supprime en n'ayant pas de
frontière alignée.

Question : pourquoi un 429 sans `Retry-After` peut-il aggraver une surcharge ? Réponse attendue :
faute d'indication sur le moment de retenter, les clients réessaient immédiatement et en boucle,
ce qui ajoute de la charge au service déjà saturé.

## Test de maîtrise

Sans relire, décrivez les trois stratégies par un schéma temporel de leur comportement de bord,
puis dimensionnez une limite complète pour un point d'accès de votre choix — forme, débit, rafale,
clé, en-têtes — et justifiez chaque paramètre.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
