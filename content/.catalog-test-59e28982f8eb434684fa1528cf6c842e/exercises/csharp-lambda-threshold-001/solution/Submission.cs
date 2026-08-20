using System.Linq;

public static class Submission
{
    public static int CountAtLeast(int[] values, int minimum)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Le prédicat est une lambda passée à Count : la borne incluse s'écrit >=.
        return values.Count(value => value >= minimum);
    }
}
