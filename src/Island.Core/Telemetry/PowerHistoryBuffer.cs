namespace Island.Core.Telemetry;

public sealed class PowerHistoryBuffer
{
    private readonly double[] buffer;
    private int count;
    private int head;
    private readonly object gate = new();

    public int Capacity { get; }

    public PowerHistoryBuffer(int capacity = 60)
    {
        Capacity = Math.Max(1, capacity);
        buffer = new double[Capacity];
    }

    public void Add(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) return;
        lock (gate)
        {
            buffer[head] = value;
            head = (head + 1) % Capacity;
            if (count < Capacity) count++;
        }
    }

    public IReadOnlyList<double> GetSamples()
    {
        lock (gate)
        {
            if (count == 0) return Array.Empty<double>();
            var result = new double[count];
            int start = (head - count + Capacity) % Capacity;
            for (int i = 0; i < count; i++)
            {
                result[i] = buffer[(start + i) % Capacity];
            }
            return result;
        }
    }

    public double? Mean
    {
        get
        {
            lock (gate)
            {
                if (count == 0) return null;
                double sum = 0;
                for (int i = 0; i < count; i++)
                {
                    sum += buffer[i];
                }
                return sum / count;
            }
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            count = 0;
            head = 0;
        }
    }
}
