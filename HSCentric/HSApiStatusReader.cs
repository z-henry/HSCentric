using System;

namespace HSCentric
{
	internal sealed class HSApiStatusReader : IHSStatusReader
	{
		public HSApiStatusReader(HSStatusReaderContext context)
		{
			m_client = new HSApiClient(context.UnitId, context.HSModPort);
		}

		public bool TryReadMercenaryStatus(out MercenaryStatusSnapshot status)
		{
			status = null;
			RewardXP rewardXP;
			if (!TryBuildRewardXP(out rewardXP))
				return false;

			status = new MercenaryStatusSnapshot
			{
				RewardXP = rewardXP
			};
			return true;
		}

		public bool TryReadBattlegroundsStatus(out BattlegroundsStatusSnapshot status)
		{
			status = new BattlegroundsStatusSnapshot();
			bool hasValue = false;

			RewardXP rewardXP;
			if (TryBuildRewardXP(out rewardXP))
			{
				status.RewardXP = rewardXP;
				hasValue = true;
			}

			int? pvpRate;
			if (m_client.TryGetBattlegroundsPvp(out pvpRate) && pvpRate.HasValue)
			{
				status.PvpRate = pvpRate.Value;
				hasValue = true;
			}

			if (!hasValue)
			{
				status = null;
				return false;
			}
			return true;
		}

		public bool TryReadMercenaryRecord(out MercenaryRecordSnapshot status)
		{
			status = null;
			int? pvpRate;
			if (!m_client.TryGetMercenaryPvp(out pvpRate) || !pvpRate.HasValue)
				return false;

			status = new MercenaryRecordSnapshot
			{
				PvpRate = pvpRate.Value
			};
			return true;
		}

		public bool TryReadBuddyStatus(out BuddyStatusSnapshot status)
		{
			status = new BuddyStatusSnapshot();
			bool hasValue = false;

			RewardXP rewardXP;
			if (TryBuildRewardXP(out rewardXP))
			{
				status.RewardXP = rewardXP;
				hasValue = true;
			}

			HSApiClient.ConstructedInfo info;
			if (m_client.TryGetConstructedInfo(out info))
			{
				string classicRate = BuildConstructedRate(info);
				if (!string.IsNullOrWhiteSpace(classicRate))
				{
					status.ClassicRate = classicRate;
					hasValue = true;
				}
			}

			if (!hasValue)
			{
				status = null;
				return false;
			}
			return true;
		}

		public bool TryCallConcedeAndClose()
		{
			return m_client.TryCallConcedeAndClose();
		}

		private bool TryBuildRewardXP(out RewardXP rewardXP)
		{
			rewardXP = null;
			HSApiClient.PassInfo pass;
			if (!m_client.TryGetPassInfo(out pass))
				return false;

			if (pass.Level.HasValue && pass.ProgressXp.HasValue)
			{
				rewardXP = new RewardXP
				{
					Level = pass.Level.Value,
					ProgressXP = pass.ProgressXp.Value
				};
				return true;
			}

			if (pass.IsMax == true)
			{
				rewardXP = new RewardXP
				{
					Level = 400,
					ProgressXP = 0
				};
				return true;
			}

			return false;
		}

		private static string BuildConstructedRate(HSApiClient.ConstructedInfo info)
		{
			if (info == null)
				return "";
			if (!string.IsNullOrWhiteSpace(info.ClassicRate))
				return info.ClassicRate;
			if (!string.IsNullOrWhiteSpace(info.StandardText) && !string.IsNullOrWhiteSpace(info.WildText))
				return $"标准:{info.StandardText} 狂野:{info.WildText}";
			if (!string.IsNullOrWhiteSpace(info.StandardText))
				return $"标准:{info.StandardText}";
			if (!string.IsNullOrWhiteSpace(info.WildText))
				return $"狂野:{info.WildText}";
			return "";
		}

		private readonly HSApiClient m_client;
	}
}