# Cartes de révision

## card-api-webhook-signature-001-rule

**Question :** Sur quelle chaîne exacte se recalcule la signature d'un webhook, et pourquoi
inclure l'horodatage ?  
**Réponse attendue :** Sur l'horodatage et le corps brut joints par un point ; lier l'horodatage
empêche de recoller un vieux corps signé à un horodatage frais et prépare le contrôle anti-rejeu.

## card-api-webhook-signature-001-edge

**Question :** Pourquoi vérifier la signature sur le corps brut plutôt que re-sérialisé ?  
**Réponse attendue :** La re-sérialisation réordonne les clés et normalise les espaces, produisant
un corps différent de celui signé ; la signature échoue alors même sur un envoi authentique.
