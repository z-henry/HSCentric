using HSCentric.Const;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HSCentric
{
	internal sealed class HSLogStatusReader : IHSStatusReader
	{
		public HSLogStatusReader(HSStatusReaderContext context)
		{
			m_context = context;
		}

		public bool TryReadMercenaryStatus(out MercenaryStatusSnapshot status)
		{
			status = null;
			FileInfo targetFile;
			if (!TryGetLatestUpdatedFile(m_context.GetMercenaryLogDirectory(), "mercenarylog@*.log", FILE_TYPE.佣兵日志, out targetFile))
				return false;

			foreach (string line in File.ReadLines(targetFile.FullName).Reverse<string>())
			{
				if (line.IndexOf("战令信息") <= 0)
					continue;

				Match match = Regex.Match(line, @"^.*等级:([\d]*).*经验:([\d]*).*$");
				if (match.Groups.Count != 3)
					return false;

				status = new MercenaryStatusSnapshot
				{
					RewardXP = new RewardXP
					{
						Level = Convert.ToInt32(match.Groups[1].Value),
						ProgressXP = Convert.ToInt32(match.Groups[2].Value),
					}
				};
				return true;
			}

			return false;
		}

		public bool TryReadBattlegroundsStatus(out BattlegroundsStatusSnapshot status)
		{
			status = null;
			string rootPath = m_context.GetBattlegroundsLogDirectory();
			if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
				return false;

			string todayPattern = $"battlegrounds@{DateTime.Now:yyyy-MM-dd}.log";
			FileInfo targetFile = new DirectoryInfo(rootPath)
				.GetFiles(todayPattern, SearchOption.TopDirectoryOnly)
				.FirstOrDefault();
			if (targetFile == null)
				return false;

			DateTime lastCheckTime = m_context.GetLastReadTime(FILE_TYPE.酒馆日志);
			if (targetFile.LastWriteTime <= lastCheckTime)
				return false;
			m_context.SetLastReadTime(FILE_TYPE.酒馆日志, targetFile.LastWriteTime);

			List<string> lines = ReadAllLinesShared(targetFile.FullName, Encoding.UTF8);
			status = new BattlegroundsStatusSnapshot();
			bool hasValue = false;

			for (int i = lines.Count - 1; i >= 0; i--)
			{
				Match match = Regex.Match(lines[i], @"(\d{2}:\d{2}:\d{2}\.\d{3}).*战令信息.*等级:(\d+) 经验:(\d+)");
				if (!match.Success)
					continue;

				DateTime current;
				if (TryParseLogTime(match.Groups[1].Value, out current) && current > lastCheckTime)
				{
					status.RewardXP = new RewardXP
					{
						Level = int.Parse(match.Groups[2].Value),
						ProgressXP = int.Parse(match.Groups[3].Value),
					};
					hasValue = true;
				}
				break;
			}

			for (int i = lines.Count - 1; i >= 0; i--)
			{
				Match match = Regex.Match(lines[i], @"^(\d{2}:\d{2}:\d{2}\.\d{3})\t\[经验变动\] (.*)，传统通行证，获得经验:(\d+)$");
				if (!match.Success)
					continue;

				DateTime current;
				if (!TryParseLogTime(match.Groups[1].Value, out current) || current <= lastCheckTime)
					continue;

				int xp = int.Parse(match.Groups[3].Value);
				string desc = match.Groups[2].Value;
				if (desc.Contains("完成任务"))
					status.QuestXpGain += xp;
				else if (desc.Contains("完成成就"))
					status.AchievementXpGain += xp;
				else if (!desc.Contains("完成对局"))
					status.OtherXpGain += xp;
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
			FileInfo targetFile;
			if (!TryGetLatestUpdatedFile(m_context.GetGameRecordDirectory(), "gamerecord@*.log", FILE_TYPE.佣兵对局日志, out targetFile))
				return false;

			string[] lines = File.ReadAllLines(targetFile.FullName);
			if (lines.Length == 0)
				return false;

			string[] lineSplit = lines.Last().Split('\t');
			int pvpRate;
			if (lineSplit.Length < 3 || !int.TryParse(lineSplit[2], out pvpRate))
				return false;

			status = new MercenaryRecordSnapshot
			{
				PvpRate = pvpRate
			};
			return true;
		}

		public bool TryReadBuddyStatus(out BuddyStatusSnapshot status)
		{
			status = null;
			FileInfo targetFile;
			if (!TryGetLatestUpdatedFile(m_context.GetHBLogDirectory(), "Hearthbuddy*.txt", FILE_TYPE.兄弟日志, out targetFile))
				return false;

			status = new BuddyStatusSnapshot();
			int anomalyCount = 0;
			bool hasValue = false;
			Encoding gb2312 = Encoding.GetEncoding("GB2312");
			foreach (string line in File.ReadLines(targetFile.FullName, gb2312).Reverse<string>())
			{
				if (line.IndexOf("[监控插件] 合计: 战令") > 0)
				{
					Match match = Regex.Match(line, @"^.*合计: 战令([\d]*)级\(([\d]*)/[\d]*\)\([\d]*/小时\)\s(.*)\s[\d]*/[\d]*.*$");
					if (match.Groups.Count == 4)
					{
						status.RewardXP = new RewardXP
						{
							Level = Convert.ToInt32(match.Groups[1].Value),
							ProgressXP = Convert.ToInt32(match.Groups[2].Value),
						};
						status.ClassicRate = match.Groups[3].Value;
						hasValue = true;
					}
					break;
				}

				if (line.Contains("检测到异常情况，将随机点击"))
				{
					anomalyCount++;
					if (anomalyCount >= 5)
					{
						status.HasAnomaly = true;
						hasValue = true;
						break;
					}
				}
				else
				{
					anomalyCount = 0;
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
			return false;
		}

		private bool TryGetLatestUpdatedFile(string directoryPath, string searchPattern, FILE_TYPE fileType, out FileInfo targetFile)
		{
			targetFile = null;
			if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
				return false;

			List<FileInfo> files = new DirectoryInfo(directoryPath)
				.GetFiles(searchPattern, SearchOption.TopDirectoryOnly)
				.ToList();
			targetFile = files.OrderByDescending(x => x.LastWriteTime.Ticks).FirstOrDefault();
			if (targetFile == null)
				return false;

			if (targetFile.LastWriteTime <= m_context.GetLastReadTime(fileType))
				return false;
			m_context.SetLastReadTime(fileType, targetFile.LastWriteTime);
			return true;
		}

		private static List<string> ReadAllLinesShared(string filePath, Encoding encoding)
		{
			List<string> lines = new List<string>();
			using (FileStream fs = new FileStream(
				filePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete))
			using (StreamReader reader = new StreamReader(fs, encoding))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					lines.Add(line);
				}
			}
			return lines;
		}

		private static bool TryParseLogTime(string text, out DateTime current)
		{
			current = DateTime.MinValue;
			DateTime parsedTime;
			if (!DateTime.TryParseExact(text, "HH:mm:ss.fff", null, DateTimeStyles.None, out parsedTime))
				return false;
			current = DateTime.Today.Add(parsedTime.TimeOfDay);
			return true;
		}

		private readonly HSStatusReaderContext m_context;
	}
}