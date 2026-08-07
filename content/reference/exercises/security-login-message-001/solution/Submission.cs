public static class Submission
{
    public static string LoginFailure(bool userExists, bool proofValid)
    {
        _ = userExists; _ = proofValid; return "Identifiants invalides.";
    }
}
