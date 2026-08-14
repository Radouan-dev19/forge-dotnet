public static class Submission
{
    public static string ReverseText(string text)
    {
        if (text is null)
        {
            throw new System.ArgumentNullException(nameof(text));
        }

        // Le constructeur empile chaque caractère ; ToArray restitue du sommet vers le
        // fond, donc déjà dans l'ordre inverse — c'est la nature même de la pile.
        var stack = new System.Collections.Generic.Stack<char>(text);
        return new string(stack.ToArray());
    }
}
