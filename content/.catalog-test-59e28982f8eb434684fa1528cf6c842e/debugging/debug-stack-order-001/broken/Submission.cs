public static class Submission
{
    public static string Reverse(string text)
    {
        var stack = new System.Collections.Generic.Stack<char>(text); char[] result = stack.ToArray(); System.Array.Reverse(result); return new string(result);
    }
}
