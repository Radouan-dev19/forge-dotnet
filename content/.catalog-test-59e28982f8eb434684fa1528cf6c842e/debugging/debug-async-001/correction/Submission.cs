using System.Linq;
using System.Threading.Tasks;

public static class Submission
{
    public static int SumAsyncResults(int[] values)
    {
        Task<int>[] tasks = values.Select(async value => { await Task.Delay(1); return value; }).ToArray();
        return Task.WhenAll(tasks).GetAwaiter().GetResult().Sum();
    }
}
