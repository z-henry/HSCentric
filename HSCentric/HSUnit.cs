using HSCentric.Const;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HSCentric
{
	[Serializable]
	public class HSUnit
	{
		public HSUnit()
		{
			m_taskManager = new TaskManager(this, null, new TaskUnit(), false);
		}

		[Serializable]
		public class CacheConfig
		{
			public string mode = "";
			public DateTime awakeTime = new DateTime(2000, 1, 1);
			public int awakePeriod = 25;
			public string teamName = "";
			public string strategyName = "";
			public bool mercPluginEnable = false;
			public bool scale = false;
			public string map = "2-5";
			public int numCore = 0;
			public int numTotal = 6;
		}

		public object DeepClone()
		{
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, this); //复制到流中
			ms.Position = 0;
			return (bf.Deserialize(ms));
		}

		public CacheConfig BasicConfigValue
		{
			get
			{
				m_cacheConfig = ReadConfigValue() ?? m_cacheConfig;
				return m_cacheConfig;
			}
		}

		public bool Enable
		{
			get { return m_enable; }
			set { m_enable = value; }
		}

		public string ID
		{
			get { return m_ID; }
			set { m_ID = value; }
		}

		public string Token
		{
			get { return m_token; }
			set { m_token = value; }
		}

		public string HBPath
		{
			get { return m_hbPath; }
			set { m_hbPath = value; }
		}

		public string HSPath
		{
			get { return m_hsPath; }
			set { m_hsPath = value; }
		}

		public TaskUnit CurrentTask
		{
			get { return Tasks.GetCurrentTask(); }
		}

		public TaskManager Tasks
		{
			get { return m_taskManager; }
			set { m_taskManager = value; }
		}

		public RewardXP XP
		{
			get { return m_rewardXP; }
			set { m_rewardXP = value; }
		}

		public string ClassicRate
		{
			get { return m_classicRate; }
			set { m_classicRate = value; }
		}

		public int MercPvpRate
		{
			get { return m_pvpRate; }
			set { m_pvpRate = value; }
		}

		public int HSModPort
		{
			get { return m_hsmodPort; }
			set { m_hsmodPort = value; }
		}

		public int StatsMonth
		{
			get { return m_statsMonth; }
			set { m_statsMonth = value; }
		}

		public void InitConfig()
		{
			InitMercPlugin();
			InitHsMod();
		}

		private void InitMercPlugin()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
			{
				// 获取文件的目录路径
				string directory = Path.GetDirectoryName(pathConfig.ToString());

				// 如果目录不存在，创建目录
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				// 创建文件并写入空内容或初始内容，UTF-8 编码
				using (StreamWriter sw = new StreamWriter(pathConfig.ToString(), false, new UTF8Encoding(false)))
				{
					sw.Write("");  // 可以在这里写入默认内容，如果不需要可以留空
				}
			}

			MyConfig.WriteIniValue("配置", "是否自动升级技能", true.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("配置", "是否自动制作佣兵", true.ToString(), pathConfig.ToString());
		}

		private void InitHsMod()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (false == File.Exists(pathConfig.ToString()))
			{
				// 获取文件的目录路径
				string directory = Path.GetDirectoryName(pathConfig.ToString());

				// 如果目录不存在，创建目录
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				// 创建文件并写入空内容或初始内容，UTF-8 编码
				using (StreamWriter sw = new StreamWriter(pathConfig.ToString(), false, new UTF8Encoding(false)))
				{
					sw.Write("");  // 可以在这里写入默认内容，如果不需要可以留空
				}
			}

			MyConfig.WriteIniValue("全局", "HsMod状态", true.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("全局", "设置模板", "AwayFromKeyboard", pathConfig.ToString());
// 			MyConfig.WriteIniValue("全局", "游戏帧率", "-1", pathConfig.ToString());
// 			MyConfig.WriteIniValue("炉石", "快速战斗", true.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("开发", "网站端口", m_hsmodPort.ToString(), pathConfig.ToString());
		}

		public void ReleaseConfig()
		{
			ReleaseMercPlugin();
			ReleaseHsMod();
		}

		private void ReleaseMercPlugin()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (true == File.Exists(pathConfig.ToString()))
				MyConfig.WriteIniValue("配置", "插件开关", false.ToString(), pathConfig.ToString());
		}

		private void ReleaseHsMod()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (true == File.Exists(pathConfig.ToString()))
				MyConfig.WriteIniValue("全局", "HsMod状态", false.ToString(), pathConfig.ToString());
		}


		public bool IsActive()
		{
			TaskUnit currentTask = CurrentTask;
			TimeSpan time_now = DateTime.Now.TimeOfDay;
			return time_now >= currentTask.StartTime.TimeOfDay && time_now <= currentTask.StopTime.TimeOfDay;
		}

		public bool IsProcessAlive()
		{
			if (HearthstoneProcess() != null)
			{
				return true;
			}
			else
			{
				m_pid = 0;
				return false;
			}
		}

		public bool IsLogUpdated()
		{
			return LogsUpdated(m_hsLogFileDir) && LogsUpdated(System.IO.Path.GetDirectoryName(m_hbPath) + "/Logs");
		}

		public bool LogsUpdated(string log_dir)
		{
			if (string.IsNullOrEmpty(log_dir))
				return true;

			// 如果目录不存在，返回 false
			if (!Directory.Exists(log_dir))
				return false;

			string[] logFiles = Directory.GetFiles(log_dir);
			double interval = 5f;

			// 遍历每个文件，检查修改时间
			foreach (string filePath in logFiles)
			{
				FileInfo logFile = new FileInfo(filePath);
				TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - logFile.LastWriteTime.Ticks);

				// 如果有文件在指定间隔内被修改过，返回 true
				if (timeSpan.TotalMinutes < interval)
				{
					return true;
				}
			}
			// 如果没有任何文件在指定间隔内被修改过，返回 false
			return false;
		}

		public void KillHS(string msg = "")
		{
			HearthstoneProcess()?.Kill();
			Out.Log(string.Format("[{0}]结束 {1}", ID, msg));
			m_pid = 0;
			m_hsLogFileDir = "";
			Common.Delay(5 * 1000);
		}

		public async void Start(string msg, bool need_hb)
		{
			StartHS(msg);
			int pid = m_pid;
			await Delay(30*1000);
			if (pid != m_pid)
				return;
			m_hsLogFileDir = GetHSLogPath();
			Out.Log(string.Format("[{0}]记录炉石日志路径 {1}", ID, m_hsLogFileDir));

			if (true == NeedUpdateHS())
				HSUnitManager.Get().InterruptBeforeUpdate();

			if (need_hb)
			{
				int try_count = 0;
				while(false == HSSuccessLogin())
				{

					int delay = GetQueueSec();
					if (delay < 30)
						try_count++;
					if (try_count >= 5)
					{
						Out.Log(string.Format("[{0}]HS登录检测失败", ID, try_count));
						KillHS("HS登录检测失败");
						return;
					}

					if (delay < 0)
						Out.Log(string.Format("[{0}]HS启动等待", ID, try_count));
					else
						Out.Log(string.Format("[{0}]HS排队等待：{2}秒", ID, try_count, delay));
					await Delay(30 * 1000);
					if (pid != m_pid)
						return;
				}

				StartHB(msg);
			}
		}

		public void StartHS(string msg = "")
		{
			Process process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = m_hsPath;
			process.StartInfo.Arguments += " " + m_token;
			process.StartInfo.Arguments += " --hsunitid:" + m_ID;
			process.StartInfo.Arguments += " --startmethod:hscentric";
			process.Start();
			process.WaitForInputIdle();
			m_pid = process.Id;
			m_hsLogFileDir = "";
			Out.Log(string.Format("[{0}]启动 {1} [pid:{2}]", ID, msg, m_pid));
		}

		public void StartHB(string msg = "")
		{
			var currentTask = CurrentTask;
			Process process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = HBPath;
			process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(HBPath);
			process.StartInfo.Arguments = "--autostart --config:Default";
			process.StartInfo.Arguments += " --pid:" + m_pid.ToString();
			process.StartInfo.Arguments += " --deck:" + currentTask.TeamName;
			process.StartInfo.Arguments += " --behavior:" + ((int)(BEHAVIOR_MODE)Enum.Parse(typeof(BEHAVIOR_MODE), currentTask.StrategyName)).ToString();
			process.StartInfo.Arguments += " --rule:" + ((int)currentTask.Mode - (int)TASK_MODE.狂野).ToString();
			process.StartInfo.Arguments += " --os:10";
			process.Start();
			process.WaitForInputIdle();
			int pid = process.Id;

			Out.Log(string.Format("[{0}]启动HB {1} [pid:{2}] [arg:{3}]", ID, msg, pid, process.StartInfo.Arguments));
		}

		public bool NeedAdjustMode()
		{
			var basicConfigValue = BasicConfigValue;
			TaskUnit currentTask = CurrentTask;
			if (Common.IsBuddyMode(currentTask.Mode))
			{
				if (basicConfigValue.mercPluginEnable == true)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				if (basicConfigValue.mercPluginEnable == false ||
					currentTask.Mode.ToString() != basicConfigValue.mode ||
					currentTask.TeamName != basicConfigValue.teamName ||
					currentTask.StrategyName != basicConfigValue.strategyName ||
					currentTask.Scale != basicConfigValue.scale ||
					currentTask.Map != basicConfigValue.map ||
					currentTask.MercTeamNumCore != basicConfigValue.numCore ||
					currentTask.MercTeamNumTotal != basicConfigValue.numTotal)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public bool AdjustMode()
		{
			TaskUnit currentTask = CurrentTask;
			WriteConfigValue(!Common.IsBuddyMode(currentTask.Mode), currentTask);
			var basicConfigValue = BasicConfigValue;
			return true;
		}

		private Process HearthstoneProcess()
		{
			if (m_pid == 0)
				return null;

			Process target = null;
			try
			{
				target = Process.GetProcessById(m_pid);
			}
			catch
			{
				return null;
			}
			return target;
		}

		private CacheConfig ReadConfigValue()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == File.Exists(pathConfig.ToString()))
				return null;
			FileInfo fileConfig = new FileInfo(pathConfig.ToString());
			if (fileConfig.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.佣兵配置])
				return null;
			m_fileLastEdit[(int)FILE_TYPE.佣兵配置] = fileConfig.LastWriteTime;

			CacheConfig resultConfig = new CacheConfig();
			resultConfig.awakeTime = MyConfig.ReadIniValue<DateTime>("配置", "唤醒时间", new DateTime(2000, 1, 1), pathConfig.ToString());
			resultConfig.awakePeriod = 60 * MyConfig.ReadIniValue<int>("配置", "唤醒时间间隔", 22, pathConfig.ToString());
			resultConfig.mode = MyConfig.ReadIniValue<string>("配置", "插件运行模式", "", pathConfig.ToString());
			resultConfig.teamName = MyConfig.ReadIniValue<string>("配置", "使用的队伍名称", "", pathConfig.ToString());
			resultConfig.strategyName = MyConfig.ReadIniValue<string>("配置", "战斗策略", "", pathConfig.ToString());
			resultConfig.mercPluginEnable = MyConfig.ReadIniValue<bool>("配置", "插件开关", false, pathConfig.ToString());
			resultConfig.scale = MyConfig.ReadIniValue<bool>("配置", "自动齿轮加速", false, pathConfig.ToString());
			resultConfig.map = MyConfig.ReadIniValue<string>("配置", "要刷的地图", "2-5", pathConfig.ToString());
			resultConfig.numCore = MyConfig.ReadIniValue<int>("配置", "队伍核心人数", 0, pathConfig.ToString());
			resultConfig.numTotal = MyConfig.ReadIniValue<int>("配置", "总队伍人数", 6, pathConfig.ToString());

			// 			Out.Log($"[{ID}]更新缓存 awakeTime:{resultConfig.awakeTime} awakePeriod:{resultConfig.awakePeriod} " +
			// 				$"mode:{resultConfig.mode} teamName:{resultConfig.teamName} strategyName:{resultConfig.strategyName} " +
			// 				$"mercPluginEnable:{resultConfig.mercPluginEnable} scale:{resultConfig.scale} map:{resultConfig.map} " +
			// 				$"numCore:{resultConfig.numCore} numTotal:{resultConfig.numTotal}"
			// 				);
			return resultConfig;
		}

		private void WriteConfigValue(bool Enable, TaskUnit task)
		{
			WriteConfigHSMod(Enable, task);
			WriteConfigValueMercPlugin(Enable, task);
			Out.Log($"[{ID}]写入配置 mode:{task?.Mode} teamName:{task?.TeamName} strategyName:{task?.StrategyName} " +
				$"Enable:{Enable} Scale:{task?.Scale} Map:{task?.Map} " +
				$"MercTeamNumTotal:{task?.MercTeamNumTotal} MercTeamNumCore:{task?.MercTeamNumCore}"
				);
		}

		private void WriteConfigHSMod(bool Enable, TaskUnit task)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;

			if(Enable == false)
				MyConfig.WriteIniValue("全局", "变速齿轮状态", task.Scale.ToString(), pathConfig.ToString());
			else
				MyConfig.WriteIniValue("全局", "变速齿轮状态", false.ToString(), pathConfig.ToString());
			MyConfig.WriteIniValue("全局", "变速倍率", "8", pathConfig.ToString());
		}

		private void WriteConfigValueMercPlugin(bool Enable, TaskUnit task)
		{
			DirectoryInfo pathConfig = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/io.github.jimowushuang.hs.cfg");
			if (false == System.IO.File.Exists(pathConfig.ToString()))
				return;

			MyConfig.WriteIniValue("配置", "插件开关", Enable.ToString(), pathConfig.ToString());
			if (Enable == true)
			{
				MyConfig.WriteIniValue("配置", "插件运行模式", task.Mode.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "使用的队伍名称", task.TeamName.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "战斗策略", task.StrategyName.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "自动齿轮加速", task.Scale.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "要刷的地图", task.Map.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "总队伍人数", task.MercTeamNumTotal.ToString(), pathConfig.ToString());
				MyConfig.WriteIniValue("配置", "队伍核心人数", task.MercTeamNumCore.ToString(), pathConfig.ToString());
			}
		}

		public async Task Delay(int milliseconds)
		{
			await Task.Delay(milliseconds);
		}

		public string GetHSLogPath()
		{
			DirectoryInfo pathConfig = new DirectoryInfo(Path.GetDirectoryName(m_hsPath) + "/BepInEx/config/" + ID + "/HsMod.cfg");
			if (false == File.Exists(pathConfig.ToString()))
				return "";

			return MyConfig.ReadIniValue<string>("开发", "炉石日志", "", pathConfig.ToString());

		}

		public void ReadMercLog()
		{
			try
			{
				//佣兵日志获取经验
				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepinEx/Log/" + ID);
				if (false == System.IO.Directory.Exists(rootHS.ToString()))
					return;
				List<FileInfo> testList = rootHS.GetFiles("mercenarylog@*.log", SearchOption.TopDirectoryOnly).ToList();
				FileInfo targetFile = testList.OrderByDescending(x => x.LastWriteTime.Ticks).FirstOrDefault();
				if (targetFile == null)
					return;
				if (targetFile.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.佣兵日志])
					return;
				m_fileLastEdit[(int)FILE_TYPE.佣兵日志] = targetFile.LastWriteTime;
				foreach (string line in File.ReadLines(targetFile.FullName).Reverse<string>())
				{
					if (line.IndexOf("战令信息") > 0)
					{
						Regex regex = new Regex(@"^.*等级:([\d]*).*经验:([\d]*).*$");
						Match match = regex.Match(line);
						if (match.Groups.Count == 3)
						{
							RewardXP rewardXP = new RewardXP()
							{
								Level = Convert.ToInt32(match.Groups[1].Value),
								ProgressXP = Convert.ToInt32(match.Groups[2].Value),
							};
							m_rewardXP = rewardXP;
						}
						break;
					}
				}
			}
			catch
			{
			}
		}

		public void ReadMercRecordLog()
		{
			try
			{
				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hsPath) + "/BepinEx/Log/" + ID);
				if (false == System.IO.Directory.Exists(rootHS.ToString()))
					return;
				List<FileInfo> testList = rootHS.GetFiles("gamerecord@*.log", SearchOption.TopDirectoryOnly).ToList();
				FileInfo targetFile = testList.OrderByDescending(x => x.LastWriteTime.Ticks).FirstOrDefault();
				if (targetFile == null)
					return;
				if (targetFile.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.佣兵对局日志])
					return;
				m_fileLastEdit[(int)FILE_TYPE.佣兵对局日志] = targetFile.LastWriteTime;
				string[] lines = File.ReadAllLines(targetFile.FullName);
				if (lines.Length == 0)
					return;

				string[] lineSplit = lines.Last().Split('\t');
				m_pvpRate = int.Parse(lineSplit[2]);
			}
			catch
			{
			}
		}

		public void ReadHBLog()
		{
			try
			{
				if (string.IsNullOrEmpty(m_hbPath))
					return;
				DirectoryInfo rootHS = new DirectoryInfo(System.IO.Path.GetDirectoryName(m_hbPath) + "/Logs");
				if (false == System.IO.Directory.Exists(rootHS.ToString()))
					return;
				List<FileInfo> testList = rootHS.GetFiles("Hearthbuddy*.txt", SearchOption.TopDirectoryOnly).ToList();
				FileInfo targetFile = testList.OrderByDescending(x => x.LastWriteTime.Ticks).FirstOrDefault();
				if (targetFile == null)
					return;
				if (targetFile.LastWriteTime <= m_fileLastEdit[(int)FILE_TYPE.兄弟日志])
					return;
				m_fileLastEdit[(int)FILE_TYPE.兄弟日志] = targetFile.LastWriteTime;
				System.Text.Encoding GB2312 = System.Text.Encoding.GetEncoding("GB2312");
				foreach (string line in File.ReadLines(targetFile.FullName, System.Text.Encoding.GetEncoding("GB2312")).Reverse<string>())
				{
					if (line.IndexOf("[监控插件] 合计: 战令") > 0)
					{
						Regex regex = new Regex(@"^.*合计: 战令([\d]*)级\(([\d]*)/[\d]*\)\([\d]*/小时\)\s(.*)\s[\d]*/[\d]*.*$");
						Match match = regex.Match(line);
						if (match.Groups.Count == 4)
						{
							RewardXP rewardXP = new RewardXP()
							{
								Level = Convert.ToInt32(match.Groups[1].Value),
								ProgressXP = Convert.ToInt32(match.Groups[2].Value),
							};
							m_rewardXP = rewardXP;
						}
						m_classicRate = match.Groups[3].Value;
						return;
					}
				}
			}
			catch
			{
			}
		}

		public bool NeedUpdateHS()
		{
			string logFilePath = Path.Combine(m_hsLogFileDir, "Hearthstone.log");

			// 检查文件是否存在
			if (!File.Exists(logFilePath))
				return false;

			// 打开文件，允许其他进程同时访问（如日志进程）
			using (FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader sr = new StreamReader(fs))
			{
				string line;

				// 使用栈来倒序处理文件中的行
				var lines = new Stack<string>();
				while ((line = sr.ReadLine()) != null)
				{
					lines.Push(line);
				}

				// 反向检查
				while (lines.Count > 0)
				{
					line = lines.Pop();

					// 检查是否包含指定的更新提示信息
					if (line.Contains("《炉石传说》已更新，请下载最新版本。"))
					{
						return true;
					}
				}
			}

			return false;
		}
		public bool HSSuccessLogin()
		{
			string logFilePath = Path.Combine(m_hsLogFileDir, "Hearthstone.log");

			// 检查文件是否存在
			if (!File.Exists(logFilePath))
				return false;

			// 打开文件，允许其他进程同时访问（如写入日志的进程）
			using (FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader sr = new StreamReader(fs))
			{
				string line;

				// 倒序读取文件内容：由于 StreamReader 不能倒序读取，我们可以采用一个反向扫描的方法。
				var lines = new Stack<string>();
				while ((line = sr.ReadLine()) != null)
				{
					lines.Push(line);
				}

				// 反向检查
				while (lines.Count > 0)
				{
					line = lines.Pop();
					if (line.Contains("[Startup] Startup stage LaunchGame"))
					{
						return true;
					}
				}
			}

			return false;
		}
		public int GetQueueSec()
		{
			string logFilePath = Path.Combine(m_hsLogFileDir, "Hearthstone.log");

			// 检查文件是否存在
			if (!File.Exists(logFilePath))
				return 0;

			// 打开文件，允许其他进程同时访问（如日志进程）
			using (FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader sr = new StreamReader(fs))
			{
				string line;

				// 使用栈来倒序处理文件中的行
				var lines = new Stack<string>();
				while ((line = sr.ReadLine()) != null)
				{
					lines.Push(line);
				}

				// 反向检查
				while (lines.Count > 0)
				{
					line = lines.Pop();

					// 判断是否包含指定的更新提示信息
					if (line.Contains("排队中，预计"))
					{
						// 正则表达式匹配 "预计" 后面的数字
						string pattern = @"预计(\d+)秒";

						// 使用正则表达式提取数字
						Match match = Regex.Match(line, pattern);
						if (match.Success)
						{
							return int.Parse(match.Groups[1].Value);
						}
						else
						{
							return -1;  // 如果正则匹配失败，返回 -1 表示错误
						}
					}
				}
			}
			return -1;
		}


		public void UpdateStatsMonth()
		{
			if (m_statsMonth == DateTime.Now.Month)
				return;

			DateTime check_point = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 1, 0, 0);
			if (DateTime.Now > check_point)
			{
				m_statsMonth = DateTime.Now.Month;
				m_pvpRate = 0;
				m_classicRate = "";
				m_fileLastEdit[(int)FILE_TYPE.佣兵对局日志] = DateTime.Now;
				m_fileLastEdit[(int)FILE_TYPE.兄弟日志] = DateTime.Now;
			}
		}

		private RewardXP m_rewardXP = new RewardXP();
		private int m_pvpRate = 0;
		private string m_classicRate = "";
		private string m_hbPath = "";//hb路径
		private string m_hsPath = "";
		private string m_token = "";//token
		private int m_pid = 0;//进程id
		private string m_hsLogFileDir = "";//炉石进程对应的日志
		private string m_ID = "";//自定id
		private bool m_enable = false;//启用状态
		private int m_hsmodPort = 58744;//hsmod端口
		private TaskManager m_taskManager;

		private DateTime[] m_fileLastEdit = new DateTime[(int)FILE_TYPE.Total]{
			DateTime.Now,
			DateTime.Now,
			DateTime.Now,
			DateTime.Now,
			DateTime.Now,
		};

		private CacheConfig m_cacheConfig = new CacheConfig();
		private int m_statsMonth = -1;
	}
}