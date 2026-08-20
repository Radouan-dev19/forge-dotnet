using System;

public static class Submission
{
    // Compte les appels reseau declenches par un effet sur une suite de rendus (une cle par rendu).
    public static int FetchCount(string keys)
    {
        string[] renders = Split(keys);
        if (renders.Length == 0)
        {
            return 0;
        }

        int fetches = 1;
        for (int index = 1; index < renders.Length; index++)
        {
            if (renders[index] != renders[index - 1])
            {
                fetches++;
            }
        }

        return fetches;
    }

    private static string[] Split(string keys) =>
        keys.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
