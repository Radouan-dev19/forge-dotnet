# Choisir une source de valeur sensible

Implémentez Submission.SensitiveValueSource avec la signature fournie. Une valeur non sensible reste en configuration ; une valeur sensible utilise lʼidentité gérée ou un magasin local hors Git.

Le résultat reste déterministe et hors ligne : aucun abonnement, appel Azure ou identifiant réel nʼest nécessaire. Avant le code, écrivez un cas nominal, une frontière, un refus et le risque que ces preuves réduisent.

Exemple : entrée $(Convert-JsonCompact System.Object[] System.Object[][0]), sortie $(Convert-JsonCompact System.Object[] System.Object[][1]).
