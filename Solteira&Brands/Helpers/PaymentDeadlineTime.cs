using System;

namespace Solteira_Brands.Helpers
{
    /// <summary>
    /// <c>payment_deadline</c> is a UTC instant; EF usually loads it as <see cref="DateTimeKind.Unspecified"/>.
    /// <see cref="DateTime.ToString(string)"/> with "o" then omits "Z", and browsers parse that as <em>local</em> time,
    /// skewing countdowns by the client timezone offset (e.g. ~8h for UTC+8).
    /// </summary>
    public static class PaymentDeadlineTime
    {
        public static DateTime AsUtc(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Utc:
                    return value;
                case DateTimeKind.Local:
                    return value.ToUniversalTime();
                default:
                    return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
        }

        public static string ToRoundTripIsoUtc(DateTime value)
        {
            return AsUtc(value).ToString("o");
        }

        /// <summary>Wall clock on the app server (IIS / dev machine) for buyer-facing labels.</summary>
        public static string FormatDeadlineLocal(DateTime value, string format)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(AsUtc(value), TimeZoneInfo.Local).ToString(format);
        }

        public static string RemainingDurationText(DateTime deadlineFromDb, DateTime nowUtc)
        {
            var span = AsUtc(deadlineFromDb) - nowUtc;
            return span.TotalSeconds <= 0 ? "Expired" : span.ToString(@"dd\.hh\:mm\:ss");
        }

        public static bool IsExpiredUtc(DateTime deadlineFromDb, DateTime nowUtc)
        {
            return AsUtc(deadlineFromDb) <= nowUtc;
        }
    }
}
