namespace Island.Core.Motion;

// Exact solution of an underdamped spring over a frame; retargeting retains velocity.
public sealed class SpringValue
{
    private const double Damping = 0.78;
    private const double Frequency = 2 * Math.PI / 0.38;
    private static readonly double DampedFrequency = Frequency * Math.Sqrt(1 - Damping * Damping);

    public double Value { get; private set; }
    public double Velocity { get; private set; }

    public void Set(double value, double velocity = 0) => (Value, Velocity) = (value, velocity);

    public bool Advance(double target, double seconds)
    {
        double offset = Value - target;
        double decay = Math.Exp(-Damping * Frequency * seconds);
        double angle = DampedFrequency * seconds;
        double b = (Velocity + Damping * Frequency * offset) / DampedFrequency;
        var (sin, cos) = Math.SinCos(angle);
        double displacement = decay * (offset * cos + b * sin);
        Velocity = decay * DampedFrequency * (b * cos - offset * sin) - Damping * Frequency * displacement;
        Value = target + displacement;
        if (Math.Abs(displacement) > 0.0005 || Math.Abs(Velocity) > 0.012) return true;
        Set(target);
        return false;
    }
}
