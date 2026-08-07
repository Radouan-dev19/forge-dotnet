# Diagnostiquer performance et sécurité sans masquer la cause

## Objectif observable

À la fin de cette leçon, vous pourrez prendre une décision sur un cas nouveau, la relier à une contrainte explicite et produire une preuve qui réfute une option plausible mais incorrecte.

## Prérequis

Relire $previousLessonId. Tout exercice principal fonctionne hors ligne ; lorsquʼAzure est évoqué, le mode simulé est la preuve de référence et un compte cloud reste facultatif.

## Intuition

Mesurer avant dʼoptimiser : reproduire, borner lʼimpact, comparer une métrique, corriger, puis prouver la non-régression. Un incident de sécurité conserve les preuves sans recopier de donnée sensible.

## Explication

Mesurer avant dʼoptimiser : reproduire, borner lʼimpact, comparer une métrique, corriger, puis prouver la non-régression. Un incident de sécurité conserve les preuves sans recopier de donnée sensible. Écrivez dʼabord le contrat, les entrées non fiables, la responsabilité exacte, les coûts possibles et la méthode de suppression. Une étape manuelle reste annoncée comme manuelle.

## Exemple commenté

Une lecture lente est reliée à un volume et une requête ; le correctif est validé par un budget de latence et un contrôle dʼautorisation inchangé. Faites varier une contrainte et expliquez pourquoi le choix pourrait alors changer.

## Contre-exemple et erreur fréquente

Désactiver un contrôle ou augmenter tous les timeouts peut réduire un symptôme tout en aggravant le risque. Reproduisez le défaut avec une preuve locale avant de proposer une correction.

## Vérification de compréhension

Nommez le besoin, une option écartée, un risque de sécurité ou de coût, puis une preuve observable qui distingue les deux options.

:::quiz
id=performance-security-incident-001-check
question=Quelle preuve démontre le mieux la compréhension de cette leçon ?
option=Copier uniquement lʼexemple nominal
option=Prédire puis tester succès, frontière et refus sans ressource externe ni donnée sensible
option=Présenter une inspection manuelle comme une validation automatique
correct=1
success=Correct : une preuve variée, locale et bornée réfute les erreurs plausibles.
retry=Revenez au besoin, aux contraintes, aux limites et au contre-exemple avant de choisir.
:::

## Exercice guidé

Analysez le cas fourni, remplissez la matrice besoin / option / compromis / preuve et exécutez le contrôle simulé associé. Aucun compte Azure nʼest requis.

## Exercice autonome

Changez une hypothèse fonctionnelle ou opérationnelle. Prenez une nouvelle décision, écrivez sa limite et proposez une commande PowerShell reproductible qui ne crée aucune ressource payante.

## Débogage

Partez du contre-exemple, observez le symptôme sans donnée sensible, localisez la première hypothèse fausse et ajoutez une preuve de non-régression.

## Entretien

Expliquez en trois minutes la décision, son coût, sa frontière de sécurité et la donnée qui vous ferait changer dʼavis. Distinguez observation, hypothèse et fait vérifié.

## Résumé

- Le besoin gouverne le service ou la technique.
- Les coûts, droits et données sont bornés avant lʼexécution.
- Une preuve locale reste disponible sans abonnement cloud.
- Une limite explicitée vaut mieux quʼune promesse non vérifiable.

## Cartes de révision

1. Quelle contrainte justifie le choix ?
2. Quelle preuve locale réfute lʼoption incorrecte ?
3. Quelle donnée ne doit jamais entrer dans les logs ou le dépôt ?

## Test de maîtrise

Sans relire la leçon, traitez un scénario différent, produisez la matrice de décision et une preuve locale. Si une solution ou réponse modèle a été consultée, expliquez-la avec vos mots et planifiez une reprise à blanc : cette tentative nʼest pas maîtrisée.
