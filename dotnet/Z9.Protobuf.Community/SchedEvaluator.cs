using System;
using System.Collections.Generic;
using System.Linq;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// Evaluates whether a Sched (schedule) is active at a given date/time,
    /// taking into account days of the week, time intervals, and holidays.
    /// Ported from z9main RestrictionEvaluator.
    /// </summary>
    public static class SchedEvaluator
    {
        /// <summary>
        /// If true, a null schedule means "always active" (24/7).
        /// Matches the commercial version's default behavior.
        /// </summary>
        public static bool NullSchedIs24x7 = true;

        /// <summary>
        /// Returns true if the given schedule is active at the specified local date/time.
        /// </summary>
        /// <param name="localDateTime">The local date/time to evaluate</param>
        /// <param name="sched">The schedule, or null (treated as 24/7 if NullSchedIs24x7)</param>
        /// <param name="holidays">All holidays to consider (may be null or empty)</param>
        /// <returns>True if the schedule is active</returns>
        public static bool InSched(DateTime localDateTime, Sched sched, IEnumerable<Hol> holidays)
        {
            return InSchedReturnElement(localDateTime, sched, holidays) != null;
        }

        /// <summary>
        /// Returns the matching SchedElement if the schedule is active, or null if not.
        /// </summary>
        public static SchedElement InSchedReturnElement(DateTime localDateTime, Sched sched, IEnumerable<Hol> holidays)
        {
            if (sched == null)
                return NullSchedIs24x7 ? new SchedElement() : null;

            // Zero out sub-second precision
            localDateTime = ZeroMillis(localDateTime);

            SchedDay schedDay = ToSchedDay(localDateTime.DayOfWeek);

            // Get matching holiday types in two modes
            bool allPreserve = GetMatchingHolTypes(localDateTime, holidays, true, out HashSet<int> holTypesPreserve);
            bool allOverride = GetMatchingHolTypes(localDateTime, holidays, false, out HashSet<int> holTypesOverride);

            foreach (SchedElement e in sched.Elements)
            {
                // Check if today's instance of this element matches
                if (MatchSchedDayOrHoliday(e, schedDay, allPreserve, holTypesPreserve, allOverride, holTypesOverride)
                    && TimeInSchedElement(localDateTime, e))
                    return e;

                // For plusDays > 0, also check if the previous day's instance
                // of this element crosses into the current time.
                if (e.PlusDays > 0)
                {
                    DateTime yesterday = localDateTime.AddDays(-1);
                    SchedDay yesterdaySchedDay = ToSchedDay(yesterday.DayOfWeek);
                    bool allPreserveY = GetMatchingHolTypes(yesterday, holidays, true, out HashSet<int> holTypesPreserveY);
                    bool allOverrideY = GetMatchingHolTypes(yesterday, holidays, false, out HashSet<int> holTypesOverrideY);

                    if (MatchSchedDayOrHoliday(e, yesterdaySchedDay, allPreserveY, holTypesPreserveY, allOverrideY, holTypesOverrideY)
                        && TimeInSchedElementFromDate(localDateTime, yesterday.Date, e))
                        return e;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the given date/time matches a holiday definition, handling
        /// repeat, multi-day, and leap year logic.
        /// </summary>
        public static bool MatchesHol(DateTime localDateTime, Hol hol)
        {
            if (hol.Date == null)
                return false;

            DateTime holDate = ToDateTime(hol.Date);
            int numDays = hol.NumDays;
            if (numDays < 1)
                return false;

            DateTime startFirstYear = holDate.Date;
            DateTime stopFirstYear = startFirstYear.AddDays(numDays - 1);

            DateTime localDate = localDateTime.Date;

            if (localDate < startFirstYear)
                return false;

            bool repeat = hol.Repeat;
            bool repeatIndefinitely = repeat && hol.NumYearsRepeatCase == Hol.NumYearsRepeatOneofCase.None;
            int numYearsRepeat = !repeat ? 0 : (repeatIndefinitely ? int.MaxValue : hol.NumYearsRepeat);

            for (int yOffset = 0; yOffset <= numYearsRepeat; yOffset++)
            {
                DateTime startOffset;
                DateTime stopOffset;

                try
                {
                    startOffset = startFirstYear.AddYears(yOffset);
                    stopOffset = stopFirstYear.AddYears(yOffset);
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }

                // Leap day handling
                if (IsLeapDay(startFirstYear) && !IsLeapDay(startOffset))
                {
                    if (numDays == 1)
                    {
                        // Skip this year — can't have Feb 29 in a non-leap year
                        continue;
                    }
                    else
                    {
                        startOffset = startOffset.AddDays(1);
                    }
                }
                if (IsLeapDay(stopFirstYear) && !IsLeapDay(stopOffset))
                {
                    if (numDays == 1)
                    {
                        continue;
                    }
                    else
                    {
                        stopOffset = stopOffset.AddDays(-1);
                    }
                }

                if (localDate >= startOffset && localDate <= stopOffset)
                {
                    return true;
                }

                if (localDate < startOffset)
                    break;
            }

            return false;
        }

        // --- Internal helpers ---

        internal static SchedDay ToSchedDay(DayOfWeek dow)
        {
            switch (dow)
            {
                case DayOfWeek.Monday: return SchedDay.Mon;
                case DayOfWeek.Tuesday: return SchedDay.Tues;
                case DayOfWeek.Wednesday: return SchedDay.Wed;
                case DayOfWeek.Thursday: return SchedDay.Thur;
                case DayOfWeek.Friday: return SchedDay.Fri;
                case DayOfWeek.Saturday: return SchedDay.Sat;
                case DayOfWeek.Sunday: return SchedDay.Sun;
                default: throw new ArgumentException("Unknown DayOfWeek: " + dow);
            }
        }

        /// <summary>
        /// Finds all holiday types that match the given date/time for the specified preserveSchedDay mode.
        /// Returns true if "all holiday types" matched (sentinel).
        /// </summary>
        internal static bool GetMatchingHolTypes(DateTime localDateTime, IEnumerable<Hol> holidays,
            bool preserveSchedDay, out HashSet<int> holTypeUnids)
        {
            holTypeUnids = new HashSet<int>();
            if (holidays == null)
                return false;

            foreach (Hol hol in holidays)
            {
                bool holPreserve = hol.PreserveSchedDay;
                if (holPreserve != preserveSchedDay)
                    continue;

                if (MatchesHol(localDateTime, hol))
                {
                    if (hol.AllHolTypes)
                        return true; // sentinel: all types match

                    foreach (int unid in hol.HolTypesUnid)
                        holTypeUnids.Add(unid);
                }
            }

            return false;
        }

        internal static bool MatchSchedDayOrHoliday(SchedElement e, SchedDay schedDay,
            bool allHolTypesPreserve, HashSet<int> holTypesPreserve,
            bool allHolTypesOverride, HashSet<int> holTypesOverride)
        {
            // Preserve mode: holiday match is additive (still check schedDay as fallback)
            if (allHolTypesPreserve)
            {
                if (e.Holidays)
                    return true;
            }
            else if (holTypesPreserve.Count > 0)
            {
                if (MatchAnyHolType(e, holTypesPreserve))
                    return true;
            }

            // Override mode: holiday replaces day-of-week matching entirely
            if (allHolTypesOverride)
            {
                return e.Holidays;
            }
            else if (holTypesOverride.Count > 0)
            {
                return MatchAnyHolType(e, holTypesOverride);
            }

            // No holidays matched — check day of week
            return e.SchedDays.Contains(schedDay);
        }

        internal static bool MatchAnyHolType(SchedElement e, HashSet<int> holTypeUnids)
        {
            if (!e.Holidays)
                return false;

            if (e.HolTypesUnid.Count == 0)
            {
                // Empty list with holidays=true means "all holiday types"
                return true;
            }

            foreach (int unid in e.HolTypesUnid)
            {
                if (holTypeUnids.Contains(unid))
                    return true;
            }

            return false;
        }

        internal static bool TimeInSchedElement(DateTime localDateTime, SchedElement e)
        {
            return TimeInSchedElementFromDate(localDateTime, localDateTime.Date, e);
        }

        /// <summary>
        /// Checks if localDateTime falls within the element's time range,
        /// using baseDate as the date on which the element's start time is anchored.
        /// This allows checking yesterday's element crossing into today via plusDays.
        /// </summary>
        internal static bool TimeInSchedElementFromDate(DateTime localDateTime, DateTime baseDate, SchedElement e)
        {
            if (e.Start == null || e.Stop == null)
                return false;

            DateTime startTime = baseDate.Add(ToTimeSpan(e.Start));
            DateTime stopTime = baseDate.Add(ToTimeSpan(e.Stop)).AddDays(e.PlusDays);

            return localDateTime >= startTime && localDateTime <= stopTime;
        }

        internal static DateTime ToDateTime(SqlDateData d)
        {
            return new DateTime(d.Year, d.Month, d.Day);
        }

        internal static TimeSpan ToTimeSpan(SqlTimeData t)
        {
            return new TimeSpan(t.Hour, t.Minute, t.Second);
        }

        private static DateTime ZeroMillis(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
        }

        private static bool IsLeapDay(DateTime dt)
        {
            return dt.Month == 2 && dt.Day == 29;
        }
    }
}
