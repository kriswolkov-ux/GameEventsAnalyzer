using System.Diagnostics;

namespace GameEventsAnalyzer;

public static class BenchmarkRunner
{
    public static void Compare(
        List<GameEvent> events,
        int repetitions = 5)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (repetitions < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(repetitions));
        }

        GameEventAnalyzer.AnalyzeWithLinq(events);
        GameEventAnalyzer.AnalyzeWithPlinq(events);

        var linqTimes = new List<double>();
        var plinqTimes = new List<double>();

        for (int i = 0; i < repetitions; i++)
        {
            var linqTime = Measure(
                () => GameEventAnalyzer.AnalyzeWithLinq(events));

            var plinqTime = Measure(
                () => GameEventAnalyzer.AnalyzeWithPlinq(events));

            if (!linqTime.Result.SequenceEqual(plinqTime.Result))
            {
                throw new InvalidOperationException(
                    "Результаты LINQ и PLINQ не совпадают.");
            }

            linqTimes.Add(linqTime.ElapsedMilliseconds);
            plinqTimes.Add(plinqTime.ElapsedMilliseconds);
        }

        Console.WriteLine($"LINQ:  {Median(linqTimes):F3} мс");
        Console.WriteLine($"PLINQ: {Median(plinqTimes):F3} мс");
    }

    private static (List<PlayerStatistics> Result, double ElapsedMilliseconds)
        Measure(Func<List<PlayerStatistics>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = action();
        stopwatch.Stop();

        return (result, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static double Median(List<double> values)
    {
        double[] sorted = values.OrderBy(x => x).ToArray();
        int middle = sorted.Length / 2;

        if (sorted.Length % 2 == 1)
        {
            return sorted[middle];
        }

        return (sorted[middle - 1] + sorted[middle]) / 2;
    }
}
