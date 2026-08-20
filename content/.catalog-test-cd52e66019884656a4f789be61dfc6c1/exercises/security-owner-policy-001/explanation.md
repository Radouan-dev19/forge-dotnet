# Explication

Peut-il modifier *cette* ressource ? La question contient le mot qui fait tout l'exercice :
« cette ». L'énoncé demande ce qu'un contrôle limité à l'action laisserait faire en changeant
un identifiant d'adresse — et la réponse porte un nom dans les classements de vulnérabilités :
la référence directe non sécurisée, première cause de fuite de données des API réelles. Un
utilisateur légitimement autorisé à modifier *ses* commandes incrémente l'identifiant dans
l'adresse et modifie celles du voisin : le contrôle « a le droit de modifier des commandes »
était vrai, le contrôle « est propriétaire de *celle-ci* » n'existait pas. L'autorisation a
deux étages — l'action et la ressource — et cette fonction est l'étage que les applications
oublient.

La règle composée se lit dans l'ordre des gardes. Le privilège d'administration s'évalue en
premier et passe outre : c'est un choix de politique — l'administrateur intervient sur les
ressources d'autrui, c'est sa fonction — et sa position en tête le rend lisible et testable
séparément. Vient ensuite la garde des identités absentes, et sa position *avant* l'égalité
n'est pas décorative : deux identités blanches sont égales entre elles, et sans cette garde,
un acteur sans identité pourrait modifier une ressource sans propriétaire — l'autorisation par
le néant, le genre de trou qui ne se voit que dans le cas croisé que les cachés posent.
Refuser quand l'un des deux côtés manque suit le principe des politiques d'accès : dans le
doute, non — l'échec d'une vérification n'ouvre jamais.

L'égalité finale est *ordinale et sensible à la casse* : les identifiants d'utilisateurs sont
des clés techniques, et une comparaison qui fusionnerait `u1` et `U1` accorderait à un compte
les ressources d'un autre sur une simple collision de casse. C'est le même raisonnement que
pour les audiences des jetons — la tolérance de casse, confort ailleurs, est ici une faille.

Les cas suivent l'énoncé : le propriétaire qui passe, le tiers refusé, l'administrateur qui
passe outre — y compris sur la ressource d'autrui —, et les identités absentes refusées.

Le coût est constant. La transposition est l'architecture de l'autorisation à deux étages :
le premier — rôle, portée — se vérifie au middleware, déclarativement ; le second — la
propriété — exige la ressource, donc se vérifie *dans* le gestionnaire, après chargement, par
une politique comme celle-ci. La revue de code d'une API se fait avec cette grille : pour
chaque route qui prend un identifiant, où est le contrôle de propriété ? S'il n'y a que
l'attribut d'autorisation, l'étage manque.
