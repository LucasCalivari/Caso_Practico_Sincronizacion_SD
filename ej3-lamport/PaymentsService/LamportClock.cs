using System.Threading;

public sealed class LamportClock
{
    private long _value;

    // Evento interno o envío: incrementar y obtener la marca.
    public long Tick() => Interlocked.Increment(ref _value);

    // Recepción: max(reloj local, marca recibida) + 1.
    public long Receive(long received)
    {
        while (true)
        {
            long current = Interlocked.Read(ref _value);
            long next = checked(Math.Max(current, received) + 1);
            if (Interlocked.CompareExchange(ref _value, next, current) == current)
                return next;
        }
    }

    public long Read() => Interlocked.Read(ref _value);
}
