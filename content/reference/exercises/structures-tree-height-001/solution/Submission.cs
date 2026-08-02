public static class Submission
{
    public static int TreeHeight(int[] parents)
    {
        int height = 0; for (int node = 0; node < parents.Length; node++) { int depth = 1, current = node, guard = 0; while (parents[current] != -1) { current = parents[current]; depth++; if (++guard > parents.Length) return -1; } if (depth > height) height = depth; } return height;
    }
}
