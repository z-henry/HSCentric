using HSCentric.Const;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HSCentric
{
	public static class Common
	{
		public static bool IsBuddyMode(TASK_MODE mode)
		{
			return mode == TASK_MODE.狂野 || mode == TASK_MODE.标准 || mode == TASK_MODE.经典 || mode == TASK_MODE.休闲 || mode == TASK_MODE.幻变;
		}
		public static bool IsBGMode(TASK_MODE mode)
		{
			return mode == TASK_MODE.酒馆;
		}
		public static bool IsMercMode(TASK_MODE mode)
		{
			return mode == TASK_MODE.刷图 || mode == TASK_MODE.神秘人 || mode == TASK_MODE.佣兵任务 || 
				mode == TASK_MODE.一条龙 || mode == TASK_MODE.PVP || mode == TASK_MODE.挂机收菜 || 
				mode == TASK_MODE.PVP || mode == TASK_MODE.解锁装备 || mode == TASK_MODE.主线任务 || mode == TASK_MODE.活动任务;
		}
		public static void Delay(int mm)
		{
			DateTime current = DateTime.Now;
			while (current.AddMilliseconds(mm) > DateTime.Now)
			{
				System.Windows.Forms.Application.DoEvents();
			}
		}
	}
}
