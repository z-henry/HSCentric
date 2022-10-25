using HSCentric.Const;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace HSCentric
{
	[Serializable]
	public class TaskManager
	{
		public TaskManager(HSUnit hsUnit, List<TaskUnit> tasks, TaskUnit taskspec, bool swtichTask)
		{
			m_hsunit = hsUnit;
			m_specTask = taskspec;
			m_switchTask = swtichTask;
			if (tasks != null)
				tasks.ForEach(i => Add(i));
		}
		public List<TaskUnit> GetTasks()
		{
			return m_tasks;
		}

		public TaskUnit GetTask(int index)
		{
			if (m_tasks.Count <= index)
				return null;

			return m_tasks[index];
		}
		public TaskUnit GetTaskSpec()
		{
			return m_specTask;
		}

		public bool Remove(int index)
		{
			if (m_tasks.Count <= index)
				return false;

			m_tasks.RemoveAt(index);
			Sort();
			return true;
		}
		public bool Add(TaskUnit task)
		{
			if (!IsTimeLegal(task))
				return false;

			m_tasks.Add(task);
			Sort();
			return true;
		}
		public bool RemoveSpec()
		{
			m_specTask = null;
			return true;
		}
		public bool AddSpec(TaskUnit task)
		{
			m_specTask = task;
			return true;
		}
		public bool SwitchTask
		{
			get { return m_switchTask; }
			set { m_switchTask = value; }
		}

		public bool Modify(int index, TaskUnit task)
		{
			if (!IsTimeLegal(task, index))
				return false;

			m_tasks[index] = task;
			Sort();
			return true;
		}
		public TaskUnit GetCurrentTask()
		{
			TaskUnit result = m_tasks[0];
			DateTime currentTime = DateTime.Now;
			foreach (TaskUnit task in m_tasks)
			{
				if (currentTime.TimeOfDay <= task.StopTime.TimeOfDay)
				{
					result = task;
					break;
				}
			}
			return SwitchTaskIfMeetConditions(result);
		}
		public object DeepClone()
		{
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, this); //复制到流中
			ms.Position = 0;
			return (bf.Deserialize(ms));
		}


		private void Sort()
		{
			m_tasks.Sort();
		}
		private bool IsTimeLegal(TaskUnit task, int exclude = -1)
		{
			for (int i = 0, ii = m_tasks.Count; i < ii; ++i)
			{
				if (i == exclude)
					continue;

				TaskUnit task_iter = m_tasks[i];
				if ((task_iter.StartTime.TimeOfDay >= task.StartTime.TimeOfDay && task_iter.StopTime.TimeOfDay <= task.StopTime.TimeOfDay) ||
					(task_iter.StopTime.TimeOfDay >= task.StartTime.TimeOfDay && task_iter.StartTime.TimeOfDay <= task.StopTime.TimeOfDay))
					return false;
			}
			return true;
		}
		private TaskUnit SwitchTaskIfMeetConditions(TaskUnit taskunit)
		{
			if (false == m_switchTask)
				return taskunit;
			TaskUnit taskunit_switch = (TaskUnit)m_specTask.DeepClone();
			taskunit_switch.StartTime = taskunit.StartTime;
			taskunit_switch.StopTime = taskunit.StopTime;
			if (m_hsunit.XP.Level >= 400)
				if (taskunit.Mode == TASK_MODE.PVP && m_hsunit.MercPvpRate >= 12000)
					return taskunit_switch;
				else if (Common.IsBuddyMode(taskunit.Mode) && m_hsunit.ClassicRate.IndexOf("传说")  != -1)
					return taskunit_switch;
				else if (taskunit.Mode == TASK_MODE.挂机收菜)
					return taskunit_switch;

			return taskunit;
		}

		private List<TaskUnit> m_tasks = new List<TaskUnit>();
		private HSUnit m_hsunit = null;
		private TaskUnit m_specTask = null;
		private bool m_switchTask = false;
	}

	[Serializable]
	public class TaskUnit:IComparable<TaskUnit>
	{
		public DateTime StartTime
		{
			get { return m_startTime; }
			set 
			{ 
				m_startTime = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
			}
		}
		public DateTime StopTime
		{
			get { return m_stopTime; }
			set
			{
				m_stopTime = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 59);
			}
		}
		public string TeamName
		{
			get { return m_teamName; }
			set { m_teamName = value; }
		}
		public TASK_MODE Mode
		{
			get { return m_mode; }
			set { m_mode = value; }
		}
		public string StrategyName
		{
			get { return m_strategyName; }
			set { m_strategyName = value; }
		}

		public bool Scale
		{
			set { m_scale = value; }
			get { return m_scale; }
		}
		public string Map
		{
			set { m_map = value; }
			get { return m_map; }
		}
		public int MercTeamNumCore
		{
			set { m_numCore = value; }
			get { return m_numCore; }
		}
		public int MercTeamNumTotal
		{
			set { m_numTotal = value; }
			get { return m_numTotal; }
		}

		public bool IsTimeLegal()
		{
			return m_startTime.TimeOfDay < m_stopTime.TimeOfDay;
		}
		public object DeepClone()
		{
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, this); //复制到流中
			ms.Position = 0;
			return (bf.Deserialize(ms));
		}

		public int CompareTo(TaskUnit other)
		{
			return this.m_stopTime.TimeOfDay.CompareTo(other.m_stopTime.TimeOfDay);
		}

		private DateTime m_startTime = DateTime.Now;
		private DateTime m_stopTime = DateTime.Now.AddMinutes(1);
		private TASK_MODE m_mode = TASK_MODE.一条龙;
		private string m_teamName ="初始队伍";
		private string m_strategyName = "PVE策略";
		private bool m_scale = false;
		private string m_map = "2-5";
		private int m_numCore = 0;
		private int m_numTotal = 6;
	}
}
