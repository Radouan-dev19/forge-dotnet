public static class Submission
{
    public static int AncestorCount(int[] parents, int node)
    {
        if (node < 0 || node >= parents.Length) return -1; int count = 0, current = node; while (parents[current] != -1) { current = parents[current]; if (current < 0 || current >= parents.Length || ++count > parents.Length) return -1; } return count;
    }
}
