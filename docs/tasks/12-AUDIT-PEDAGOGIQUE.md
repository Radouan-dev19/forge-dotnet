# Incrément 12 — Audit pédagogique final

## 1. Statut

Non commencé.

## 2. Objectif

Agir comme jury indépendant et contradictoire, attaquer les signaux de maîtrise et corriger seulement les défauts pédagogiques P0/P1 avant verdict final.

## 3. Contexte à lire

Tous les documents, fiches et rapports, particulièrement `PRODUCT_SPEC.md`, `CURRICULUM.md`, `CONTENT_GUIDE.md`, rapport/matrice `11`, puis le produit sans dépendre des intentions des auteurs.

## 4. Prérequis

`11` validé avec 16 critères conformes ; auditeur différent ou posture explicitement indépendante ; environnement et données de test réinitialisables.

## 5. Périmètre inclus

Personas débutant fragile, tricheur, consommateur de solutions, faible SQL, fort quiz/faible pratique, sans Docker, retour après deux semaines ; faux signaux, lacunes, difficulté, ambiguïtés, explications, blocages, dépendance IA ; corrections P0/P1 et backlog priorisé.

## 6. Périmètre explicitement exclu

Complaisance envers l'implémentation, nouvelle vision produit, sujets post-parcours, promesse d'emploi/salaire et correction silencieuse de P2/P3 massive.

## 7. Fichiers ou projets principalement concernés

Produit complet en lecture/audit, tests adversariaux, contenus fautifs ciblés, rapport `docs/PEDAGOGICAL_AUDIT.md` et corrections P0/P1 nécessaires.

## 8. Étapes d'implémentation

Geler version auditée ; définir scripts/personas avant essai ; collecter preuves ; tenter triche/contournement ; mesurer compréhension/charge ; classer P0–P3 ; reproduire ; corriger P0/P1 avec tests ; réauditer ; publier verdict et backlog sans minimiser les défauts.

## 9. Règles d'architecture

L'audit n'altère pas les frontières pour faciliter un scénario ; corrections suivent les modules existants ; toute modification structurelle requiert validation humaine distincte.

## 10. Règles de sécurité

Utiliser données fictives, environnement isolé et runners bornés ; ne pas affaiblir sécurité pour tester ; documenter tout contournement ; préserver solutions/tests cachés.

## 11. Tests à écrire

Régressions pour chaque P0/P1 ; scénarios triche/solution rapide/quiz-only/SQL faible/absence Docker/retour tardif ; vérification seuils, révisions et attributions d'aide.

## 12. Tests manuels à effectuer

Exécuter intégralement les sept personas, entretiens/explications, exercices ambigus, parcours de reprise et mode manuel ; faire relire un échantillon par un humain indépendant si disponible.

## 13. Commandes de vérification

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
# Exécuter la suite adversariale et le validateur complet de contenu.
git diff --check
git status --short
```

## 14. Critères d'acceptation

Tous les personas exécutés avec preuves ; aucun P0/P1 ouvert ; faux signaux majeurs corrigés ; backlog P2/P3 priorisé ; verdict pouvant être « refusé » ; suite complète verte.

## 15. Conditions d'arrêt

Audit non indépendant, persona non exécutable, P0/P1 non corrigé, score contournable, contenu critique ambigu, sécurité affaiblie ou pression pour déclarer succès sans preuve.

## 16. Mise à jour attendue de la roadmap

Cocher `12` uniquement après verdict indépendant favorable et P0/P1 clos ; sinon laisser ouvert et documenter le refus. Aucun incrément suivant n'est créé implicitement.

## 17. Format obligatoire du rapport final

Version auditée ; méthode/personas ; preuves ; défauts P0–P3 ; corrections et tests ; verdict motivé (accepté/refusé) ; backlog ; risques résiduels ; déclaration d'indépendance et absence de promesse.
