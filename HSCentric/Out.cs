using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository;
using System;

namespace HSCentric
{
	public static class Out
	{
		private static readonly ILog Logger;

		public static readonly string LogBaseName = "Logs/hscentric@";

		// 定义日志事件
		public static event Action<string, string> Logged;

		// 静态构造函数，初始化日志配置
		static Out()
		{
			ConfigureLog4Net(); // 动态配置日志
			Logger = LogManager.GetLogger(typeof(Out));
		}

		private static void ConfigureLog4Net()
		{
			// 定义日志格式
			var layout = new PatternLayout
			{
				ConversionPattern = "%date [%thread] %-5level - %message%newline" // 日志格式
			};
			layout.ActivateOptions();

			// 配置 RollingFileAppender
			var appender = new RollingFileAppender
			{
				AppendToFile = true, // 是否追加到现有日志文件
				File = LogBaseName, // 基础日志文件路径
				RollingStyle = RollingFileAppender.RollingMode.Date, // 按日期滚动
				StaticLogFileName = false, // 生成动态文件名
				DatePattern = "yyyy-MM-dd'.log'", // 日志文件名日期格式
				Layout = layout, // 设置日志格式
			};
			appender.ActivateOptions();

			// 获取日志仓库
			ILoggerRepository repository = LogManager.GetRepository();

			// 配置全局日志
			log4net.Config.BasicConfigurator.Configure(repository, appender);
		}

		public static void Info(string message)
		{
			Logger.Info(message);
			Logged?.Invoke("Info", message);
		}

		public static void Debug(string message)
		{
			Logger.Debug(message);
			Logged?.Invoke("Debug", message);
		}

		public static void Error(string message)
		{
			Logger.Error(message);
			Logged?.Invoke("Error", message);
		}
	}
}
