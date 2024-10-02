using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace HSCentric
{
	[Serializable]
	public class RewardXP
	{
		private static readonly Dictionary<int, int> CumulativeExpTable = new Dictionary<int, int>
		{
		{1, 0},
		{2, 100},
		{3, 375},
		{4, 700},
		{5, 1075},
		{6, 1475},
		{7, 1925},
		{8, 2425},
		{9, 2975},
		{10, 3600},
		{11, 4275},
		{12, 5000},
		{13, 5800},
		{14, 6650},
		{15, 7550},
		{16, 8500},
		{17, 9525},
		{18, 10600},
		{19, 11725},
		{20, 12900},
		{21, 14100},
		{22, 15350},
		{23, 16650},
		{24, 17975},
		{25, 19325},
		{26, 20700},
		{27, 22100},
		{28, 23525},
		{29, 24975},
		{30, 26425},
		{31, 27900},
		{32, 29400},
		{33, 30900},
		{34, 32400},
		{35, 33925},
		{36, 35450},
		{37, 36975},
		{38, 38525},
		{39, 40075},
		{40, 41625},
		{41, 43175},
		{42, 44725},
		{43, 46275},
		{44, 47825},
		{45, 49375},
		{46, 50950},
		{47, 52525},
		{48, 54100},
		{49, 55675},
		{50, 57250}
		};

		public object DeepClone()
		{
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, this); //复制到流中
			ms.Position = 0;
			return (bf.Deserialize(ms));
		}
		public int TotalXP
		{
			get 
			{
				int cumulative = 0;

				if (m_Level <= 50)
				{
					// 等级 1 到 50
					cumulative = CumulativeExpTable[m_Level];
				}
				else
				{
					// 等级 51 到 400
					cumulative = CumulativeExpTable[50]; // 等级50的累计经验

					// 计算等级51到当前等级-1的经验
					int additionalLevels = m_Level - 50;
					int stepSize = 25;
					int levelsPerStep = 50;
					int stepIndex = (additionalLevels - 1) / levelsPerStep; // 0-based index

					// 确保stepIndex不超过7（因为1500是最终经验值）
					if (stepIndex >= 7)
						stepIndex = 7;

					// 基础经验值
					int baseExp = 1325;

					// 计算完整步骤的经验
					for (int s = 0; s < stepIndex; s++)
					{
						int expPerLevel = baseExp + stepSize * s;
						cumulative += levelsPerStep * expPerLevel;
					}

					// 当前步骤的经验
					int currentStepExp = stepIndex < 7 ? baseExp + stepSize * stepIndex : 1500;

					// 当前步骤中已经完成的等级数
					int levelsInCurrentStep = additionalLevels - (stepIndex * levelsPerStep);
					cumulative += levelsInCurrentStep * currentStepExp;
				}

				// 总经验值
				return cumulative + m_ProgressXP;
			}
		}
		public int ProgressXP 
		{
			get { return m_ProgressXP; }
			set { m_ProgressXP = value; }
		}
		public int Level
		{
			get { return m_Level; }
			set { m_Level = value; }
		}

		private int m_ProgressXP = 0;
		private int m_Level = 2;
	}
}
