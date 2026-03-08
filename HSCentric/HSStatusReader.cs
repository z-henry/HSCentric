using HSCentric.Const;
using System;
using System.IO;

namespace HSCentric
{
	internal interface IHSStatusReader
	{
		bool TryReadMercenaryStatus(out MercenaryStatusSnapshot status);
		bool TryReadBattlegroundsStatus(out BattlegroundsStatusSnapshot status);
		bool TryReadMercenaryRecord(out MercenaryRecordSnapshot status);
		bool TryReadBuddyStatus(out BuddyStatusSnapshot status);
		bool TryCallConcedeAndClose();
	}

	internal sealed class MercenaryStatusSnapshot
	{
		public RewardXP RewardXP { get; set; }
	}

	internal sealed class BattlegroundsStatusSnapshot
	{
		public RewardXP RewardXP { get; set; }
		public int? PvpRate { get; set; }
		public int QuestXpGain { get; set; }
		public int AchievementXpGain { get; set; }
		public int OtherXpGain { get; set; }
	}

	internal sealed class MercenaryRecordSnapshot
	{
		public int? PvpRate { get; set; }
	}

	internal sealed class BuddyStatusSnapshot
	{
		public RewardXP RewardXP { get; set; }
		public string ClassicRate { get; set; } = "";
		public bool HasAnomaly { get; set; }
	}

	internal sealed class HSStatusReaderContext
	{
		public HSStatusReaderContext(
			string unitId,
			string hsPath,
			string hbPath,
			int hsModPort,
			Func<FILE_TYPE, DateTime> getLastReadTime,
			Action<FILE_TYPE, DateTime> setLastReadTime)
		{
			UnitId = unitId ?? "";
			HSPath = hsPath ?? "";
			HBPath = hbPath ?? "";
			HSModPort = hsModPort;
			m_getLastReadTime = getLastReadTime;
			m_setLastReadTime = setLastReadTime;
		}

		public string UnitId { get; private set; }
		public string HSPath { get; private set; }
		public string HBPath { get; private set; }
		public int HSModPort { get; private set; }

		public DateTime GetLastReadTime(FILE_TYPE fileType)
		{
			if (m_getLastReadTime == null)
				return DateTime.MinValue;
			return m_getLastReadTime(fileType);
		}

		public void SetLastReadTime(FILE_TYPE fileType, DateTime value)
		{
			if (m_setLastReadTime == null)
				return;
			m_setLastReadTime(fileType, value);
		}

		public string GetMercenaryLogDirectory()
		{
			return CombineWithHSRoot("BepinEx", "Log", UnitId, "mercenarylog");
		}

		public string GetBattlegroundsLogDirectory()
		{
			return CombineWithHSRoot("BepinEx", "Log", UnitId, "battlegrounds");
		}

		public string GetGameRecordDirectory()
		{
			return CombineWithHSRoot("BepinEx", "Log", UnitId);
		}

		public string GetHBLogDirectory()
		{
			if (string.IsNullOrEmpty(HBPath))
				return "";

			string hbDirectory = Path.GetDirectoryName(HBPath);
			if (string.IsNullOrEmpty(hbDirectory))
				return "";

			return Path.Combine(hbDirectory, "Logs");
		}

		private string CombineWithHSRoot(params string[] segments)
		{
			if (string.IsNullOrEmpty(HSPath))
				return "";

			string hsDirectory = Path.GetDirectoryName(HSPath);
			if (string.IsNullOrEmpty(hsDirectory))
				return "";

			string result = hsDirectory;
			foreach (string segment in segments)
			{
				result = Path.Combine(result, segment);
			}
			return result;
		}

		private readonly Func<FILE_TYPE, DateTime> m_getLastReadTime;
		private readonly Action<FILE_TYPE, DateTime> m_setLastReadTime;
	}

	internal static class HSStatusReaderFactory
	{
		public static IHSStatusReader Create(HSStatusReaderContext context)
		{
			return new HSFallbackStatusReader(
				new HSApiStatusReader(context),
				new HSLogStatusReader(context));
		}
	}
}