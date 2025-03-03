using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace HSCentric
{
	[Serializable]
	public class RewardXP
	{
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
				int totalxp = XPTools.Instance.CalculateTotalExperience(m_Level, m_ProgressXP);
				return totalxp;
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
		private int m_Level = 1;
	}
}
