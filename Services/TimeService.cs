using System;
using System.Diagnostics;

namespace DuplicateFolderScanner.Services
{
    public interface ITimeService
    {
        long GetTimestamp();
        long GetElapsedMicroseconds(long startTimestamp);
        long GetElapsedMicroseconds(long startTimestamp, long endTimestamp);
        long GetElapsedMilliseconds(long startTimestamp);
        DateTime GetUtcNow();
    }

    public class TimeService : ITimeService
    {
        private static readonly double TicksPerMicrosecond = Stopwatch.Frequency / 1_000_000.0;
        private static readonly double TicksPerMillisecond = Stopwatch.Frequency / 1_000.0;

        public TimeService()
        {
            // Ensure Stopwatch is high resolution
            if (!Stopwatch.IsHighResolution)
            {
                throw new NotSupportedException("High resolution timer is not supported on this system.");
            }
        }

        public long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        public long GetElapsedMicroseconds(long startTimestamp)
        {
            return GetElapsedMicroseconds(startTimestamp, GetTimestamp());
        }

        public long GetElapsedMicroseconds(long startTimestamp, long endTimestamp)
        {
            long elapsedTicks = endTimestamp - startTimestamp;
            return (long)(elapsedTicks / TicksPerMicrosecond);
        }

        public long GetElapsedMilliseconds(long startTimestamp)
        {
            long elapsedTicks = GetTimestamp() - startTimestamp;
            return (long)(elapsedTicks / TicksPerMillisecond);
        }

        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}