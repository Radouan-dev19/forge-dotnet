public static class Submission
{
    public static int[] NextGreater(int[] values)
    {
        // Moins un par défaut : les indices sans supérieur à droite garderont cette valeur.
        int[] result = new int[values.Length];
        System.Array.Fill(result, -1);

        // La pile contient les indices encore sans réponse, valeurs décroissantes du fond
        // vers le sommet : le premier supérieur rencontré répond à tout ce qu'il dépasse.
        var stack = new System.Collections.Generic.Stack<int>();

        for (int i = 0; i < values.Length; i++)
        {
            // La valeur courante répond à tous les indices en attente qu'elle dépasse.
            while (stack.Count > 0 && values[i] > values[stack.Peek()])
            {
                result[stack.Pop()] = values[i];
            }

            stack.Push(i);
        }

        return result;
    }
}
