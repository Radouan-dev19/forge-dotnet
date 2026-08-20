using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string Reduce(string state, string actions)
    {
        // Un etat ou des actions absents ne sont pas un etat vide : on refuse explicitement.
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (actions is null)
        {
            throw new ArgumentNullException(nameof(actions));
        }

        // Nouvelle table, comparaison ordinale : on ne touchera jamais a l'entree recue.
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        // Lecture de l'etat : chaque paire se coupe sur son PREMIER egal, la derniere valeur gagne.
        foreach (string pair in state.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int split = pair.IndexOf('=');

            // Un segment sans egal n'est pas une paire valide : on l'ignore.
            if (split < 0)
            {
                continue;
            }

            map[pair.Substring(0, split)] = pair.Substring(split + 1);
        }

        // Rejeu des actions, verbe par verbe, sur la copie uniquement.
        foreach (string segment in actions.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = segment.IndexOf(':');

            // Un segment sans deux-points n'est pas une action : on l'ignore sans echouer.
            if (colon < 0)
            {
                continue;
            }

            string verb = segment.Substring(0, colon);
            string args = segment.Substring(colon + 1);

            if (verb == "set")
            {
                int equals = args.IndexOf('=');

                // Sans egal dans les arguments, il n'y a rien a poser : action ignoree.
                if (equals < 0)
                {
                    continue;
                }

                map[args.Substring(0, equals)] = args.Substring(equals + 1);
            }
            else if (verb == "del")
            {
                // Retire la cle si presente ; Remove ne fait rien quand elle est absente.
                map.Remove(args);
            }
            else if (verb == "inc")
            {
                if (!map.TryGetValue(args, out string? current))
                {
                    // Cle absente : on part de zero, l'increment vaut donc un.
                    map[args] = "1";
                }
                else if (int.TryParse(current, out int number))
                {
                    // Valeur entiere valide : on ecrit l'increment.
                    map[args] = (number + 1).ToString();
                }

                // Valeur presente mais non entiere : aucune branche, l'action est abandonnee.
            }

            // Tout autre verbe est inconnu : on ne fait rien, comme le veut la regle.
        }

        // Tri ordinal des cles pour une sortie stable, independante de la culture courante.
        List<string> keys = map.Keys.ToList();
        keys.Sort(StringComparer.Ordinal);

        // Reassemblage sous la forme cle=valeur jointe par des points-virgules.
        return string.Join(";", keys.Select(key => key + "=" + map[key]));
    }
}
