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
		public TaskManager (List<TaskUnit> tasks = null)
		{
			if (tasks != null)
				tasks.ForEach(i => m_tasks.Add(i));
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
		public bool Modify(int index, TaskUnit task)
		{
			if (!IsTimeLegal(task))
				return false;

			m_tasks[index] = task;
			Sort();
			return true;
		}
		public TaskUnit GetCurrentTask()
		{
			DateTime currentTime = DateTime.Now;
			foreach (TaskUnit task in m_tasks)
			{
				if (currentTime <= task.StopTime)
					return task;
			}
			return m_tasks[0];
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
		private bool IsTimeLegal(TaskUnit task)
		{
			foreach (TaskUnit task_iter in m_tasks)
			{
				if ((task_iter.StartTime.TimeOfDay >= task.StartTime.TimeOfDay && task_iter.StopTime.TimeOfDay <= task.StopTime.TimeOfDay) ||
					(task_iter.StopTime.TimeOfDay >= task.StartTime.TimeOfDay && task_iter.StartTime.TimeOfDay <= task.StopTime.TimeOfDay))
					return false;
			}
			return true;
		}
		private List<TaskUnit> m_tasks = new List<TaskUnit>();
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
		public string StragyName
		{
			get { return m_stragyName; }
			set { m_stragyName = value; }
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
		private DateTime m_stopTime = DateTime.Now;
		private TASK_MODE m_mode = TASK_MODE.挂机收菜;
		private string m_teamName ="";
		private string m_stragyName = "";
	}
}
