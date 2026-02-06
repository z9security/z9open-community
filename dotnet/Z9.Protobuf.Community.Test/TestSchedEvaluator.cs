using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Z9.Protobuf;
using Z9.Spcore.Proto;

namespace Z9.Protobuf.Community.Test
{
    [TestClass]
    public class TestSchedEvaluator
    {
        // ---- Helper methods ----

        private static SqlTimeData Time(int hour, int minute, int second)
        {
            return SpCoreProtoUtil.ToSqlTimeData(hour, minute, second);
        }

        private static SqlDateData Date(int year, int month, int day)
        {
            return new SqlDateData { Year = year, Month = month, Day = day };
        }

        private static SchedElement MakeElement(SqlTimeData start, SqlTimeData stop, int plusDays, params SchedDay[] days)
        {
            var e = new SchedElement { Start = start, Stop = stop, PlusDays = plusDays, Holidays = false };
            foreach (var d in days)
                e.SchedDays.Add(d);
            return e;
        }

        private static SchedElement MakeAllDaysElement(SqlTimeData start, SqlTimeData stop)
        {
            return MakeElement(start, stop, 0,
                SchedDay.Mon, SchedDay.Tues, SchedDay.Wed, SchedDay.Thur, SchedDay.Fri, SchedDay.Sat, SchedDay.Sun);
        }

        private static Hol MakeHol(int year, int month, int day, int numDays, bool repeat, bool allHolTypes,
            bool preserveSchedDay, int? numYearsRepeat = null, params int[] holTypeUnids)
        {
            var hol = new Hol
            {
                Date = Date(year, month, day),
                NumDays = numDays,
                Repeat = repeat,
                AllHolTypes = allHolTypes,
                PreserveSchedDay = preserveSchedDay
            };
            if (numYearsRepeat.HasValue)
                hol.NumYearsRepeat = numYearsRepeat.Value;
            foreach (var u in holTypeUnids)
                hol.HolTypesUnid.Add(u);
            return hol;
        }

        // ---- ToSchedDay ----

        [TestMethod]
        public void TestToSchedDay()
        {
            Assert.AreEqual(SchedDay.Mon, SchedEvaluator.ToSchedDay(DayOfWeek.Monday));
            Assert.AreEqual(SchedDay.Tues, SchedEvaluator.ToSchedDay(DayOfWeek.Tuesday));
            Assert.AreEqual(SchedDay.Wed, SchedEvaluator.ToSchedDay(DayOfWeek.Wednesday));
            Assert.AreEqual(SchedDay.Thur, SchedEvaluator.ToSchedDay(DayOfWeek.Thursday));
            Assert.AreEqual(SchedDay.Fri, SchedEvaluator.ToSchedDay(DayOfWeek.Friday));
            Assert.AreEqual(SchedDay.Sat, SchedEvaluator.ToSchedDay(DayOfWeek.Saturday));
            Assert.AreEqual(SchedDay.Sun, SchedEvaluator.ToSchedDay(DayOfWeek.Sunday));
        }

        // ---- MatchesHol: single day ----

        [TestMethod]
        public void TestMatchesHol_SingleDay()
        {
            // Christmas 2011, single day, no repeat
            var hol = MakeHol(2011, 12, 25, 1, false, false, false, null, 1);

            var dateTime = new DateTime(2011, 12, 25, 0, 0, 0);

            // Matches on the exact day
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime, hol));
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddHours(1), hol));
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddHours(23).AddMinutes(59).AddSeconds(59), hol));

            // Does NOT match before/after
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddSeconds(-1), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddDays(1), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddDays(-1), hol));

            // Does NOT match next year (no repeat)
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddYears(1), hol));
        }

        // ---- MatchesHol: repeat 1 year ----

        [TestMethod]
        public void TestMatchesHol_Repeat1Year()
        {
            var hol = MakeHol(2011, 12, 25, 1, true, false, false, 1, 1);

            var dateTime = new DateTime(2011, 12, 25, 0, 0, 0);
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime, hol));
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddYears(1), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddYears(2), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddYears(-1), hol));
        }

        // ---- MatchesHol: repeat 2 years ----

        [TestMethod]
        public void TestMatchesHol_Repeat2Years()
        {
            var hol = MakeHol(2011, 12, 25, 1, true, false, false, 2, 1);

            var dateTime = new DateTime(2011, 12, 25, 0, 0, 0);
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime, hol));
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddYears(1), hol));
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddYears(2), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddYears(3), hol));
        }

        // ---- MatchesHol: repeat indefinitely ----

        [TestMethod]
        public void TestMatchesHol_RepeatIndefinitely()
        {
            // repeat=true, numYearsRepeat not set → indefinite
            var hol = MakeHol(2011, 12, 25, 1, true, false, false, null, 1);

            var dateTime = new DateTime(2011, 12, 25, 0, 0, 0);
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime, hol));
            for (int i = 0; i < 100; i++)
                Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddYears(i), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddYears(-1), hol));
        }

        // ---- MatchesHol: multi-day ----

        [TestMethod]
        public void TestMatchesHol_MultiDay()
        {
            // repeat indefinitely
            var hol = MakeHol(2011, 12, 25, 3, true, false, false, null, 1);

            var dateTime = new DateTime(2011, 12, 25, 0, 0, 0);

            for (int yr = 0; yr < 10; yr++)
            {
                // Before start
                Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddYears(yr).AddSeconds(-1), hol));
                // Day 0, 1, 2 match
                Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddYears(yr), hol));
                Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddYears(yr).AddDays(1), hol));
                Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddYears(yr).AddDays(2), hol));
                // Day 3 does NOT match
                Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddYears(yr).AddDays(3), hol));
            }
        }

        // ---- MatchesHol: leap year ----

        [TestMethod]
        public void TestMatchesHol_LeapYear()
        {
            // Leap Day holiday, repeating for 100 years
            var hol = MakeHol(2020, 2, 29, 1, true, false, false, 100, 1);

            var dateTime = new DateTime(2020, 2, 29, 0, 0, 0);

            // Matches on exact date
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime, hol));
            Assert.IsTrue(SchedEvaluator.MatchesHol(dateTime.AddHours(1), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddSeconds(-1), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddDays(1), hol));
            Assert.IsFalse(SchedEvaluator.MatchesHol(dateTime.AddDays(-1), hol));

            // Matches on leap years: 2024, 2028, 2032
            for (int i = 0; i < 4; i++)
                Assert.IsTrue(SchedEvaluator.MatchesHol(new DateTime(2020 + i * 4, 2, 29, 0, 0, 0), hol));

            // NEVER bleeds into Feb 28 in any year
            var feb28 = new DateTime(2020, 2, 28, 0, 0, 0);
            for (int i = 0; i < 20; i++)
                Assert.IsFalse(SchedEvaluator.MatchesHol(feb28.AddYears(i), hol));

            // NEVER bleeds into Mar 1 in any year
            var mar1 = new DateTime(2020, 3, 1, 0, 0, 0);
            for (int i = 0; i < 20; i++)
                Assert.IsFalse(SchedEvaluator.MatchesHol(mar1.AddYears(i), hol));
        }

        // ---- InSched: null sched is 24/7 ----

        [TestMethod]
        public void TestInSched_NullSchedIs24x7()
        {
            var now = new DateTime(2024, 6, 15, 12, 0, 0);
            Assert.IsTrue(SchedEvaluator.InSched(now, null, null));
            Assert.IsNotNull(SchedEvaluator.InSchedReturnElement(now, null, null));
        }

        // ---- InSched: all days, all hours ----

        [TestMethod]
        public void TestInSched_AllDaysAllHours()
        {
            var sched = new Sched { Name = "24/7" };
            sched.Elements.Add(MakeAllDaysElement(Time(0, 0, 0), Time(23, 59, 59)));

            // Should be active at any time
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2024, 6, 15, 0, 0, 0), sched, null)); // Sat midnight
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2024, 6, 15, 12, 0, 0), sched, null)); // Sat noon
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2024, 6, 15, 23, 59, 59), sched, null)); // Sat end
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2024, 6, 17, 8, 0, 0), sched, null)); // Mon morning
        }

        // ---- InSched: empty schedule (no elements) ----

        [TestMethod]
        public void TestInSched_EmptySchedule()
        {
            var sched = new Sched { Name = "Empty" };
            Assert.IsFalse(SchedEvaluator.InSched(new DateTime(2024, 6, 15, 12, 0, 0), sched, null));
        }

        // ---- InSched: empty days (element has no days) ----

        [TestMethod]
        public void TestInSched_EmptyDays()
        {
            var sched = new Sched { Name = "No days" };
            var e = new SchedElement { Start = Time(0, 0, 0), Stop = Time(23, 59, 59), PlusDays = 0, Holidays = false };
            // No SchedDays added
            sched.Elements.Add(e);

            Assert.IsFalse(SchedEvaluator.InSched(new DateTime(2024, 6, 15, 12, 0, 0), sched, null));
        }

        // ---- InSched: day exclusion (Mon-Fri only) ----

        [TestMethod]
        public void TestInSched_DayExclusion()
        {
            var sched = new Sched { Name = "Weekdays only" };
            sched.Elements.Add(MakeElement(Time(0, 0, 0), Time(23, 59, 59), 0,
                SchedDay.Mon, SchedDay.Tues, SchedDay.Wed, SchedDay.Thur, SchedDay.Fri));

            // 2024-06-17 is a Monday
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2024, 6, 17, 12, 0, 0), sched, null));
            // 2024-06-15 is a Saturday
            Assert.IsFalse(SchedEvaluator.InSched(new DateTime(2024, 6, 15, 12, 0, 0), sched, null));
            // 2024-06-16 is a Sunday
            Assert.IsFalse(SchedEvaluator.InSched(new DateTime(2024, 6, 16, 12, 0, 0), sched, null));
        }

        // ---- InSched: time window ----

        [TestMethod]
        public void TestInSched_TimeWindow()
        {
            var sched = new Sched { Name = "9 to 5" };
            sched.Elements.Add(MakeAllDaysElement(Time(9, 0, 0), Time(17, 0, 0)));

            var date = new DateTime(2024, 6, 17, 0, 0, 0); // Monday

            Assert.IsFalse(SchedEvaluator.InSched(date.AddHours(8).AddMinutes(59).AddSeconds(59), sched, null));
            Assert.IsTrue(SchedEvaluator.InSched(date.AddHours(9), sched, null));
            Assert.IsTrue(SchedEvaluator.InSched(date.AddHours(12), sched, null));
            Assert.IsTrue(SchedEvaluator.InSched(date.AddHours(17), sched, null));
            Assert.IsFalse(SchedEvaluator.InSched(date.AddHours(17).AddSeconds(1), sched, null));
        }

        // ---- InSched: plusDays / midnight crossing ----

        [TestMethod]
        public void TestInSched_PlusDays_MidnightCrossing()
        {
            var sched = new Sched { Name = "Night shift" };
            // 22:00 to 06:00 next day (plusDays=1)
            sched.Elements.Add(MakeAllDaysElement(Time(22, 0, 0), Time(6, 0, 0)));
            sched.Elements[0].PlusDays = 1;

            var date = new DateTime(2024, 6, 17, 0, 0, 0); // Monday

            // Before start
            Assert.IsFalse(SchedEvaluator.InSched(date.AddHours(21).AddMinutes(59).AddSeconds(59), sched, null));
            // At start
            Assert.IsTrue(SchedEvaluator.InSched(date.AddHours(22), sched, null));
            // Midnight
            Assert.IsTrue(SchedEvaluator.InSched(date.AddDays(1), sched, null));
            // 3am next day
            Assert.IsTrue(SchedEvaluator.InSched(date.AddDays(1).AddHours(3), sched, null));
            // At stop (06:00 next day)
            Assert.IsTrue(SchedEvaluator.InSched(date.AddDays(1).AddHours(6), sched, null));
            // After stop
            Assert.IsFalse(SchedEvaluator.InSched(date.AddDays(1).AddHours(6).AddSeconds(1), sched, null));
        }

        // ---- InSched: holiday override (preserveSchedDay=false) ----

        [TestMethod]
        public void TestInSched_HolidayOverride()
        {
            var sched = new Sched { Name = "With holiday override" };

            // Normal element: all days 00:00-23:59
            sched.Elements.Add(MakeAllDaysElement(Time(0, 0, 0), Time(23, 59, 59)));

            // Holiday element: holidays only, 00:00-00:01 (very short window)
            var holElement = new SchedElement
            {
                Start = Time(0, 0, 0),
                Stop = Time(0, 1, 0),
                PlusDays = 0,
                Holidays = true
            };
            holElement.HolTypesUnid.Add(1); // specific type
            sched.Elements.Add(holElement);

            // Holiday: Jan 15, 2011, override (preserveSchedDay=false), type 1
            var hol = MakeHol(2011, 1, 15, 1, false, false, false, null, 1);

            var holidays = new List<Hol> { hol };

            var dateTime = new DateTime(2011, 1, 15, 0, 0, 0); // Saturday

            // Without holidays, normal schedule applies
            Assert.IsTrue(SchedEvaluator.InSched(dateTime, sched, null));
            Assert.IsTrue(SchedEvaluator.InSched(dateTime.AddHours(1), sched, null));

            // With holidays (override mode), on the holiday date:
            // Only the holiday element's time window applies (00:00-00:01)
            Assert.IsTrue(SchedEvaluator.InSched(dateTime, sched, holidays));
            Assert.IsTrue(SchedEvaluator.InSched(dateTime.AddMinutes(1), sched, holidays));
            Assert.IsFalse(SchedEvaluator.InSched(dateTime.AddMinutes(1).AddSeconds(1), sched, holidays));
            Assert.IsFalse(SchedEvaluator.InSched(dateTime.AddHours(1), sched, holidays));

            // On a non-holiday date, normal schedule works fine
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2011, 1, 16, 12, 0, 0), sched, holidays));
        }

        // ---- InSched: holiday preserveSchedDay=true ----

        [TestMethod]
        public void TestInSched_HolidayPreserveSchedDay()
        {
            var sched = new Sched { Name = "With preserve holiday" };

            // Normal element: weekdays 09:00-17:00
            sched.Elements.Add(MakeElement(Time(9, 0, 0), Time(17, 0, 0), 0,
                SchedDay.Mon, SchedDay.Tues, SchedDay.Wed, SchedDay.Thur, SchedDay.Fri));

            // Holiday element: holidays, 00:00-23:59 (full day access on holidays)
            var holElement = new SchedElement
            {
                Start = Time(0, 0, 0),
                Stop = Time(23, 59, 59),
                PlusDays = 0,
                Holidays = true
            };
            sched.Elements.Add(holElement);

            // Holiday: Wed Jan 15, 2025, preserveSchedDay=true, allHolTypes=true
            var hol = MakeHol(2025, 1, 15, 1, false, true, true);
            var holidays = new List<Hol> { hol };

            // 2025-01-15 is a Wednesday
            var dateTime = new DateTime(2025, 1, 15, 7, 0, 0);

            // Without holidays: 7am is outside 9-5 → inactive
            Assert.IsFalse(SchedEvaluator.InSched(dateTime, sched, null));

            // With holidays (preserve mode): holiday element gives full-day access,
            // AND the normal weekday element still applies too.
            // 7am matches the holiday element (00:00-23:59)
            Assert.IsTrue(SchedEvaluator.InSched(dateTime, sched, holidays));
            // 10am matches both holiday and weekday elements
            Assert.IsTrue(SchedEvaluator.InSched(dateTime.AddHours(3), sched, holidays));
        }

        // ---- InSched: holiday with specific types ----

        [TestMethod]
        public void TestInSched_HolidayWithSpecificTypes()
        {
            var sched = new Sched { Name = "Type-filtered holidays" };

            // Normal element: all days 09:00-17:00
            sched.Elements.Add(MakeAllDaysElement(Time(9, 0, 0), Time(17, 0, 0)));

            // Holiday element for type 1 only, 00:00-08:00
            var holElement1 = new SchedElement
            {
                Start = Time(0, 0, 0),
                Stop = Time(8, 0, 0),
                PlusDays = 0,
                Holidays = true
            };
            holElement1.HolTypesUnid.Add(1);
            sched.Elements.Add(holElement1);

            // Holiday: type 1
            var hol1 = MakeHol(2024, 12, 25, 1, false, false, false, null, 1);
            // Holiday: type 2 (not in element)
            var hol2 = MakeHol(2024, 12, 26, 1, false, false, false, null, 2);

            var holidays = new List<Hol> { hol1, hol2 };

            // Dec 25 at 7am — type 1 matches holiday element → active
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2024, 12, 25, 7, 0, 0), sched, holidays));

            // Dec 26 at 7am — type 2 doesn't match any holiday element → override blocks day element too
            Assert.IsFalse(SchedEvaluator.InSched(new DateTime(2024, 12, 26, 7, 0, 0), sched, holidays));

            // Dec 26 at 10am — type 2 override, no holiday element matches type 2 → inactive
            Assert.IsFalse(SchedEvaluator.InSched(new DateTime(2024, 12, 26, 10, 0, 0), sched, holidays));
        }

        // ---- InSched: holiday allHolTypes ----

        [TestMethod]
        public void TestInSched_HolidayAllTypes()
        {
            var sched = new Sched { Name = "All types holiday" };

            // Normal element: all days 09:00-17:00
            sched.Elements.Add(MakeAllDaysElement(Time(9, 0, 0), Time(17, 0, 0)));

            // Holiday element: any holiday type, 06:00-08:00
            var holElement = new SchedElement
            {
                Start = Time(6, 0, 0),
                Stop = Time(8, 0, 0),
                PlusDays = 0,
                Holidays = true
            };
            // Empty HolTypesUnid with holidays=true means ALL types
            sched.Elements.Add(holElement);

            // Holiday with allHolTypes=true
            var hol = MakeHol(2024, 7, 4, 1, false, true, false);
            var holidays = new List<Hol> { hol };

            // July 4 at 7am — holiday element matches (all types, 06:00-08:00)
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2024, 7, 4, 7, 0, 0), sched, holidays));

            // July 4 at 10am — holiday override mode, only holiday elements checked.
            // holElement is 06:00-08:00, so 10am doesn't match
            Assert.IsFalse(SchedEvaluator.InSched(new DateTime(2024, 7, 4, 10, 0, 0), sched, holidays));

            // July 5 at 10am — not a holiday, normal schedule, 09:00-17:00 → active
            Assert.IsTrue(SchedEvaluator.InSched(new DateTime(2024, 7, 5, 10, 0, 0), sched, holidays));
        }

        // ---- InSched: multiple elements, first match wins ----

        [TestMethod]
        public void TestInSched_MultipleElements()
        {
            var sched = new Sched { Name = "Multi-element" };

            // Element 1: Mon-Fri 09:00-12:00
            sched.Elements.Add(MakeElement(Time(9, 0, 0), Time(12, 0, 0), 0,
                SchedDay.Mon, SchedDay.Tues, SchedDay.Wed, SchedDay.Thur, SchedDay.Fri));

            // Element 2: Mon-Fri 13:00-17:00
            sched.Elements.Add(MakeElement(Time(13, 0, 0), Time(17, 0, 0), 0,
                SchedDay.Mon, SchedDay.Tues, SchedDay.Wed, SchedDay.Thur, SchedDay.Fri));

            var monday = new DateTime(2024, 6, 17, 0, 0, 0);

            Assert.IsFalse(SchedEvaluator.InSched(monday.AddHours(8), sched, null));
            Assert.IsTrue(SchedEvaluator.InSched(monday.AddHours(9), sched, null));
            Assert.IsTrue(SchedEvaluator.InSched(monday.AddHours(11), sched, null));
            Assert.IsFalse(SchedEvaluator.InSched(monday.AddHours(12).AddSeconds(1), sched, null));
            Assert.IsTrue(SchedEvaluator.InSched(monday.AddHours(13), sched, null));
            Assert.IsTrue(SchedEvaluator.InSched(monday.AddHours(16), sched, null));
            Assert.IsFalse(SchedEvaluator.InSched(monday.AddHours(17).AddSeconds(1), sched, null));
        }

        // ---- InSched: 24/7 except wednesday (from Java test) ----

        [TestMethod]
        public void TestInSched_24x7ExceptWednesday()
        {
            var sched = new Sched { Name = "24/7 but not Wed" };
            sched.Elements.Add(MakeElement(Time(0, 0, 0), Time(23, 59, 59), 0,
                SchedDay.Mon, SchedDay.Tues, SchedDay.Thur, SchedDay.Fri, SchedDay.Sat, SchedDay.Sun));

            // 2012-02-08 is a Wednesday
            var wed = new DateTime(2012, 2, 8, 12, 0, 0);
            Assert.IsFalse(SchedEvaluator.InSched(wed, sched, null));

            // Thursday
            var thu = new DateTime(2012, 2, 9, 12, 0, 0);
            Assert.IsTrue(SchedEvaluator.InSched(thu, sched, null));
        }

        // Note: Hol.Start/Stop time fields (22/23) are not in the community proto,
        // so holiday time-of-day filtering is not supported in the community edition.
    }
}
