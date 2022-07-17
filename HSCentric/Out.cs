using System;
using System.IO;
using System.Text;

namespace HSCentric
{
	public static class Out
	{
		public static void Log(string log)
		{
			string errorLogFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
			if (!Directory.Exists(errorLogFilePath))
			{
				Directory.CreateDirectory(errorLogFilePath);
			}
			string logFile = System.IO.Path.Combine(errorLogFilePath, "hscentriclog" + "@" + DateTime.Today.ToString("yyyy-MM-dd") + ".log");
			bool writeBaseInfo = System.IO.File.Exists(logFile);
			StreamWriter swLogFile = new StreamWriter(logFile, true, Encoding.Unicode);
			swLogFile.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "\t" + log);
			swLogFile.Close();
			swLogFile.Dispose();
		}
	}
}
