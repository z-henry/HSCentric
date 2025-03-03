using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace HSCentric
{
	internal class XPTools
	{

		public static XPTools Instance
		{
			get { return m_Instance ?? (m_Instance = new XPTools()); }
		}

		private bool InitList()
		{
			if (m_LevelExpNeededList.Count != 0)
				return true;

			string json = File.ReadAllText("Globle_LevelExpNeededList.json");
			dynamic jsonObject = JsonConvert.DeserializeObject(json);
			m_LevelExpNeededList = jsonObject.XPList.ToObject<List<int>>();
			return true;
		}
		public int CalculateTotalExperience(int currentLevel, int currentProgress)
		{
			if (false == InitList())
				return -1;

			// 确保当前等级在合法范围内
			if (currentLevel < 1 || currentLevel > m_LevelExpNeededList.Count)
				return -1;

			// 计算从等级1到当前等级的总经验
			int totalExperience = 0;
			for (int i = 0; i < currentLevel - 1; i++)
				totalExperience += m_LevelExpNeededList[i];

			// 加上当前等级的经验进度
			totalExperience += currentProgress;

			return totalExperience;
		}


		private List<int> m_LevelExpNeededList = new List<int>();//每级所需经验
		private static XPTools m_Instance;
	}

}
