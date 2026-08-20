using System;

public static class Submission
{
    public static string ChooseFlow(string clientProfile)
    {
        if (string.IsNullOrWhiteSpace(clientProfile))
        {
            return "invalid-profile";
        }

        // Compteurs par axe : chaque axe doit finir à exactement un exemplaire.
        int userPresent = 0;
        int machineOnly = 0;
        int confidential = 0;
        int publicClient = 0;

        var options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        foreach (string segment in clientProfile.Split(',', options))
        {
            switch (segment.ToLowerInvariant())
            {
                case "user-present":
                    userPresent++;
                    break;
                case "machine-only":
                    machineOnly++;
                    break;
                case "confidential":
                    confidential++;
                    break;
                case "public":
                    publicClient++;
                    break;
                default:
                    // Une étiquette inconnue invalide tout le profil : on ne devine pas.
                    return "invalid-profile";
            }
        }

        // Exactement une étiquette par axe : ni absence, ni doublon, ni contradiction.
        if (userPresent + machineOnly != 1 || confidential + publicClient != 1)
        {
            return "invalid-profile";
        }

        // Un humain présent : code d'autorisation avec preuve, public ou confidentiel.
        if (userPresent == 1)
        {
            return "authorization-code-pkce";
        }

        // Machine à machine : les identifiants client exigent un secret gardable.
        return confidential == 1 ? "client-credentials" : "refused";
    }
}
