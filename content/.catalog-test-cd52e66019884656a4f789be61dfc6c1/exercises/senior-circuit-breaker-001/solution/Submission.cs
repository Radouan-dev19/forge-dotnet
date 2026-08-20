using System;

public static class Submission
{
    public static string CircuitState(string events)
    {
        // Une trace nulle est une absence d'information, pas une trace vide : on la refuse.
        ArgumentNullException.ThrowIfNull(events);

        string state = "closed";
        int fails = 0;

        // Une seule passe suffit : l'etat resume tout le passe pertinent de la trace.
        foreach (string token in events.Split(';'))
        {
            if (token == "fail")
            {
                if (state == "half-open")
                {
                    // L'essai probatoire a echoue : on rouvre sans attendre le seuil.
                    state = "open";
                    fails = 0;
                }
                else if (state == "closed")
                {
                    fails++;
                    if (fails >= 3)
                    {
                        // Trois echecs consecutifs : le circuit se coupe.
                        state = "open";
                        fails = 0;
                    }
                }
            }
            else if (token == "ok")
            {
                if (state == "half-open")
                {
                    // L'essai probatoire a reussi : le service est retabli, on referme.
                    state = "closed";
                    fails = 0;
                }
                else if (state == "closed")
                {
                    // Un succes efface les echecs isoles accumules.
                    fails = 0;
                }
            }
            else if (token == "tick")
            {
                if (state == "open")
                {
                    // Le temps ecoule autorise un unique essai probatoire.
                    state = "half-open";
                }
            }

            // Tout autre jeton est ignore : il ne fait pas evoluer l'automate.
        }

        return state;
    }
}
