# Skills et commandes : encapsuler vos procédures

Quand vous tapez la même explication à votre assistant pour la troisième fois, vous avez identifié
un skill qui manque. Un skill — commande personnalisée, selon le vocabulaire de l'outil — est une
procédure nommée, versionnée dans le dépôt, que l'assistant charge à la demande : vous invoquez un
nom court, il déroule la méthode complète.

## Ce qu'un skill est, et ce qu'il n'est pas

C'est la différence entre demander et prescrire. Un prompt dit « relis ce fichier » et laisse le
modèle décider ce que relire veut dire ; un skill de revue dit dans quel ordre examiner, quelles
catégories de défauts chercher, quel format de constat rendre, et quand s'arrêter. Le skill capture
votre **méthode**, pas votre intention du moment. Il n'est pas non plus de la documentation : un
document explique à un humain, un skill instruit un exécutant — impératif, ordonné, avec des
critères de fin vérifiables.

## L'anatomie d'un bon skill

Cinq parties, quel que soit l'outil :

1. **Le nom et le déclencheur** — court, mémorisable, sans ambiguïté avec un autre skill.
2. **Le contexte d'application** — sur quoi il opère (les fichiers modifiés, un dossier donné, un
   ticket) et ce qu'il doit lire avant d'agir.
3. **La procédure** — les étapes numérotées, dans l'ordre réel, avec les commandes exactes du
   projet. C'est ici que meurt le skill vague : « vérifie la qualité » ne s'exécute pas, « lance la
   suite de tests, puis le vérificateur de format, et refuse de conclure si l'un échoue » s'exécute.
4. **Les interdits** — ce que le skill ne fait jamais : toucher à tel dossier, commiter, relâcher un
   seuil. Les interdits explicites valent mieux que la confiance.
5. **Le format de restitution** — à quoi ressemble une exécution réussie, pour que vous la
   vérifiiez en trois secondes.

## Trois skills qui rentabilisent un poste de développeur

**La revue de pré-commit.** Avant chaque commit : relire le diff, chercher les catégories que votre
équipe rate le plus (gestion d'erreurs absente, cas limites, secrets), exécuter les tests concernés,
rendre les constats triés par sévérité — avec la discipline apprise dans la piste senior de ce
parcours : un constat prouvé pèse plus qu'une préférence, et le dire dans le constat.

**Le rituel de reprise.** En début de session : lire l'état du dépôt, les derniers commits, la liste
des tests rouges s'il y en a, et produire un résumé de cinq lignes de « où on en est ». Deux minutes
de machine qui remplacent vingt minutes de réacclimatation humaine.

**Le gabarit de commit et de PR.** Formater le message selon la convention du dépôt, lister ce qui a
été vérifié et comment, refuser de rédiger si les tests n'ont pas tourné. Le skill devient le
gardien de la convention — plus fiable qu'une bonne résolution.

## Skill, prompt ou fichier de consignes ?

Trois questions tranchent. **La consigne s'applique-t-elle toujours ?** Fichier de consignes du
dépôt. **S'applique-t-elle à la demande, avec une méthode en plusieurs étapes ?** Skill. **Est-elle
propre à l'instant ?** Prompt, et il n'a pas vocation à survivre. Le piège inverse existe : un skill
pour tout, y compris ce qu'une phrase ferait — chaque skill a un coût d'entretien, et un catalogue
de quarante skills dont trente sont périmés vaut moins que huit skills exacts.

## L'entretien, qui décide de tout

Un skill est du code : versionnez-le avec le dépôt, relisez-le en revue, datez ses hypothèses. Quand
une commande du projet change, le skill qui la cite ment — et un skill qui ment est pire que pas de
skill, car il échoue avec assurance. Le test d'un bon skill est le même que celui d'un bon script :
un nouveau venu l'exécute sans vous poser de question, et le résultat se vérifie sans vous. Ce
dépôt suit d'ailleurs cette logique pour lui-même : ses procédures vivent dans des scripts et des
documents versionnés, jamais dans la mémoire de quelqu'un — faites subir le même sort à vos usages
d'assistant.
