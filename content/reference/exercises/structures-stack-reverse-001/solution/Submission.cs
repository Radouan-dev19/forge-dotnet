public static class Submission
{
    public static string ReverseText(string text)
    {
        if (text is null) throw new System.ArgumentNullException(nameof(text)); var stack = new System.Collections.Generic.Stack<char>(text); return new string(stack.ToArray());
    }
}
