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
		public static extern bool WritePrivateProfileString(byte[] section, byte[] key, byte[] val, string filePath);
		[DllImport("kernel32")]
		public static extern int GetPrivateProfileString(byte[] section, byte[] key, byte[] def, byte[] retVal, int size, string filePath);

		private static byte[] getBytes(string s, string encodingName)
		{
			return null == s ? null : Encoding.GetEncoding(encodingName).GetBytes(s);
		}
		public static void IniWriteValue(string Section, string Key, string Value, string path, string encodingName = "utf-8")
		{
			WritePrivateProfileString(getBytes(Section, encodingName), getBytes(Key, encodingName), getBytes(Value, encodingName), path);
		}

		public static T IniReadValue<T>(string Section, string Key, T Default, string path, string encodingName = "utf-8")
		{
			byte[] buffer = new byte[8192];
			int count = GetPrivateProfileString(getBytes(Section, encodingName), getBytes(Key, encodingName), getBytes("", encodingName), buffer, 8192, path);
			string result = Encoding.GetEncoding(encodingName).GetString(buffer, 0, count).Trim();
			if (result.Length == 0)
				return Default;
			else
				return (T)Convert.ChangeType(result.ToString(), typeof(T));
		}
	}
}
