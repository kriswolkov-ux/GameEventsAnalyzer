namespace GameEventsAnalyzer;

public static class GameEventAnalyzer
{
    public static List<PlayerStatistics> AnalyzeWithLinq(
        IEnumerable<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events
            .Where(e => e.Type == "Kill")
            .GroupBy(e => e.PlayerId)
            .Select(group => new PlayerStatistics(
                PlayerId: group.Key,
                KillCount: group.Count(),
                TotalPoints: group.Sum(e => (long)e.Points)))
            .OrderByDescending(x => x.TotalPoints)
            .ThenBy(x => x.PlayerId)
            .ToList();
    }

    public static List<PlayerStatistics> AnalyzeWithPlinq(
        IEnumerable<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        // PLINQ не обязательно быстрее LINQ: фильтрация и группировка здесь
        // простые, а распределение работы по потокам, объединение частичных
        // результатов и сортировка тоже требуют времени. Выигрыш появляется,
        // когда обработка одного элемента заметно дороже накладных расходов.
        return events
            .AsParallel()
            .Where(e => e.Type == "Kill")
            .GroupBy(e => e.PlayerId)
            .Select(group => new PlayerStatistics(
                PlayerId: group.Key,
                KillCount: group.Count(),
                TotalPoints: group.Sum(e => (long)e.Points)))
            .OrderByDescending(x => x.TotalPoints)
            .ThenBy(x => x.PlayerId)
            .ToList();
    }

    public static List<int> GetPlayerIdsWrong(
        IEnumerable<GameEvent> events)
    {
        // Ошибка: List<T> не потокобезопасен. Несколько потоков одновременно
        // вызывают Add и перестраивают внутренний массив, поэтому возможны
        // потерянные элементы, повреждение коллекции и исключения вроде
        // ArgumentOutOfRangeException: "capacity was less than the current size"
        // или ArgumentException: "Source array was not long enough".
        var result = new List<int>();

        events
            .AsParallel()
            .ForAll(e => result.Add(e.PlayerId));

        return result;
    }

    public static List<int> GetPlayerIds(
        IEnumerable<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        // ToList() безопаснее, чем ForAll + общий List<T>: результат
        // собирается внутри самого PLINQ-запроса, каждый поток формирует
        // свою часть, а общий список никто одновременно не меняет —
        // поэтому нет гонки и потерянных элементов.
        return events
            .AsParallel()
            .Select(e => e.PlayerId)
            .ToList();
    }
}
