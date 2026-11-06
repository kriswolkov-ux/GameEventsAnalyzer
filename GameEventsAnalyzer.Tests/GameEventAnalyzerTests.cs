using GameEventsAnalyzer;

namespace GameEventsAnalyzer.Tests;

public class GameEventAnalyzerTests
{
    private static readonly DateTime TestDate =
        new(2026, 1, 1);

    private static List<GameEvent> SampleEvents() =>
        new()
        {
            new(1, 1, "Kill", 100, TestDate),
            new(2, 1, "Kill", 150, TestDate),
            new(3, 2, "Kill", 200, TestDate),
            new(4, 1, "Death", 0, TestDate),
            new(5, 2, "Loot", 20, TestDate)
        };

    [Fact]
    public void LinqAndPlinq_ReturnSameStatistics()
    {
        var events = SampleEvents();

        var linq = GameEventAnalyzer.AnalyzeWithLinq(events);
        var plinq = GameEventAnalyzer.AnalyzeWithPlinq(events);

        Assert.Equal(linq, plinq);
        Assert.Equal(2, linq.Count);

        Assert.Equal(
            new PlayerStatistics(1, 2, 250),
            linq[0]);

        Assert.Equal(
            new PlayerStatistics(2, 1, 200),
            linq[1]);
    }

    [Fact]
    public void Analyze_EmptyCollection_ReturnsEmptyResult()
    {
        var events = new List<GameEvent>();

        Assert.Empty(GameEventAnalyzer.AnalyzeWithLinq(events));
        Assert.Empty(GameEventAnalyzer.AnalyzeWithPlinq(events));
    }

    [Fact]
    public void Analyze_WithoutKills_ReturnsEmptyResult()
    {
        var events = new List<GameEvent>
        {
            new(1, 1, "Death", 0, TestDate),
            new(2, 2, "Loot", 20, TestDate)
        };

        Assert.Empty(GameEventAnalyzer.AnalyzeWithPlinq(events));
    }

    [Fact]
    public void Analyze_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => GameEventAnalyzer.AnalyzeWithLinq(null!));

        Assert.Throws<ArgumentNullException>(
            () => GameEventAnalyzer.AnalyzeWithPlinq(null!));
    }

    [Fact]
    public void Analyze_OnePlayer_ReturnsSingleRow()
    {
        var events = new List<GameEvent>
        {
            new(1, 7, "Kill", 10, TestDate),
            new(2, 7, "Kill", 20, TestDate),
            new(3, 7, "Kill", 30, TestDate)
        };

        var result = GameEventAnalyzer.AnalyzeWithPlinq(events);

        Assert.Single(result);
        Assert.Equal(7, result[0].PlayerId);
        Assert.Equal(3, result[0].KillCount);
        Assert.Equal(60, result[0].TotalPoints);
    }

    [Fact]
    public void Analyze_EqualPoints_SortedByPlayerId()
    {
        var events = new List<GameEvent>
        {
            new(1, 3, "Kill", 100, TestDate),
            new(2, 1, "Kill", 100, TestDate)
        };

        var linq = GameEventAnalyzer.AnalyzeWithLinq(events);
        var plinq = GameEventAnalyzer.AnalyzeWithPlinq(events);

        Assert.Equal(new[] { 1, 3 }, linq.Select(x => x.PlayerId));
        Assert.Equal(new[] { 1, 3 }, plinq.Select(x => x.PlayerId));
    }

    [Fact]
    public void GetPlayerIds_ReturnsAllIdentifiers()
    {
        var events = GameEventGenerator.Generate(1_000, 12345);

        var ids = GameEventAnalyzer.GetPlayerIds(events);

        Assert.Equal(events.Count, ids.Count);

        Assert.Equal(
            events.Select(e => e.PlayerId).OrderBy(id => id),
            ids.OrderBy(id => id));
    }

    [Fact]
    public void Analyze_SingleEvent_ReturnsOneRow()
    {
        var events = new List<GameEvent>
        {
            new(1, 5, "Kill", 100, TestDate)
        };

        var linq = GameEventAnalyzer.AnalyzeWithLinq(events);
        var plinq = GameEventAnalyzer.AnalyzeWithPlinq(events);

        Assert.Single(linq);
        Assert.Equal(new PlayerStatistics(5, 1, 100), linq[0]);
        Assert.Equal(linq, plinq);
    }

    [Fact]
    public void Analyze_ManyPlayersWithEqualPoints_SortedByPlayerId()
    {
        var events = Enumerable.Range(1, 100)
            .Select(id => new GameEvent(id, 101 - id, "Kill", 50, TestDate))
            .ToList();

        var linq = GameEventAnalyzer.AnalyzeWithLinq(events);
        var plinq = GameEventAnalyzer.AnalyzeWithPlinq(events);

        Assert.Equal(
            Enumerable.Range(1, 100),
            linq.Select(x => x.PlayerId));

        Assert.Equal(linq, plinq);
    }

    [Fact]
    public void Analyze_LargeCollection_LinqAndPlinqMatch()
    {
        var events = GameEventGenerator.Generate(200_000, 12345);

        var linq = GameEventAnalyzer.AnalyzeWithLinq(events);
        var plinq = GameEventAnalyzer.AnalyzeWithPlinq(events);

        Assert.Equal(linq, plinq);
    }
}
