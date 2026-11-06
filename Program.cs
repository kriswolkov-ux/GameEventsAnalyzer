namespace GameEventsAnalyzer;

internal class Program
{
    static void Main()
    {
        Console.WriteLine("=== Game Events Analyzer ===");

        RunChecks();
        RunMainAnalysis();
        RunSideEffectDemo();
        RunBenchmarks();

        Console.WriteLine();
        Console.WriteLine(
            "PLINQ не обязательно быстрее LINQ: фильтрация и группировка простые,");
        Console.WriteLine(
            "а распределение работы и объединение результатов тоже требуют времени.");

        Console.WriteLine("Работа завершена.");
    }

    private static void RunChecks()
    {
        Console.WriteLine();
        Console.WriteLine("=== ПРОВЕРКИ ===");

        int failed = 0;

        var testEvents = new List<GameEvent>
        {
            new(1, 1, "Kill", 100, DateTime.Now),
            new(2, 1, "Kill", 150, DateTime.Now),
            new(3, 2, "Kill", 200, DateTime.Now),
            new(4, 1, "Death", 0, DateTime.Now),
            new(5, 2, "Loot", 20, DateTime.Now)
        };

        var linq = GameEventAnalyzer.AnalyzeWithLinq(testEvents);
        var plinq = GameEventAnalyzer.AnalyzeWithPlinq(testEvents);

        bool controlOk =
            linq.Count == 2 &&
            linq[0] == new PlayerStatistics(1, 2, 250) &&
            linq[1] == new PlayerStatistics(2, 1, 200) &&
            linq.SequenceEqual(plinq);

        Report("контрольный сценарий (игрок 1: 2/250, игрок 2: 1/200)", controlOk, ref failed);

        var empty = new List<GameEvent>();

        Report(
            "пустая коллекция",
            GameEventAnalyzer.AnalyzeWithLinq(empty).Count == 0 &&
            GameEventAnalyzer.AnalyzeWithPlinq(empty).Count == 0,
            ref failed);

        var noKills = new List<GameEvent>
        {
            new(1, 1, "Death", 0, DateTime.Now),
            new(2, 2, "Loot", 20, DateTime.Now)
        };

        Report(
            "нет событий Kill",
            GameEventAnalyzer.AnalyzeWithPlinq(noKills).Count == 0,
            ref failed);

        var onePlayer = new List<GameEvent>
        {
            new(1, 7, "Kill", 10, DateTime.Now),
            new(2, 7, "Kill", 20, DateTime.Now),
            new(3, 7, "Kill", 30, DateTime.Now)
        };

        var onePlayerResult =
            GameEventAnalyzer.AnalyzeWithPlinq(onePlayer);

        Report(
            "все убийства одного игрока",
            onePlayerResult.Count == 1 &&
            onePlayerResult[0].KillCount == 3 &&
            onePlayerResult[0].TotalPoints == 60,
            ref failed);

        bool nullLinq = false;
        bool nullPlinq = false;

        try
        {
            GameEventAnalyzer.AnalyzeWithLinq(null!);
        }
        catch (ArgumentNullException)
        {
            nullLinq = true;
        }

        try
        {
            GameEventAnalyzer.AnalyzeWithPlinq(null!);
        }
        catch (ArgumentNullException)
        {
            nullPlinq = true;
        }

        Report("null: ArgumentNullException", nullLinq && nullPlinq, ref failed);

        var equalPoints = new List<GameEvent>
        {
            new(1, 3, "Kill", 100, DateTime.Now),
            new(2, 1, "Kill", 100, DateTime.Now)
        };

        var equalResult =
            GameEventAnalyzer.AnalyzeWithPlinq(equalPoints);

        Report(
            "равные очки: порядок по PlayerId",
            equalResult.Count == 2 &&
            equalResult[0].PlayerId == 1 &&
            equalResult[1].PlayerId == 3,
            ref failed);

        Console.WriteLine(
            failed == 0
                ? "Все проверки пройдены."
                : $"Провалено проверок: {failed}");
    }

    private static void Report(string name, bool ok, ref int failed)
    {
        Console.WriteLine($"{name}: {(ok ? "OK" : "FAIL")}");

        if (!ok)
        {
            failed++;
        }
    }

    private static void RunMainAnalysis()
    {
        Console.WriteLine();
        Console.WriteLine("=== АНАЛИЗ 1 000 000 СОБЫТИЙ ===");

        List<GameEvent> events =
            GameEventGenerator.Generate(1_000_000, 12345);

        Console.WriteLine($"Всего событий: {events.Count:N0}");

        var linqResult = GameEventAnalyzer.AnalyzeWithLinq(events);
        var plinqResult = GameEventAnalyzer.AnalyzeWithPlinq(events);

        Console.WriteLine();
        Console.WriteLine("Топ игроков:");

        foreach (var player in plinqResult.Take(10))
        {
            Console.WriteLine(
                $"Игрок {player.PlayerId}: " +
                $"убийств {player.KillCount}, " +
                $"очков {player.TotalPoints}");
        }

        Console.WriteLine();
        Console.WriteLine(
            $"Результаты LINQ и PLINQ совпадают: {linqResult.SequenceEqual(plinqResult)}");
    }

    private static void RunSideEffectDemo()
    {
        Console.WriteLine();
        Console.WriteLine("=== ПОБОЧНЫЕ ЭФФЕКТЫ ===");

        List<GameEvent> events =
            GameEventGenerator.Generate(1_000_000, 777);

        int expected = events.Count;

        int rightCount = GameEventAnalyzer.GetPlayerIds(events).Count;

        Console.WriteLine($"Ожидалось идентификаторов: {expected:N0}");
        Console.WriteLine($"GetPlayerIds: {rightCount:N0}");

        if (System.Diagnostics.Debugger.IsAttached)
        {
            // Под отладчиком исключение из List<T>.Add останавливает Visual Studio,
            // поэтому опасную демонстрацию запускаем только обычным запуском.
            Console.WriteLine(
                "GetPlayerIdsWrong пропущен: под отладчиком его исключение останавливает VS.");
            return;
        }

        string wrongResult;

        try
        {
            int wrongCount = GameEventAnalyzer.GetPlayerIdsWrong(events).Count;

            wrongResult = wrongCount == expected
                ? $"{wrongCount:N0} (совпало случайно)"
                : $"{wrongCount:N0}";
        }
        catch (AggregateException ex)
        {
            string inner = string.Join(
                ", ",
                ex.InnerExceptions
                    .Select(e => e.GetType().Name)
                    .Distinct());

            wrongResult = $"исключение {inner}";
        }
        catch (Exception ex)
        {
            wrongResult = $"исключение {ex.GetType().Name}";
        }

        Console.WriteLine($"GetPlayerIdsWrong: {wrongResult}");
    }

    private static void RunBenchmarks()
    {
        Console.WriteLine();
        Console.WriteLine("=== СРАВНЕНИЕ LINQ И PLINQ ===");

        int[] sizes = { 1_000, 10_000, 100_000, 1_000_000 };

        foreach (int size in sizes)
        {
            List<GameEvent> events =
                GameEventGenerator.Generate(size, 12345);

            Console.WriteLine();
            Console.WriteLine($"Событий: {size:N0}");

            BenchmarkRunner.Compare(events);
        }
    }
}
