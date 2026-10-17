using Pipeline.Application.Abstractions;

namespace Pipeline.TestKit.Fakes;

public sealed class FakeClock : IClock
{
    private DateTimeOffset _now;

    public FakeClock(DateTimeOffset? initial = null)
    {
        _now = initial ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    public DateTimeOffset UtcNow => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    public void Set(DateTimeOffset newTime) => _now = newTime;
}
