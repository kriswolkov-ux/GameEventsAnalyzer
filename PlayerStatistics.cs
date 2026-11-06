namespace GameEventsAnalyzer;

public sealed record PlayerStatistics(
    int PlayerId,
    int KillCount,
    long TotalPoints);
