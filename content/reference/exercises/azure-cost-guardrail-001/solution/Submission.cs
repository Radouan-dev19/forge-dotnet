public static class Submission
{
    public static string CostGuardrail(decimal estimatedDailyCost, decimal dailyBudget, bool deletionPlanReady)
    {
        if (estimatedDailyCost < 0 || dailyBudget <= 0) throw new System.ArgumentOutOfRangeException(); if (!deletionPlanReady) return "block"; return estimatedDailyCost <= dailyBudget ? "allow" : "block";
    }
}
