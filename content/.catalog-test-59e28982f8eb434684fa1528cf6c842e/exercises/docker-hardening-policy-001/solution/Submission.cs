public static class Submission
{
    public static bool IsHardened(bool nonRoot, bool readOnly, bool noNewPrivileges)
    {
        return nonRoot && readOnly && noNewPrivileges;
    }
}
