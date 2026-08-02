# Incrément 02A — Schémas de contenu

## 1. Statut

Validé le 25 juillet 2026. Huit schémas JSON v1 gelés ; 8 fixtures positives et 10 négatives vérifiées ; 44 tests .NET et la vérification complète sont verts.

## 2. Objectif

Définir les schémas JSON v1 canoniques et un validateur déterministe pour tous les types de contenu, avant tout catalogue ou contenu massif.

## 3. Contexte à lire

`AGENTS.md`, `ARCHITECTURE.md`, `CONTENT_GUIDE.md`, `SECURITY.md`, `CURRICULUM.md`, `ROADMAP.md`, fiche `01D`.

## 4. Prérequis

`01D` validé ; décisions JSON canonique, IDs stables et versionnement confirmées.

## 5. Périmètre inclus

Schémas leçon, exercice, curriculum, debug, SQL, entretien, anglais et projet ; contrats de validation ; fixtures valides/invalides ; CLI ou service de validation ; diagnostic précis des erreurs.

## 6. Périmètre explicitement exclu

Catalogue chargé en mémoire, recherche, lecteur UI, exercices exécutables, contenu réel au-delà des fixtures minimales.

## 7. Fichiers ou projets principalement concernés

`content/schemas/`, contrats Domain/Application, validateur Infrastructure, tests Unit/Integration, documentation du schéma.

## 8. Étapes d'implémentation

Inventorier champs obligatoires ; écrire schémas v1 ; imposer IDs/versions/chemins ; créer fixtures ciblées ; implémenter validation agrégée et messages localisés ; intégrer une commande de validation sans charger le catalogue.

## 9. Règles d'architecture

Modèles de contenu indépendants de l'UI ; validation orchestrée par Application et lecture de fichiers dans Infrastructure ; aucune persistance du catalogue en SQLite.

## 10. Règles de sécurité

Canonicaliser les chemins sous `content/`, refuser traversal/liens symboliques dangereux, HTML brut, tailles excessives et références externes obligatoires ; ne jamais exposer solutions/tests cachés.

## 11. Tests à écrire

Fixture valide par type ; champ absent ; type/enum invalide ; ID dupliqué ; version invalide ; poids hors bornes ; chemin traversal ; section/indice manquant ; message contenant fichier et propriété.

## 12. Tests manuels à effectuer

Valider un petit dossier ; introduire une erreur ; vérifier sortie lisible, code non nul et absence de fichier partiellement accepté.

## 13. Commandes de vérification

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
# Exécuter la commande de validation documentée sur les fixtures.
```

## 14. Critères d'acceptation

Tous les formats de `CONTENT_GUIDE.md` couverts, erreurs agrégées/actionnables, fixtures positives/négatives vertes, aucun catalogue/UI créé.

## 15. Conditions d'arrêt

Ambiguïté de schéma incompatible avec le guide, décision YAML requise, validation permettant un chemin hors contenu ou test négatif en échec.

## 16. Mise à jour attendue de la roadmap

Cocher `02A` seulement ; prochaine fiche `02B`.

## 17. Format obligatoire du rapport final

Schémas ; règles ; fixtures ; commande/exit codes ; tests ; erreurs exemplaires ; sécurité chemin ; décisions ; confirmation d'absence de catalogue/lecteur.
