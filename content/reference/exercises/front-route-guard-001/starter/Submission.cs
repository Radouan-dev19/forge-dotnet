using System;
using System.Text.Json;

public static class Submission
{
    public static string GuardDecision(string token, string requiredScope, int nowUnix, string currentPath)
    {
        // Decodez le segment du milieu du jeton en base64url puis en JSON, lisez exp et scope,
        // et rangez la situation dans l'une des trois issues : allow, forbidden ou redirect.
        throw new NotImplementedException("Le garde de route reste a ecrire.");
    }
}
