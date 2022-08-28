using HSCentric.Const;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace HSCentric
{
	public static class HSUnitManager
	{
		public static void Init()
		{
			LoadConfig();
		}
		public static void Release()
		{
			SaveConfig();
		}
		public static List<HSUnit> GetHSUnits()
		{
			List<HSUnit> result = new List<HSUnit>();
			lock (m_lockHS)
			{
				foreach (var iter in m_listHS)
				{
					result.Add((HSUnit)iter.DeepClone());
				}
			}
			return result;
		}
		public static void Check()
		{
			TimeSpan timespan_farmendding = new TimeSpan(0, 2, 0);//收菜模式结束一定时间内启动收尾
			lock (m_lockHS)
			{
				for (int i = 0, ii = m_listHS.Count; i < ii; ++i)
				{
					HSUnit hsUnit = m_listHS[i]; 
					var basicConfigValue = hsUnit.BasicConfigValue;
					var currentTask = hsUnit.CurrentTask;

					// 没启用就跳过
					if (!hsUnit.Enable)
						continue;

					// 不在启用时间段,启动了就干掉
					if (!hsUnit.IsActive())
					{
						if (hsUnit.IsProcessAlive())
						{
							Out.Log(string.Format("[{0}]未到启用时间", hsUnit.ID));
							hsUnit.KillHS();
						}
						continue;
					}

					// 在运行就判断是否需要杀掉
					if (hsUnit.IsProcessAlive())
					{
						string msg_kill_reason = "";
						// 需要切换模式
						if (hsUnit.NeedAdjustMode())
							msg_kill_reason = "切换模式";
						// 不更新日志就滚蛋
						else if (!hsUnit.IsLogUpdated())
							msg_kill_reason = "日志不更新";

						if (msg_kill_reason.Length > 0)
							hsUnit.KillHS(msg_kill_reason);
					}
					//炉石没运行就判断是否需要启动
					else
					{
						string msg_start_reason = "正常拽起";
						if (TASK_MODE.挂机收菜 == currentTask.Mode)
						{
							// 挂机收菜模式下，
							// 1. 到唤醒时间唤醒
							if (DateTime.Now >= basicConfigValue.AwakeTime)
								msg_start_reason = string.Format("收菜唤醒时间到了", timespan_farmendding.TotalSeconds);
							// 2. 没到唤醒时间，但是距离结束不到X分钟了，唤醒
							else if ((currentTask.StopTime.TimeOfDay - DateTime.Now.TimeOfDay).TotalSeconds < timespan_farmendding.TotalSeconds)
								msg_start_reason = string.Format("收菜模式{0}秒内结束", timespan_farmendding.TotalSeconds);
							// 3. 其他情况就不拽起了
							else
								msg_start_reason = "";
						}
						if (msg_start_reason.Length > 0)
						{
							if (hsUnit.NeedAdjustMode())
								hsUnit.AdjustMode();
							hsUnit.InitHsMod();
							hsUnit.StartHS(msg_start_reason); 
							if (Common.IsBuddyMode(currentTask.Mode))
								hsUnit.StartHB(msg_start_reason);
						}
					}
				}
			}
		}
		public static void CheckLog()
		{
			lock (m_lockHS)
			{
				for (int i = 0, ii = m_listHS.Count; i < ii; ++i)
				{
					HSUnit hsUnit = m_listHS[i];
					hsUnit.ReadLog();
				}
				SaveConfig();
				BackUpConfig();
			}
		}

		internal static void FlipEnable(int index)
		{
			lock (m_lockHS)
			{
				m_listHS[index].Enable = !m_listHS[index].Enable;
			}
		}
		internal static void SetEnable(string memberName, bool flag)
		{
			lock (m_lockHS)
			{
				foreach(var iter in m_listHS)
				{
					if (iter.ID == memberName)
						iter.Enable = flag;
				}
			}
		}

		internal static void Remove(int index)
		{
			lock (m_lockHS)
			{
				m_listHS.RemoveAt(index);
			}
		}
		public static void Add(HSUnit unit)
		{
			lock (m_lockHS)
			{
				m_listHS.Add(unit);
			}

		}
		static void LoadConfig()
		{
			HSUnitSection config = ConfigurationManager.GetSection("userinfo") as HSUnitSection;
			foreach (HSUnitElement hs in config.HSUnit.Cast<HSUnitElement>().ToList())
			{
				List<TaskUnit> tasks = new List<TaskUnit>();
				foreach (TaskElement task in hs.Tasks.Cast<TaskElement>().ToList())
				{
					tasks.Add(new TaskUnit()
					{
						Mode = (TASK_MODE)Enum.Parse(typeof(TASK_MODE), task.Mode),
						StartTime = Convert.ToDateTime(task.StartTime),
						StopTime = Convert.ToDateTime(task.StopTime),
						StrategyName = task.StrategyName,
						TeamName = task.TeamName,
					});
				}
				m_listHS.Add(new HSUnit()
				{
					ID = hs.ID,
					Enable = hs.Enable,
					Tasks = new TaskManager(tasks),
					XP = new RewardXP () { Level = hs.Level, ProgressXP = hs.XP },
					Token = hs.Token,
					HBPath = hs.HBPath,
				});
			}
			m_hsPath = ConfigurationManager.AppSettings["hs_path"];
		}
		static void SaveConfig()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			HSUnitSection section = config.GetSection("userinfo") as HSUnitSection;
			section.HSUnit.Clear();
			foreach (HSUnit hs in m_listHS)
			{
				int index_task = 0;
				TaskCollection tasks = new TaskCollection();
				foreach (TaskUnit task in hs.Tasks.GetTasks())
				{
					tasks.Add(new TaskElement()
					{
						ID = ++index_task,
						Mode = task.Mode.ToString(),
						TeamName = task.TeamName,
						StrategyName = task.StrategyName,
						StartTime = task.StartTime.ToString("G"),
						StopTime = task.StopTime.ToString("G"),
					});
				}
				section.HSUnit.Add(new HSUnitElement()
				{
					ID = hs.ID,
					Enable = hs.Enable,
					Tasks = tasks,
					Level = hs.XP.Level,
					XP = hs.XP.ProgressXP,
					HBPath = hs.HBPath,
					Token = hs.Token,
				});
			}
			config.AppSettings.Settings["hs_path"].Value = m_hsPath;
			config.Save();
// 			ConfigurationManager.RefreshSection("userinfo");
		}

		static List<HSUnit> m_listHS = new List<HSUnit>();
		static object m_lockHS = new object();
		static public string m_hsPath = "";

		internal static void Modify(int index, HSUnit unit)
		{
			lock (m_lockHS)
			{
				m_listHS[index] = unit;
			}
		}
		internal static HSUnit Get(int index)
		{
			lock (m_lockHS)
			{
				return (HSUnit)m_listHS[index].DeepClone();
			}
		}

		internal static void StartOnce(int index)
		{
			lock (m_lockHS)
			{
				if (!m_listHS[index].IsProcessAlive())
				{
					m_listHS[index].InitHsMod();
					m_listHS[index].StartHS("手动启动");
				}
			}
		}
		internal static void BackUpConfig()
		{
			//运行备份更新bat
			Process proc;
			proc = new Process();
			proc.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + @"script\backup\backup.bat";
			proc.StartInfo.Arguments = "\"" + System.IO.Path.GetDirectoryName(m_hsPath) + "\"";
			proc.StartInfo.CreateNoWindow = true;   //不创建该进程的窗口
			proc.StartInfo.UseShellExecute = false;   //不使用shell壳运行
			proc.Start();
			proc.WaitForExit();
		}
	}

}
