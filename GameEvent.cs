namespace GameEventsAnalyzer;

public sealed record GameEvent(
    int Id,
    int PlayerId,
    string Type,
    int Points,
    DateTime Timestamp);
