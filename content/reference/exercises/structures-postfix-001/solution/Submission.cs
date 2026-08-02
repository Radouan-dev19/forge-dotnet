public static class Submission
{
    public static int EvaluatePostfix(string expression)
    {
        var stack = new System.Collections.Generic.Stack<int>(); foreach (string token in expression.Split(" ", System.StringSplitOptions.RemoveEmptyEntries)) { if (int.TryParse(token, out int value)) stack.Push(value); else { int right = stack.Pop(), left = stack.Pop(); stack.Push(token == "+" ? left + right : left * right); } } return stack.Pop();
    }
}
