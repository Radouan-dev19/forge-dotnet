using System.Linq;
using System.Threading.Tasks;

public static class Submission
{
    public static int SumAsyncResults(int[] values)
    {
        Task<int>[] tasks = values.Select(async value => { await Task.Delay(100); return value; }).ToArray();
        return tasks.Where(task => task.IsCompletedSuccessfully).Sum(task => task.Result);
    }
}
