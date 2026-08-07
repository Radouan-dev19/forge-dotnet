public static class Submission
{
    public static string IncidentBriefStatus(bool impactStated, bool evidenceCited, bool nextStepOwned)
    {
        return impactStated && evidenceCited && nextStepOwned ? "ready" : "incomplete";
    }
}
