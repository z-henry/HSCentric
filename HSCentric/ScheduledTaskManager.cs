using System;
using System.Collections.Generic;
using System.Threading;

namespace HSCentric
{
	/// <summary>
	/// 单例模式管理定时调用回调函数的任务管理器。
	/// </summary>
	public sealed class ScheduledTaskManager
	{
		private static readonly Lazy<ScheduledTaskManager> _lazyInstance =
			new Lazy<ScheduledTaskManager>(() => new ScheduledTaskManager());

		/// <summary>
		/// 获取 ScheduledTaskManager 单例实例。
		/// </summary>
		public static ScheduledTaskManager Instance => _lazyInstance.Value;

		private readonly object _lock = new object();
		private readonly Dictionary<string, Timer> _timers = new Dictionary<string, Timer>();

		// 私有构造函数，防止外部实例化
		private ScheduledTaskManager() { }

		/// <summary>
		/// 添加或更新一个任务。
		/// </summary>
		/// <param name="id">任务标识。</param>
		/// <param name="time">任务触发时间点。</param>
		/// <param name="callback">触发时要调用的回调函数。</param>
		public void AddOrUpdateTask(string id, DateTime time, Action callback)
		{
			if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Task id cannot be null or whitespace.", nameof(id));
			if (callback == null) throw new ArgumentNullException(nameof(callback));

			TimeSpan dueTime = time - DateTime.Now;
			if (dueTime < TimeSpan.Zero)
			{
				dueTime = TimeSpan.Zero;
			}

			lock (_lock)
			{
				// 如果已存在同 id 的任务，则先移除
				if (_timers.TryGetValue(id, out var existing))
				{
					existing.Dispose();
					_timers.Remove(id);
				}

				// 创建新的定时器，只触发一次
				Timer timer = null;
				timer = new Timer(state =>
				{
					try
					{
						callback();
					}
					catch (Exception ex)
					{
						// 使用 Out.Error 记录异常
						Out.Error($"ScheduledTask '{id}' execution error: {ex}");
					}
					finally
					{
						// 执行完毕后自动移除定时器
						lock (_lock)
						{
							timer.Dispose();
							_timers.Remove(id);
						}
					}
				}, null, dueTime, Timeout.InfiniteTimeSpan);

				_timers[id] = timer;
			}
		}

		/// <summary>
		/// 删除指定 id 的任务。
		/// </summary>
		/// <param name="id">任务标识。</param>
		public void RemoveTask(string id)
		{
			if (string.IsNullOrWhiteSpace(id)) return;

			lock (_lock)
			{
				if (_timers.TryGetValue(id, out var timer))
				{
					timer.Dispose();
					_timers.Remove(id);
				}
			}
		}
	}
}
