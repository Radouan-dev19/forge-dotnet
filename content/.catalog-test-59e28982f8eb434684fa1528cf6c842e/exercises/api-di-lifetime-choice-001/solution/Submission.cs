public static class Submission
{
    public static string LifetimeFor(bool holdsRequestState, bool statelessShared)
    {
        if (holdsRequestState) return "scoped"; return statelessShared ? "singleton" : "transient";
    }
}
