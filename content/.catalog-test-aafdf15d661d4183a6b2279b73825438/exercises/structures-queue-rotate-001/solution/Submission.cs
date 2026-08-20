public static class Submission
{
    public static int[] RotateQueue(int[] values, int count)
    {
        // La longueur sert de diviseur : le cas vide sort avant la normalisation.
        if (values.Length == 0)
        {
            return System.Array.Empty<int>();
        }

        // La file se construit depuis l'entrée : l'ordre FIFO initial est celui du tableau.
        var queue = new System.Collections.Generic.Queue<int>(values);

        // Repli du compte dans [0, n[ : les rotations négatives et les tours complets
        // se normalisent avant de toucher la file.
        int turns = ((count % values.Length) + values.Length) % values.Length;

        for (int i = 0; i < turns; i++)
        {
            // Une rotation : la tête sort et se replace en queue.
            queue.Enqueue(queue.Dequeue());
        }

        return queue.ToArray();
    }
}
