public static class Submission
{
    public static string ConfigKey(string section, string key)
    {
        if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key)) throw new System.ArgumentException("Clé incomplète."); return $"{section.Trim()}:{key.Trim()}";
    }
}
