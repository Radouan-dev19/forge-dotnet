public static class Submission
{
    public static string Reverse(string text)
    {
        var stack = new System.Collections.Generic.Stack<char>(text); return new string(stack.ToArray());
    }
}
