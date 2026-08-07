public static class Submission
{
    public static bool IsCommitSubjectValid(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return false; string value = subject.Trim(); return value.Length <= 72 && !value.EndsWith(".", System.StringComparison.Ordinal);
    }
}
