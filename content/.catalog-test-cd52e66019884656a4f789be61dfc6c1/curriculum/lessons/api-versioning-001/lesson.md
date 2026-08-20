# Versionner une API : où porter le numéro, et comment mourir proprement

## Objectif observable

À la fin de cette leçon, vous saurez choisir un emplacement de version entre l'URL, un en-tête et
le type de média en justifiant le compromis, décider si un changement donné est cassant, et
dérouler le retrait d'une version sans surprendre les consommateurs qui en dépendent encore.

## Prérequis

- Avoir lu `api-http-semantics-001` : les méthodes et les statuts que la version conserve.
- Savoir ce qu'est un contrat public entre un producteur et des consommateurs qu'on ne contrôle
  pas.

## Intuition

Une version d'API est une promesse gelée : « ce que vous consommez aujourd'hui continuera de se
comporter ainsi, même quand j'évoluerai ». Sans elle, chaque amélioration risque de casser
quelqu'un que vous ne connaissez pas. Versionner, c'est se donner le droit de changer sans
trahir — et le vrai sujet n'est pas le numéro, c'est ce qui oblige à l'incrémenter et comment
retirer l'ancien.

## Explication

**Trois emplacements, trois compromis.** La version *dans l'URL* — un segment de chemin — est la
plus visible et la plus simple à router : elle se lit dans un journal, se teste dans un
navigateur, se met en cache sans ambiguïté. Son défaut est doctrinal : deux URL désignent alors
la « même » ressource sous deux formes, ce qui froisse les puristes de l'identité des ressources.
La version *dans un en-tête* dédié garde une URL unique par ressource, au prix de l'invisibilité —
on ne voit plus la version dans le lien, les caches doivent être configurés pour en tenir compte,
et un appel sans l'en-tête tombe sur un défaut qu'il faut décider. La version *dans le type de
média* — la négociation de contenu poussée à son terme — est la plus fidèle à l'esprit HTTP et la
plus déroutante en pratique : peu d'outils la manipulent naturellement. Aucune n'est absolument
supérieure ; le choix se justifie par le public — l'URL pour une API publique large, l'en-tête ou
le média pour un écosystème maîtrisé.

**Ce qui est cassant, et ce qui ne l'est pas.** La distinction gouverne tout, car seul un
changement cassant force une nouvelle version. Sont cassants : retirer un champ d'une réponse,
retirer un point d'accès, rendre obligatoire un champ d'entrée jusque-là facultatif, resserrer un
type ou un domaine de valeurs accepté, changer le sens d'un champ existant. Ne sont pas cassants :
ajouter un champ *facultatif* en entrée, ajouter un champ en sortie — à condition que les
consommateurs ignorent ce qu'ils ne connaissent pas —, ajouter un point d'accès. La règle
d'asymétrie est utile à retenir : on peut presque toujours *ajouter* sans casser, presque jamais
*retirer* ni *restreindre*. C'est la même logique que le versionnage sémantique des bibliothèques,
transposée au contrat HTTP.

**Le principe de robustesse encadre le non-cassant.** Un ajout en sortie n'est sûr que si les
consommateurs sont tolérants — s'ils ignorent les champs inconnus au lieu d'échouer à les
désérialiser. Vous ne contrôlez pas leur code, mais vous documentez cette attente comme une
condition du contrat : « des champs peuvent apparaître ; ignorez ceux que vous ne connaissez
pas ». Sans cette clause, même l'ajout devient cassant en pratique, et l'on se retrouve à
versionner pour un champ de plus.

**Retirer une version est un processus, pas un interrupteur.** On annonce l'obsolescence bien à
l'avance — un en-tête qui signale la dépréciation et une date de fin de vie, une documentation
qui pointe la version de remplacement. On mesure ensuite l'usage résiduel : couper une version
que personne n'appelle est indolore, couper celle qu'un partenaire critique utilise encore est un
incident. La coupure elle-même rend un statut franc — la ressource n'est plus là — plutôt qu'un
silence ou une redirection surprise. Versionner sans jamais retirer aboutit à un musée de
versions que plus personne ne sait maintenir : le retrait fait partie du cycle, pas de son
échec.

## Exemple commenté

Décider si un changement est cassant, encodé comme le noyau de l'exercice guidé :

```csharp
// Un ajout facultatif en entrée est compatible ; retirer ou restreindre ne l'est pas.
public static bool IsBreakingChange(string changeKind)
{
    string normalized = changeKind.Trim().ToLowerInvariant();

    // Liste blanche des changements SÛRS : tout le reste est présumé cassant.
    return normalized switch
    {
        "add-optional-input" => false,
        "add-output-field" => false,
        "add-endpoint" => false,
        _ => true,
    };
}
```

La liste blanche est le bon sens du domaine : dans le doute, un changement est cassant. Présumer
l'inverse — « compatible sauf preuve du contraire » — laisse passer les régressions silencieuses.

## Contre-exemple et erreur fréquente

Le code fautif « améliore » une réponse en renommant un champ, sans changer de version :

```csharp
// FAUTIF : le champ est renommé dans la même version.
// Avant : { "name": "Ada" }
// Après : { "fullName": "Ada" }
return new { fullName = customer.Name };   // Tout consommateur lisant "name" casse.
```

Le symptôme ne se voit pas chez vous : vos tests lisent déjà `fullName`. Il se voit chez les
consommateurs, dispersés, qui reçoivent soudain une réponse sans le champ qu'ils attendaient — un
renommage est un retrait *et* un ajout, donc doublement cassant. La correction respecte le
contrat gelé :

```csharp
// CORRIGÉ : la v1 garde "name" ; le nouveau nom vit dans une v2, ou s'ajoute sans retirer.
return new { name = customer.Name };
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : rendre obligatoire un champ d'entrée jusque-là facultatif,
est-ce cassant ? Pour qui, exactement ?

:::quiz
id=api-versioning-001-check
question=Lequel de ces changements peut être livré SANS nouvelle version ?
option=Retirer un champ devenu inutile d'une réponse
option=Ajouter un champ facultatif en entrée, en laissant l'ancien comportement inchangé quand il est absent
option=Rendre obligatoire un paramètre de requête auparavant facultatif
correct=1
success=Exact : ajouter du facultatif ne casse aucun appel existant, qui continue de fonctionner sans le fournir. Retirer et rendre obligatoire cassent des appels en place.
retry=Reprenez la règle d'asymétrie : quelle opération — ajouter, retirer, restreindre — laisse les appels existants intacts ?
:::

## Exercice guidé

Ouvrez l'exercice `api-version-change-001` dans `/practice`, puis procédez ainsi.

1. Listez les catégories de changement du contrat et rangez chacune du côté sûr ou cassant.
2. Implémentez la normalisation de l'étiquette avant toute décision.
3. Adoptez la présomption de danger : tout ce qui n'est pas explicitement sûr est cassant.
4. Prédisez le verdict de chaque cas visible, dont un changement inconnu.

## Exercice autonome

Prenez une API que vous connaissez et écrivez trois évolutions souhaitables. Pour chacune :
est-elle cassante ? Si oui, comment l'introduire — nouvelle version, ou reformulation
non cassante ? Décrivez ensuite le plan de retrait de la version qu'elle remplacerait.

## Débogage

Un ticket indique : « Depuis le déploiement de ce matin, l'application mobile en production affiche
des champs vides sur l'écran de profil, alors que l'API répond 200. »

1. **Symptôme** : réponse réussie, données manquantes côté client, apparu à un déploiement précis.
2. **Hypothèse** : un champ de réponse a été renommé ou retiré dans la version que le mobile
   consomme, sans montée de version.
3. **Preuve** : comparer le corps renvoyé aujourd'hui à celui d'avant le déploiement, sur la même
   route et la même version, et repérer le champ disparu.
4. **Prévention** : un test de contrat qui échoue si un champ documenté disparaît d'une version
   publiée, et la discipline de n'ajouter qu'en place.

## Entretien

Question posée à voix haute : *comment versionnez-vous une API, et qu'est-ce qui vous oblige à
créer une nouvelle version ?*

Une réponse solide compare les trois emplacements par leur compromis plutôt que d'en décréter un
seul valable, énonce la règle d'asymétrie ajouter/retirer/restreindre pour définir le cassant, et
n'oublie pas le retrait : dépréciation annoncée, mesure de l'usage résiduel, coupure franche.
Elle relie le tout au versionnage sémantique, dont c'est la transposition HTTP.

## Résumé

- Trois emplacements — URL, en-tête, type de média — chacun avec son compromis de visibilité et
  de routage.
- Est cassant ce qui retire, restreint ou change le sens ; ajouter du facultatif ne l'est pas.
- Le principe de robustesse rend l'ajout sûr : les consommateurs ignorent ce qu'ils ne
  connaissent pas.
- Dans le doute, un changement est cassant : la présomption protège les consommateurs.
- Retirer une version se planifie : dépréciation annoncée, usage mesuré, coupure franche.

## Cartes de révision

Question : pourquoi un renommage de champ de réponse est-il doublement cassant ? Réponse
attendue : il retire l'ancien nom — les lecteurs de l'ancien champ cassent — et en ajoute un
nouveau, donc c'est un retrait et un ajout à la fois.

Question : quelle présomption adopter face à un changement dont on n'est pas sûr qu'il soit
compatible ? Réponse attendue : le présumer cassant, car « compatible sauf preuve du contraire »
laisse passer les régressions silencieuses chez des consommateurs qu'on ne contrôle pas.

## Test de maîtrise

Sans relire, classez huit changements d'API de votre invention en cassants ou non, en justifiant
chacun par la règle d'asymétrie. Puis décrivez le cycle de vie complet d'une version, de son
introduction à son retrait, en nommant ce que le consommateur voit à chaque étape.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
