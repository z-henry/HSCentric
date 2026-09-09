using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace HSCentric
{
    [Serializable]
    public sealed class ExperienceTotals
    {
        public long Xp { get; set; }
        public long RuntimeSeconds { get; set; }
        public long QuestXp { get; set; }
        public long OtherXp { get; set; }
        [JsonIgnore] public long BattleXp => Math.Max(0, Xp - QuestXp - OtherXp);
        [JsonIgnore] public double? Rate => RuntimeSeconds > 0 ? (double?)Math.Round(Xp * 3600d / RuntimeSeconds, 2) : null;

        public object Summary() => new { Xp, RuntimeSeconds, QuestXp, OtherXp, BattleXp, Rate,
            battleRate = RuntimeSeconds > 0 ? (double?)Math.Round(BattleXp * 3600d / RuntimeSeconds) : null,
            questRate = RuntimeSeconds > 0 ? (double?)Math.Round(QuestXp * 3600d / RuntimeSeconds) : null,
            otherRate = RuntimeSeconds > 0 ? (double?)Math.Round(OtherXp * 3600d / RuntimeSeconds) : null };

        public static ExperienceTotals Sum(IEnumerable<ExperienceTotals> items) => new ExperienceTotals {
            Xp = items.Sum(x => x.Xp), RuntimeSeconds = items.Sum(x => x.RuntimeSeconds),
            QuestXp = items.Sum(x => x.QuestXp), OtherXp = items.Sum(x => x.OtherXp) };
    }

    // Stored with the account's existing XML configuration, under daily_stats.
    // Cumulative legacy counters remain intact; dates are never guessed for old XP.
    [Serializable]
    public sealed class DailyStatistics
    {
        public string StartedOn { get; set; } = Key(DateTime.Today);
        public SortedDictionary<string, ExperienceTotals> Days { get; set; } = new SortedDictionary<string, ExperienceTotals>(StringComparer.Ordinal);
        public static string Key(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public ExperienceTotals Get(DateTime date) { ExperienceTotals value; return Days.TryGetValue(Key(date), out value) ? value : new ExperienceTotals(); }
        private ExperienceTotals Ensure(DateTime date)
        {
            string key = Key(date);
            if (!Days.ContainsKey(key)) Days[key] = new ExperienceTotals();
            return Days[key];
        }

        // Runtime follows the existing valid XP sample intervals. Split at backend
        // local midnight; allocate XP to the sample date, never interpolate earnings.
        public long Record(DateTime start, DateTime end, long xp)
        {
            if (start == DateTime.MaxValue || end < start) return 0;
            long totalSeconds = (long)(end - start).TotalSeconds, allocated = 0;
            DateTime cursor = start;
            while (cursor < end)
            {
                DateTime boundary = cursor.Date.AddDays(1);
                DateTime stop = boundary < end ? boundary : end;
                long cumulative = (long)(stop - start).TotalSeconds;
                Ensure(cursor).RuntimeSeconds += cumulative - allocated;
                allocated = cumulative; cursor = stop;
            }
            Ensure(end).Xp += Math.Max(0, xp);
            return totalSeconds;
        }

        public void Classify(DateTime date, long xp, bool quest)
        {
            if (xp <= 0) return;
            if (quest) Ensure(date).QuestXp += xp; else Ensure(date).OtherXp += xp;
        }
        public ExperienceTotals Between(DateTime start, DateTime end) => ExperienceTotals.Sum(Days.Where(p =>
            string.CompareOrdinal(p.Key, Key(start)) >= 0 && string.CompareOrdinal(p.Key, Key(end)) <= 0).Select(p => p.Value));
        public string Serialize() => JsonConvert.SerializeObject(this);
        public static DailyStatistics Deserialize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new DailyStatistics();
            var result = JsonConvert.DeserializeObject<DailyStatistics>(text);
            DateTime date;
            if (result == null || result.Days == null || !DateTime.TryParseExact(result.StartedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                throw new JsonException("每日经验统计格式错误。");
            foreach (var pair in result.Days)
                if (!DateTime.TryParseExact(pair.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) || pair.Value == null ||
                    pair.Value.Xp < 0 || pair.Value.RuntimeSeconds < 0 || pair.Value.QuestXp < 0 || pair.Value.OtherXp < 0)
                    throw new JsonException("每日经验统计包含无效记录。");
            return result;
        }
    }
}
