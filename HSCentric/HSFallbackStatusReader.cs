using System;

namespace HSCentric
{
	internal sealed class HSFallbackStatusReader : IHSStatusReader
	{
		public HSFallbackStatusReader(IHSStatusReader primaryReader, IHSStatusReader fallbackReader)
		{
			m_primaryReader = primaryReader;
			m_fallbackReader = fallbackReader;
		}

		public bool TryReadMercenaryStatus(out MercenaryStatusSnapshot status)
		{
			status = null;
			MercenaryStatusSnapshot primaryStatus;
			if (m_primaryReader.TryReadMercenaryStatus(out primaryStatus) && primaryStatus != null && primaryStatus.RewardXP != null)
			{
				status = primaryStatus;
				return true;
			}
			return m_fallbackReader.TryReadMercenaryStatus(out status);
		}

		public bool TryReadBattlegroundsStatus(out BattlegroundsStatusSnapshot status)
		{
			status = null;
			BattlegroundsStatusSnapshot primaryStatus;
			BattlegroundsStatusSnapshot fallbackStatus;
			bool hasPrimary = m_primaryReader.TryReadBattlegroundsStatus(out primaryStatus);
			bool hasFallback = m_fallbackReader.TryReadBattlegroundsStatus(out fallbackStatus);
			if (!hasPrimary && !hasFallback)
				return false;

			status = new BattlegroundsStatusSnapshot
			{
				RewardXP = primaryStatus != null ? primaryStatus.RewardXP : fallbackStatus != null ? fallbackStatus.RewardXP : null,
				PvpRate = primaryStatus != null && primaryStatus.PvpRate.HasValue ? primaryStatus.PvpRate : fallbackStatus != null ? fallbackStatus.PvpRate : null,
				QuestXpGain = primaryStatus != null && primaryStatus.QuestXpGain != 0 ? primaryStatus.QuestXpGain : fallbackStatus != null ? fallbackStatus.QuestXpGain : 0,
				AchievementXpGain = primaryStatus != null && primaryStatus.AchievementXpGain != 0 ? primaryStatus.AchievementXpGain : fallbackStatus != null ? fallbackStatus.AchievementXpGain : 0,
				OtherXpGain = primaryStatus != null && primaryStatus.OtherXpGain != 0 ? primaryStatus.OtherXpGain : fallbackStatus != null ? fallbackStatus.OtherXpGain : 0,
			};

			return status.RewardXP != null ||
				status.PvpRate.HasValue ||
				status.QuestXpGain != 0 ||
				status.AchievementXpGain != 0 ||
				status.OtherXpGain != 0;
		}

		public bool TryReadMercenaryRecord(out MercenaryRecordSnapshot status)
		{
			status = null;
			MercenaryRecordSnapshot primaryStatus;
			if (m_primaryReader.TryReadMercenaryRecord(out primaryStatus) && primaryStatus != null && primaryStatus.PvpRate.HasValue)
			{
				status = primaryStatus;
				return true;
			}
			return m_fallbackReader.TryReadMercenaryRecord(out status);
		}

		public bool TryReadBuddyStatus(out BuddyStatusSnapshot status)
		{
			status = null;
			BuddyStatusSnapshot primaryStatus;
			BuddyStatusSnapshot fallbackStatus;
			bool hasPrimary = m_primaryReader.TryReadBuddyStatus(out primaryStatus);
			bool hasFallback = m_fallbackReader.TryReadBuddyStatus(out fallbackStatus);
			if (!hasPrimary && !hasFallback)
				return false;

			string classicRate = "";
			if (primaryStatus != null && !string.IsNullOrWhiteSpace(primaryStatus.ClassicRate))
				classicRate = primaryStatus.ClassicRate;
			else if (fallbackStatus != null && !string.IsNullOrWhiteSpace(fallbackStatus.ClassicRate))
				classicRate = fallbackStatus.ClassicRate;

			status = new BuddyStatusSnapshot
			{
				RewardXP = primaryStatus != null ? primaryStatus.RewardXP : fallbackStatus != null ? fallbackStatus.RewardXP : null,
				ClassicRate = classicRate,
				HasAnomaly = (primaryStatus != null && primaryStatus.HasAnomaly) || (fallbackStatus != null && fallbackStatus.HasAnomaly)
			};

			return status.RewardXP != null ||
				!string.IsNullOrWhiteSpace(status.ClassicRate) ||
				status.HasAnomaly;
		}

		public bool TryCallConcedeAndClose()
		{
			if (m_primaryReader.TryCallConcedeAndClose())
				return true;
			return m_fallbackReader.TryCallConcedeAndClose();
		}

		private readonly IHSStatusReader m_primaryReader;
		private readonly IHSStatusReader m_fallbackReader;
	}
}