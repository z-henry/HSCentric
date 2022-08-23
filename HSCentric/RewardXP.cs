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
		private int[] allNeedXp ={
			0,     100,   200,   350,   500,   725,   950,   1250,  1550,  1875,//1-10
            2200,  2550,  2900,  3275,  3650,  4050,  4450,  4875,  5300,  5750,
			6200,  6750,  7350,  8000,  8675,  9350,  10225, 11100, 12100, 13200,
			14400, 15600, 16850, 18100, 19400, 20700, 22050, 23400, 24800, 26200,
			27650, 29100, 30600, 32100, 33650, 35200, 36800, 38400, 40050, 41700,
			43400, 45100, 46850, 48600, 50400, 52200, 54050, 55900, 57800, 59700,
			61650, 63600, 65600, 67600, 69650, 71700, 73825, 75950, 78200, 80450,
			82825, 85200, 87700, 90200, 92700, 95200, 97700, 100200,102700,105200,
			107700,110200,112700,115200,117700,120200,122700,125200,127700,130200,
			132700,135200,137700,140200,142700,145200,147700,150200,152700,155200,//90-100
            156525,157850,159175,160500,161825,163150,164475,165800,167125,168450,
			169775,171100,172425,173750,175075,176550,178025,179500,180975,182450,
			183925,185400,186875,188350,189825,191300,192775,194250,195725,197200
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
				int total;
				if (m_Level > 130)
					total = allNeedXp[129] + (m_Level - 130) * 1500 + m_ProgressXP;
				else 
					total = allNeedXp[m_Level - 1] + m_ProgressXP;

				return total; 
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
