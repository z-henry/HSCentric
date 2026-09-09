using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using HSCentric;

internal static class DailyStatisticsTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }
    private static int Main()
    {
        try { Run(); return 0; }
        catch (Exception error) {
            while (error != null) { Console.Error.WriteLine(error.GetType().FullName + ": " + error.Message); error = error.InnerException; }
            return 1;
        }
    }
    private static void Run()
    {
        var start = new DateTime(2026, 9, 8, 23, 59, 30);
        var daily = new DailyStatistics();
        Check(daily.Record(start, start.AddSeconds(90), 150) == 90, "interval counted once");
        Check(daily.Get(start).RuntimeSeconds == 30 && daily.Get(start.AddDays(1)).RuntimeSeconds == 60, "midnight runtime split");
        Check(daily.Get(start).Xp == 0 && daily.Get(start.AddDays(1)).Xp == 150, "XP belongs to sampling day");
        daily.Record(start.AddSeconds(90), start.AddSeconds(90), -100);
        Check(daily.Get(start.AddDays(1)).Xp == 150, "negative XP never subtracts historical gains");
        Check(daily.Record(DateTime.MaxValue, start, 300) == 0 && daily.Record(start, start.AddSeconds(-1), 300) == 0, "inactive and backward intervals ignored");
        daily.Classify(start.AddDays(1), 30, true); daily.Classify(start.AddDays(1), 20, false);
        Check(daily.Get(start.AddDays(1)).BattleXp == 100, "quest and other breakdown retained");
        var weighted = new DailyStatistics();
        weighted.Record(start.Date.AddHours(1), start.Date.AddHours(2), 100);
        weighted.Record(start.Date.AddDays(1).AddHours(1), start.Date.AddDays(1).AddHours(10), 1800);
        Check(weighted.Between(start.Date, start.Date.AddDays(1)).Rate == 190, "period efficiency weighted by runtime rather than daily mean");
        Check(weighted.Get(start.AddDays(2)).Rate == null, "missing day has no fabricated zero rate");
        var roundTrip = DailyStatistics.Deserialize(daily.Serialize());
        Check(roundTrip.Get(start.AddDays(1)).QuestXp == 30 && roundTrip.Days.Count == 2, "daily serialization roundtrip");
        Check(DailyStatistics.Deserialize("").Days.Count == 0, "legacy config initializes empty daily history");
        bool rejected = false;
        try { DailyStatistics.Deserialize("{\"StartedOn\":\"broken\",\"Days\":{}}"); } catch { rejected = true; }
        Check(rejected, "corrupt history fails instead of silently losing records");
        var multi = new DailyStatistics(); multi.Record(start, start.AddDays(2).AddSeconds(90), 250);
        Check(multi.Days.Values.Sum(x => x.RuntimeSeconds) == 172890, "multi-day allocation conserves runtime");
        var unit = new HSUnit { XP = new RewardXP { Level = 2, ProgressXP = 100 }, TotalGaintXP = 500, TotalRunningTime = 3600 };
        var update = typeof(HSUnit).GetMethod("XPUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        update.Invoke(unit, new object[] { new RewardXP { Level = 2, ProgressXP = 200 } });
        Check(unit.TotalGaintXP == 500 && unit.DailyStats.Days.Count == 0, "first sample after restart only establishes baseline");
        unit.LastXPUpdateTime = DateTime.Now.AddSeconds(-60);
        update.Invoke(unit, new object[] { new RewardXP { Level = 2, ProgressXP = 250 } });
        Check(unit.TotalGaintXP == 550 && unit.DailyStats.Get(DateTime.Today).Xp == 50, "collector updates daily and historical XP together");
        unit.LastXPUpdateTime = DateTime.Now.AddSeconds(-60);
        update.Invoke(unit, new object[] { new RewardXP { Level = 1, ProgressXP = 0 } });
        Check(unit.TotalGaintXP == 550, "reward track reset never creates negative earnings");
        unit.Enable = false; Check(unit.LastXPUpdateTime == DateTime.MaxValue, "disable closes sampling interval");
        var clone = (HSUnit)unit.DeepClone(); clone.DailyStats.Get(DateTime.Today).Xp += 1;
        Check(unit.DailyStats.Get(DateTime.Today).Xp == 50, "account editing clones daily history independently");

        var logUnit = new HSUnit { ID = "log-fixture", HSPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "synthetic", "Hearthstone.exe") };
        typeof(HSUnit).GetField("m_hsLogFileDir", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(logUnit, "synthetic");
        var watermarks = (DateTime[])typeof(HSUnit).GetField("m_fileLastEdit", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(logUnit);
        for (int i = 0; i < watermarks.Length; i++) watermarks[i] = DateTime.Today;
        string logFolder = Path.Combine(Path.GetDirectoryName(logUnit.HSPath), "BepinEx", "Log", logUnit.ID, "battlegrounds");
        Directory.CreateDirectory(logFolder);
        string stamp = DateTime.Now.ToString("HH:mm:ss.fff");
        File.WriteAllText(Path.Combine(logFolder, "battlegrounds@" + DateTime.Today.ToString("yyyy-MM-dd") + ".log"),
            stamp + "\t[经验变动] 完成任务，传统通行证，获得经验:100\n" + stamp + " 战令信息 等级:2 经验:200\n");
        logUnit.ReadBGLog();
        Check(logUnit.XP.ProgressXP == 200 && logUnit.TotalGaintXP_Quest == 0 && logUnit.DailyStats.Days.Count == 0, "first log snapshot does not invent source gains");

        // The test executable lives in an isolated copy of the build. Seed only its config.
        var manager = HSUnitManager.Get(); manager.Init(() => {}, false);
        for (int i = 0; i < 3; i++) {
            var account = new HSUnit { ID = "daily-test-" + i, Token = "synthetic-token", HSPath = @"D:\synthetic\Hearthstone.exe", HSModPort = 59740 + i,
                XP = new RewardXP { Level = 2, ProgressXP = 100 }, TotalGaintXP = 1000, TotalRunningTime = 3600 };
            account.Tasks.Add(new TaskUnit { StartTime = DateTime.Today.AddHours(8), StopTime = DateTime.Today.AddHours(18) });
            if (i == 0) {
                account.DailyStats.Record(DateTime.Today.AddDays(-2).AddHours(1), DateTime.Today.AddDays(-2).AddHours(2), 800);
                account.DailyStats.Record(DateTime.Today.AddHours(1), DateTime.Today.AddHours(3), 2000);
                account.DailyStats.Classify(DateTime.Today, 400, true);
                account.DailyStats.Classify(DateTime.Today, 200, false);
                account.TotalGaintXP += 2800; account.TotalRunningTime += 10800; account.TotalGaintXP_Quest = 400;
                account.TotalGaintXP_Other = 200;
            }
            manager.Add(account);
        }
        manager.Release();
        var saved = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var section = (HSUnitSection)saved.GetSection("userinfo");
        var first = section.HSUnit.Cast<HSUnitElement>().Single(x => x.ID == "daily-test-0");
        Check(DailyStatistics.Deserialize(first.DailyStats).Get(DateTime.Today).Xp == 2000, "daily history persisted through actual XML save and reload");
        Check(first.TotalGaintXP == 3800, "legacy cumulative totals preserved with daily history");
        File.Copy(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HSCentric.exe.config"), true);
        Console.WriteLine("Daily statistics tests completed; isolated API fixture ready.");
    }
}
