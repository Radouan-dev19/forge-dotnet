public static class Submission
{
    public static string AgeGroup(int age)
    {
        if (age < 0) return "invalid"; if (age < 18) return "minor"; if (age < 65) return "adult"; return "senior";
    }
}
