public static class Submission
{
    public static int FinalState(int[] events)
    {
        int state = 0; foreach (int value in events) { if (value == 1 && state == 0) state = 1; else if (value == 2 && state == 1) state = 2; else if (value == 3) state = 0; } return state;
    }
}
