using System;
using System.Collections.Generic;

public static class Submission
{
    public static string ResponseContract(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            throw new ArgumentException("Une requête vide n'a pas de contrat de réponse.", nameof(request));
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in request.Split(';'))
        {
            string[] parts = pair.Split('=');
            if (parts.Length != 2 || !attributes.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un attribut de la requête est illisible ou répété.", nameof(request));
            }
        }

        if (attributes.Count != 3)
        {
            throw new ArgumentException("La requête se décrit par exactement trois attributs.", nameof(request));
        }

        string method = ValueOf(attributes, "method", "get", "post", "put", "delete");
        string state = ValueOf(attributes, "state", "present", "absent", "gone", "moved");
        string load = ValueOf(attributes, "load", "normal", "throttled");

        // L'étranglement court-circuite tout : la requête n'a été ni examinée ni exécutée, et le
        // refus sans délai de retour fabriquerait une tempête de relances.
        if (load == "throttled")
        {
            return "429|Retry-After";
        }

        // La redirection historique autorise le client à dégrader la méthode vers une lecture :
        // seule la lecture la supporte, les écritures exigent la redirection qui préserve la méthode.
        if (state == "moved")
        {
            return method == "get" ? "301|Location" : "308|Location";
        }

        // La pierre tombale dit « n'insistez plus », là où l'absence laisse espérer un retour.
        if (state == "gone")
        {
            return "410";
        }

        return (method, state) switch
        {
            ("get", "present") => "200",
            ("get", "absent") => "404",
            ("put", "present") => "204",
            ("put", "absent") => "201|Location",
            ("delete", "present") => "204",
            ("delete", "absent") => "404",
            ("post", "present") => "409",
            _ => "201|Location",
        };
    }

    private static string ValueOf(Dictionary<string, string> attributes, string key, params string[] allowed)
    {
        if (!attributes.TryGetValue(key, out string? value) || Array.IndexOf(allowed, value) < 0)
        {
            throw new ArgumentException("Un attribut de la requête manque ou sort du vocabulaire.", nameof(attributes));
        }

        return value;
    }
}
