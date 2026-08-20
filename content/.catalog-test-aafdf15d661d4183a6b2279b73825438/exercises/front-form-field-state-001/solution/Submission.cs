using System;

public static class Submission
{
    public static string FieldState(string field, string interactions)
    {
        // Des regles ou des interactions absentes ne sont pas un cas vide : on refuse.
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        if (interactions is null)
        {
            throw new ArgumentNullException(nameof(interactions));
        }

        // Lecture des regles du champ en trois indicateurs.
        bool required = false;
        bool optional = false;
        int minimumLength = 0;

        foreach (string token in field.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token == "required")
            {
                required = true;
            }
            else if (token == "optional")
            {
                optional = true;
            }
            else if (token.StartsWith("minlen=", StringComparison.Ordinal))
            {
                // Un seuil illisible est ignore : minimumLength reste a zero, donc inactif.
                if (int.TryParse(token.Substring("minlen=".Length), out int parsed))
                {
                    minimumLength = parsed;
                }
            }
        }

        // La valeur initiale est vide et le champ n'a pas encore ete touche.
        string value = "";
        bool touched = false;

        foreach (string interaction in interactions.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int equals = interaction.IndexOf('=');
            string name = equals < 0 ? interaction : interaction.Substring(0, equals);

            if (name == "input" && equals >= 0)
            {
                // La saisie remplace entierement la valeur courante.
                value = interaction.Substring(equals + 1);
            }
            else if (name == "blur")
            {
                // Quitter le champ le marque comme touche.
                touched = true;
            }
            else if (name == "reset")
            {
                // Reset ramene la valeur initiale ET efface le contact.
                value = "";
                touched = false;
            }

            // focus, comme tout evenement inconnu, ne change rien.
        }

        // La validite se calcule une seule fois, sur la valeur finale.
        bool valid = true;

        // Un champ requis laisse vide est invalide ; optional n'ajoute aucune contrainte.
        if (required && value.Length == 0)
        {
            valid = false;
        }

        // minlen ne concerne qu'une valeur non vide : un champ vide n'est jamais recale par lui.
        if (minimumLength > 0 && value.Length != 0 && value.Length < minimumLength)
        {
            valid = false;
        }

        // Composition des trois axes independants.
        string cleanliness = value.Length != 0 ? "dirty" : "pristine";
        string contact = touched ? "touched" : "untouched";
        string validity = valid ? "valid" : "invalid";

        return cleanliness + "-" + contact + "-" + validity;
    }
}
