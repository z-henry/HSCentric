using HSCentric.Const;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime;
using System.Threading.Tasks;

namespace HSCentric
{
	public class HSUnitManager
	{
		private static readonly HSUnitManager instance = new HSUnitManager();

		public static HSUnitManager Get()
		{
			return instance;
		}

		public void Init(Action callbackUpdateHS)
		{
			m_callbackUpdateHS = callbackUpdateHS;
			LoadConfig();
			foreach (var process in HSProcess())
			{
				try
				{
					process.Kill();
					Out.Info(string.Format("关闭炉石残留[pid:{0}]", process.Id));
				}
				catch (Exception ex)
				{
					Out.Error("关闭炉石残留失败: " + ex.Message);
				}
				Common.Delay(5000);
			}
		}

		public void Release()
		{
			SaveConfig();
		}

		public void InterruptBeforeUpdate()
		{
			if (false == m_waitForUpdateHS)
			{
				m_waitForUpdateHS = true;
				m_callbackUpdateHS();
			}	
		}
		public void RecoverAfterUpdated()
		{
			m_waitForUpdateHS = false;
		}

		public List<HSUnit> GetHSUnits()
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

		public void Check()
		{
			TimeSpan timespan_farmendding = new TimeSpan(0, 2, 0);//收菜模式结束一定时间内启动收尾
			lock (m_lockHS)
			{
				for (int i = 0, ii = m_listHS.Count; i < ii; ++i)
				{
					HSUnit hsUnit = m_listHS[i];
					var basicConfigValue = hsUnit.BasicConfigValue;
					var currentTask = hsUnit.CurrentTask;

					// 升级的时候关掉所有客户端
					if (m_waitForUpdateHS == true)
					{
						if (hsUnit.IsProcessAlive())
						{
							Out.Error($"需要升级");
							hsUnit.KillHS();
						}

						continue;
					}

					// 没启用就跳过
					if (!hsUnit.Enable)
					{
						// 需要关闭经验统计
						hsUnit.LastXPUpdateTime = DateTime.MaxValue;
						continue;
					}

					// 不在启用时间段,启动了就干掉
					if (!hsUnit.IsActive())
					{
						if (hsUnit.IsProcessAlive())
						{
							Out.Info($"[{hsUnit.ID}] 未到启用时间");
							hsUnit.KillHS();
							hsUnit.LastXPUpdateTime = DateTime.MaxValue;
						}
						continue;
					}

					// 在运行就判断是否需要杀掉
					if (hsUnit.IsProcessAlive())
					{
						// 需要切换模式
						if (hsUnit.NeedAdjustMode())
						{
							Out.Info($"[{hsUnit.ID}] 切换模式");
							hsUnit.KillHS();
						}
						// 不更新日志就滚蛋
						else if (!hsUnit.IsLogUpdated())
						{
							Out.Error($"[{hsUnit.ID}] 日志不更新");
							hsUnit.KillHS();
						}
					}
					//炉石没运行就判断是否需要启动
					else
					{
						string msg_start_reason = "正常拽起";
						if (TASK_MODE.挂机收菜 == currentTask.Mode)
						{
							// 挂机收菜模式下，
							// 1. 到唤醒时间唤醒
							if (DateTime.Now >= basicConfigValue.mercCacheConfig.awakeTime)
								msg_start_reason = string.Format("收菜唤醒时间到了", timespan_farmendding.TotalSeconds);
							// 2. 没到唤醒时间，但是距离结束不到X分钟了，唤醒
							else if ((currentTask.StopTime.TimeOfDay - DateTime.Now.TimeOfDay).TotalSeconds < timespan_farmendding.TotalSeconds)
								msg_start_reason = string.Format("收菜模式{0}秒内结束", timespan_farmendding.TotalSeconds);
							// 3. 其他情况就不拽起了
							else
								msg_start_reason = "";
						}
						if (msg_start_reason.Length > 0 &&
							HSProcess().Count < 999)
						{
							//if (hsUnit.NeedAdjustMode())
								hsUnit.AdjustMode();
							hsUnit.InitConfig();
							hsUnit.Start(msg_start_reason, Common.IsBuddyMode(currentTask.Mode));
							Common.Delay(5 * 1000);

						}
					}
				}
			}
		}

		public void CheckLog()
		{
			lock (m_lockHS)
			{
				for (int i = 0, ii = m_listHS.Count; i < ii; ++i)
				{
					HSUnit hsUnit = m_listHS[i];
					hsUnit.UpdateStatsMonth();

					// 没启用就跳过
					if (!hsUnit.Enable)
						continue;

					if (false == hsUnit.ReadHSLog())
					{
						Out.Error($"[{hsUnit.ID}] 炉石本体日志异常");
						hsUnit.KillHS();
					}

					if (Common.IsBGMode(hsUnit.CurrentTask.Mode))
						hsUnit.ReadBGLog();
					else if (Common.IsMercMode(hsUnit.CurrentTask.Mode))
					{
						hsUnit.ReadMercLog();
						hsUnit.ReadMercRecordLog();
					}
					else if (Common.IsBuddyMode(hsUnit.CurrentTask.Mode))
					{
						if (false == hsUnit.ReadHBLog())
						{
							Out.Error($"[{hsUnit.ID}] 炉石兄弟日志异常");
							hsUnit.KillHS();
						}
					}

				}
				SaveConfig();
				BackUpConfig();
			}
		}

		internal void FlipEnable(int index)
		{
			lock (m_lockHS)
			{
				m_listHS[index].Enable = !m_listHS[index].Enable;

				ScheduledTaskManager.Instance.RemoveTask($"{m_listHS[index].ID}_pause");
			}
		}

		internal void SetEnable(string memberName, bool flag)
		{
			lock (m_lockHS)
			{
				foreach (var iter in m_listHS)
				{
					if (iter.ID == memberName)
					{
						iter.Enable = flag;
						ScheduledTaskManager.Instance.RemoveTask($"{memberName}_pause");
					}
				}
			}
		}

		internal void SetPause(string memberName)
		{
			lock (m_lockHS)
			{
				for (int i = 0; i < m_listHS.Count; i++)
				{
					if (m_listHS[i].ID == memberName)
					{
						SetPause(i);
						break;
					}
				}
			}
		}

		internal void SetPause(int index)
		{
			lock (m_lockHS)
			{
				m_listHS[index].Enable = false;
				var memberId = m_listHS[index].ID;
				ScheduledTaskManager.Instance.AddOrUpdateTask(
					id: $"{m_listHS[index].ID}_pause",
					time: DateTime.Now.AddHours(8),
					callback: () =>
					{
						// 24 小时后再开
						SetEnable(memberId, true);
						Out.Info($"[{memberId}] 已恢复启用");
					}
				);
			}
		}

		internal void InitConfig(int index)
		{
			lock (m_lockHS)
			{
				m_listHS[index].InitConfig();
			}
		}

		internal void ReleaseConfig(int index)
		{
			lock (m_lockHS)
			{
				m_listHS[index].ReleaseConfig();
			}
		}

		internal void ResetXPRate(int index)
		{
			lock (m_lockHS)
			{
				m_listHS[index].TotalRunningTime = 0;
				m_listHS[index].TotalGaintXP = 0;
				m_listHS[index].TotalGaintXP_Achieve = 0;
				m_listHS[index].TotalGaintXP_Other = 0;
				m_listHS[index].TotalGaintXP_Quest = 0;
			}
		}

		internal void Remove(int index)
		{
			lock (m_lockHS)
			{
				m_listHS.RemoveAt(index);
			}
		}

		public void Add(HSUnit unit)
		{
			lock (m_lockHS)
			{
				m_listHS.Add(unit);
			}
		}

		private void LoadConfig()
		{
			HSUnitSection config = ConfigurationManager.GetSection("userinfo") as HSUnitSection;
			foreach (HSUnitElement hs in config.HSUnit.Cast<HSUnitElement>().ToList())
			{
				List<TaskUnit> tasks_common = new List<TaskUnit>();
				foreach (TaskElement task in hs.Tasks.Cast<TaskElement>().ToList())
				{
					tasks_common.Add(new TaskUnit()
					{
						Mode = (TASK_MODE)Enum.Parse(typeof(TASK_MODE), task.Mode),
						StartTime = Convert.ToDateTime(task.StartTime),
						StopTime = Convert.ToDateTime(task.StopTime),
						StrategyName = task.StrategyName,
						TeamName = task.TeamName,
						Scale = task.Scale,
						ClaimAchievement = task.ClaimAchievement,
						ClaimReward = task.ClaimReward,
						RefreshQuest = task.RefreshQuest,
						Map = task.Map,
						MercTeamNumCore = task.NumCore,
						MercTeamNumTotal = task.NumTotal,
					});
				}
				TaskUnit taskSpec = new TaskUnit();
				foreach (TaskElement task in hs.TasksSpec.Cast<TaskElement>().ToList())
				{
					taskSpec = new TaskUnit
					{
						Mode = (TASK_MODE)Enum.Parse(typeof(TASK_MODE), task.Mode),
						StartTime = Convert.ToDateTime(task.StartTime),
						StopTime = Convert.ToDateTime(task.StopTime),
						StrategyName = task.StrategyName,
						TeamName = task.TeamName,
						Scale = task.Scale,
						ClaimAchievement = task.ClaimAchievement,
						ClaimReward = task.ClaimReward,
						RefreshQuest = task.RefreshQuest,
						Map = task.Map,
						MercTeamNumCore = task.NumCore,
						MercTeamNumTotal = task.NumTotal,
					};
					//目前只有一次，保不齐会扩展
					break;
				}
				HSUnit hsunit = new HSUnit()
				{
					ID = hs.ID,
					Enable = hs.Enable,
					XP = new RewardXP { Level = hs.Level, ProgressXP = hs.XP },
					Token = hs.Token,
					HBPath = hs.HBPath,
					HSPath = hs.HSPath,
					MercPvpRate = hs.PvpRate,
					ClassicRate = hs.ClassicRate,
					HSModPort = hs.HSModPort,
					StatsMonth = hs.StatsMonth,
					TotalGaintXP = hs.TotalGaintXP,
					TotalGaintXP_Quest = hs.TotalGaintXP_Quest,
					TotalGaintXP_Achieve = hs.TotalGaintXP_Achieve,
					TotalGaintXP_Other = hs.TotalGaintXP_Other,
					TotalRunningTime = hs.TotalRunningTime,
				};
				hsunit.Tasks = new TaskManager(hsunit, tasks_common, taskSpec, hs.SwitchTask);//要传入对象，放到初始值设定里不会获取对象的this- -!!!
				m_listHS.Add(hsunit);
			}
		}

		private void SaveConfig()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			HSUnitSection section = config.GetSection("userinfo") as HSUnitSection;
			section.HSUnit.Clear();
			foreach (HSUnit hs in m_listHS)
			{
				int index_task = 0;
				TaskCollection tasks_common = new TaskCollection();
				foreach (TaskUnit task in hs.Tasks.GetTasks())
				{
					tasks_common.Add(new TaskElement()
					{
						ID = ++index_task,
						Mode = task.Mode.ToString(),
						TeamName = task.TeamName,
						StrategyName = task.StrategyName,
						StartTime = task.StartTime.ToString("G"),
						StopTime = task.StopTime.ToString("G"),
						Scale = task.Scale,
						ClaimAchievement = task.ClaimAchievement,
						ClaimReward = task.ClaimReward,
						RefreshQuest = task.RefreshQuest,
						Map = task.Map,
						NumTotal = task.MercTeamNumTotal,
						NumCore = task.MercTeamNumCore,
					});
				}
				TaskCollection tasks_spec = new TaskCollection();
				tasks_spec.Add(new TaskElement()
				{
					ID = ++index_task,
					Mode = hs.Tasks.GetTaskSpec().Mode.ToString(),
					TeamName = hs.Tasks.GetTaskSpec().TeamName,
					StrategyName = hs.Tasks.GetTaskSpec().StrategyName,
					StartTime = hs.Tasks.GetTaskSpec().StartTime.ToString("G"),
					StopTime = hs.Tasks.GetTaskSpec().StopTime.ToString("G"),
					Scale = hs.Tasks.GetTaskSpec().Scale,
					ClaimAchievement = hs.Tasks.GetTaskSpec().ClaimAchievement,
					ClaimReward = hs.Tasks.GetTaskSpec().ClaimReward,
					RefreshQuest = hs.Tasks.GetTaskSpec().RefreshQuest,
					Map = hs.Tasks.GetTaskSpec().Map,
					NumTotal = hs.Tasks.GetTaskSpec().MercTeamNumTotal,
					NumCore = hs.Tasks.GetTaskSpec().MercTeamNumCore
				});
				section.HSUnit.Add(new HSUnitElement()
				{
					ID = hs.ID,
					Enable = hs.Enable,
					Tasks = tasks_common,
					TasksSpec = tasks_spec,
					Level = hs.XP.Level,
					XP = hs.XP.ProgressXP,
					HBPath = hs.HBPath,
					HSPath = hs.HSPath,
					Token = hs.Token,
					ClassicRate = hs.ClassicRate,
					PvpRate = hs.MercPvpRate,
					HSModPort = hs.HSModPort,
					StatsMonth = hs.StatsMonth,
					SwitchTask = hs.Tasks.SwitchTask,
					TotalRunningTime = hs.TotalRunningTime,
					TotalGaintXP = hs.TotalGaintXP,
					TotalGaintXP_Achieve = hs.TotalGaintXP_Achieve,
					TotalGaintXP_Other = hs.TotalGaintXP_Other,
					TotalGaintXP_Quest = hs.TotalGaintXP_Quest
				});
			}
			config.Save();
			// 			ConfigurationManager.RefreshSection("userinfo");
		}

		internal void Modify(int index, HSUnit unit)
		{
			lock (m_lockHS)
			{
				m_listHS[index] = unit;
			}
		}

		internal string GetIDByIndex(int index)
		{
			lock (m_lockHS)
			{
				return m_listHS[index].ID;
			}
		}

		internal HSUnit GetUnitByIndex(int index)
		{
			lock (m_lockHS)
			{
				return (HSUnit)m_listHS[index].DeepClone();
			}
		}

		internal void StartOnce(int index)
		{
			lock (m_lockHS)
			{
				if (!m_listHS[index].IsProcessAlive())
				{
					// 					m_listHS[index].InitHsMod();
					m_listHS[index].StartHS("手动启动");
				}
			}
		}

		internal void BackUpConfig()
		{
			try
			{
				//运行备份更新bat
				Process proc;
				proc = new Process();
				proc.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + @"script\backup\backup.bat";
				proc.StartInfo.CreateNoWindow = true;   //不创建该进程的窗口
				proc.StartInfo.UseShellExecute = false;   //不使用shell壳运行
// 				proc.Start();
// 				proc.WaitForExit();
			}
			catch
			{ }
		}

		internal List<Process> HSProcess()
		{
			List<Process> listProcess = new List<Process>();
			foreach (var process_iter in Process.GetProcessesByName("Hearthstone"))
			{
				// 				Out.Log(string.Format("炉石残留[pid:{0}]", process_iter.Id));
				using (var searcher = new ManagementObjectSearcher(
					"SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process_iter.Id))
				using (var objects = searcher.Get())
				{
					var @object = objects.Cast<ManagementBaseObject>().SingleOrDefault();
					string commandline = @object?["CommandLine"]?.ToString();
					if (commandline != null &&
						commandline.IndexOf("--startmethod:hscentric") != -1)
					{
						listProcess.Add(process_iter);
					}
				}
			}
			return listProcess;
		}

		private List<HSUnit> m_listHS = new List<HSUnit>();
		private object m_lockHS = new object();
		private bool m_waitForUpdateHS = false;//等待升级
		private Action m_callbackUpdateHS;//升级完了回调函数

	}
}