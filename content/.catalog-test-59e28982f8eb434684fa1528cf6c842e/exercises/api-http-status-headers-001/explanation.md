# Explication

Le réflexe qui consiste à choisir « le bon statut » manque la moitié du contrat HTTP : plusieurs
statuts n'ont de sens qu'accompagnés d'un en-tête, et les servir nus produit des clients qui
devinent. Cet exercice force à raisonner en contrat complet — statut plus en-têtes obligatoires — et
chacun de ses étages encode une leçon que les intégrations réelles paient cher quand elle manque.

**Pourquoi l'étranglement prime sur tout.** Une requête refusée pour cause de charge n'a été ni
examinée ni exécutée : répondre selon la méthode ou l'état de la ressource prétendrait le contraire.
Et le refus nu est pire qu'inutile — un client qui reçoit un refus sans délai réessaie immédiatement,
en chœur avec tous les autres, et transforme la pointe de charge en tempête de relances. L'en-tête de
délai est ce qui convertit un refus en instruction : revenez dans tant de secondes. C'est le même
principe que la fenêtre de recul des chaînes d'intégration, exprimé cette fois par le serveur.

**Pourquoi la redirection distingue la lecture des écritures.** La redirection historique a un vice
connu : beaucoup de clients la suivent en dégradant la méthode vers une lecture. Pour un `get`, aucun
dégât ; pour un `put` ou un `post`, l'écriture disparaît en route, silencieusement. La redirection
permanente qui préserve la méthode existe précisément pour cela, et le tableau la réserve aux
méthodes qui écrivent. Servir la version historique à une écriture est le genre de choix qui
fonctionne avec le client de test et casse avec le client du partenaire.

**Pourquoi la pierre tombale n'est pas une absence.** L'absence dit « rien ici, peut-être un jour » ;
la disparition définitive dit « n'insistez plus, purgez vos références ». Un consommateur de flux, un
moteur d'indexation ou un cache réagissent différemment aux deux — l'un réessaie, l'autre nettoie.
Fondre les deux dans l'absence condamne les clients à réessayer pour toujours ce qui ne reviendra
jamais.

**Pourquoi la création porte une adresse.** Le statut de création annonce une ressource nouvelle ;
sans son adresse, le client doit la retrouver par ses propres moyens — une recherche, une convention
d'identifiant, une devinette. L'en-tête d'adresse clôt la transaction : voici ce que vous avez créé,
voici où. Le conflit sur une représentation déjà existante est son miroir : la création n'a pas eu
lieu, et le dire par un succès serait mentir sur l'état du monde.

**Pourquoi les attributs arrivent en désordre et les refus sont stricts.** La description de requête
vient d'outils variés ; exiger un ordre fabriquerait des faux rejets. En revanche, une clé répétée ou
une valeur hors vocabulaire ne se devine pas : un contrat de réponse construit sur une requête mal
décrite serait faux avec assurance, ce qui est la pire combinaison.

La transposition est directe : chaque contrôleur d'une vraie interface applique ce tableau, et les
revues d'interface gagnent à le demander écrit — la case vide se voit mieux dans un tableau que dans
un débogueur.
