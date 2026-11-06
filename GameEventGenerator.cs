namespace GameEventsAnalyzer;

public static class GameEventGenerator
{
    private static readonly string[] EventTypes =
    {
        "Kill",
        "Death",
        "Loot",
        "LevelComplete"
    };

    public static List<GameEvent> Generate(int count, int seed = 12345)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var random = new Random(seed);
        var events = new List<GameEvent>(count);

        DateTime start = new(2026, 1, 1, 12, 0, 0);

        for (int i = 0; i < count; i++)
        {
            string type = EventTypes[random.Next(EventTypes.Length)];

            int points = type switch
            {
                "Kill" => random.Next(50, 201),
                "Death" => 0,
                "Loot" => random.Next(5, 51),
                "LevelComplete" => random.Next(100, 501),
                _ => 0
            };

            events.Add(new GameEvent(
                Id: i + 1,
                PlayerId: random.Next(1, 101),
                Type: type,
                Points: points,
                Timestamp: start.AddSeconds(i)));
        }

        return events;
    }
}
