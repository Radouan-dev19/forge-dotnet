public static class Submission
{
    public static string CostGuardrail(
        decimal estimatedDailyCost,
        decimal dailyBudget,
        bool deletionPlanReady)
    {
        // Un coût négatif ou un budget non positif ne décrivent aucune situation réelle.
        if (estimatedDailyCost < 0 || dailyBudget <= 0)
        {
            throw new System.ArgumentOutOfRangeException();
        }

        // Sans plan de suppression, rien ne se crée — même gratuit en apparence.
        if (!deletionPlanReady)
        {
            return "block";
        }

        // Le coût estimé se compare au budget, borne incluse : au budget exact, ça passe.
        return estimatedDailyCost <= dailyBudget ? "allow" : "block";
    }
}
