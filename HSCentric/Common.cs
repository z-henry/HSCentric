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
			return mode == TASK_MODE.狂野 || mode == TASK_MODE.标准 || mode == TASK_MODE.经典 || mode == TASK_MODE.休闲;
		}
		public static void Delay(int mm)
		{
			DateTime current = DateTime.Now;
			while (current.AddMilliseconds(mm) > DateTime.Now)
			{
				System.Windows.Forms.Application.DoEvents();
			}
		}

		[DllImport("kernel32")]
		private static extern long WritePrivateProfileString(string section, string key,string val, string filePath);

		[DllImport("kernel32")]
		private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

		public static void IniWriteValue(string Section, string Key, string Value, string path)
		{
			WritePrivateProfileString(Section, Key, Value, path);
		}

		public static T IniReadValue<T>(string Section, string Key, T Default, string path)
		{
			StringBuilder temp = new StringBuilder(255);
			GetPrivateProfileString(Section, Key, "", temp, 255, path);
			if (temp.Length == 0)
				return Default;
			else
				return (T)Convert.ChangeType(temp.ToString(), typeof(T));
		}
	}
}
