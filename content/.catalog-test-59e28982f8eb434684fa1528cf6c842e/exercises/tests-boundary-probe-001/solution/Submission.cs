using System;
using System.Collections.Generic;

public static class Submission
{
    public static int[] BoundaryProbes(int low, int high)
    {
        if (low > high)
        {
            throw new ArgumentException(
                "Un intervalle vide n'a aucune frontière à sonder.", nameof(low));
        }

        // Les sondes sont produites dans l'ordre croissant par construction : la sonde extérieure
        // basse précède la borne basse, qui précède la borne haute, qui précède la sonde haute.
        var probes = new List<int>(4);

        // Une borne posée sur la limite du type est déjà une frontière : il n'existe aucune valeur
        // au-delà, donc aucune sonde extérieure à produire de ce côté.
        if (low > int.MinValue)
        {
            probes.Add(low - 1);
        }

        probes.Add(low);

        // Deux bornes confondues décrivent la même valeur : la sonder deux fois ne réfute rien de plus.
        if (high != low)
        {
            probes.Add(high);
        }

        if (high < int.MaxValue)
        {
            probes.Add(high + 1);
        }

        return probes.ToArray();
    }
}
