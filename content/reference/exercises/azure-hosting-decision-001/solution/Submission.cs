public static class Submission
{
    public static string HostingChoice(bool requiresContainerRevisions, bool alreadyHasContainer)
    {
        return requiresContainerRevisions && alreadyHasContainer ? "container-apps" : "app-service";
    }
}
