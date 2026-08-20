public static class Submission
{
    public static int ErrorStatus(string kind)
    {
        // Liste blanche : seules les catégories connues reçoivent un statut précis,
        // tout le reste — y compris null — se rabat sur 500 sans détailler.
        return kind?.Trim().ToLowerInvariant() switch
        {
            "validation" => 400,
            "notfound" => 404,
            "conflict" => 409,
            "unauthorized" => 401,
            "forbidden" => 403,
            _ => 500,
        };
    }
}
