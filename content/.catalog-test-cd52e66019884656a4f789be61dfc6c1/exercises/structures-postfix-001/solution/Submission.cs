public static class Submission
{
    public static int EvaluatePostfix(string expression)
    {
        var stack = new System.Collections.Generic.Stack<int>();

        foreach (string token in expression.Split(" ", System.StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(token, out int value))
            {
                // Un nombre attend son opérateur sur la pile.
                stack.Push(value);
            }
            else
            {
                // L'ordre du dépilage n'est pas symétrique : le sommet est l'opérande DROIT.
                int right = stack.Pop();
                int left = stack.Pop();

                // Contrat pédagogique : deux opérateurs seulement, plus et multiplication.
                stack.Push(token == "+" ? left + right : left * right);
            }
        }

        // L'expression bien formée laisse exactement son résultat sur la pile.
        return stack.Pop();
    }
}
