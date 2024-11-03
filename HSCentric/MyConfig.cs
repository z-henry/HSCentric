using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HSCentric
{

	internal class MyConfig
	{
		/// <summary>
		/// 读取指定 Section 和 Key 的值。
		/// </summary>
		public static T ReadIniValue<T>(string section, string key, T defaultValue, string filePath)
		{
			if (!File.Exists(filePath))
				return defaultValue;

			string currentSection = "";
			foreach (var line in File.ReadAllLines(filePath, new UTF8Encoding(false)))
			{
				string trimmedLine = line.Trim();
				// 跳过空行和注释
				if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
					continue;

				// 处理 Section
				if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
				{
					currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
					continue;
				}

				// 如果当前 Section 不匹配，跳过
				if (!currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
					continue;

				// 处理 Key-Value
				int idx = trimmedLine.IndexOf('=');
				if (idx > 0)
				{
					string currentKey = trimmedLine.Substring(0, idx).Trim();
					string value = trimmedLine.Substring(idx + 1).Trim();

					if (currentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
						return (T)Convert.ChangeType(value, typeof(T));
				}
			}

			return defaultValue;
		}

		/// <summary>
		/// 写入指定 Section 和 Key 的值。如果 Section 或 Key 不存在，则添加。
		/// </summary>
		public static void WriteIniValue(string section, string key, string value, string filePath)
		{
			List<string> lines = new List<string>();
			if (File.Exists(filePath))
				lines.AddRange(File.ReadAllLines(filePath, new UTF8Encoding(false)));

			int sectionIndex = -1;
			for (int i = 0; i < lines.Count; i++)
			{
				string trimmedLine = lines[i].Trim();
				if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
				{
					string currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
					if (currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
					{
						sectionIndex = i;
						break;
					}
				}
			}

			if (sectionIndex == -1)
			{
				// Section 不存在，添加 Section 和 Key-Value
				lines.Add("");
				lines.Add($"[{section}]");
				lines.Add($"{key} = {value}");
			}
			else
			{
				// 查找 Key 是否存在
				bool keyFound = false;
				for (int i = sectionIndex + 1; i < lines.Count; i++)
				{
					string line = lines[i].Trim();

					// 如果遇到新的 Section，停止查找
					if (line.StartsWith("[") && line.EndsWith("]"))
					{
						break;
					}

					// 跳过注释和空行
					if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
					{
						continue;
					}

					int idx = line.IndexOf('=');
					if (idx > 0)
					{
						string currentKey = line.Substring(0, idx).Trim();
						if (currentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
						{
							// 修改已有的 Key-Value
							lines[i] = $"{currentKey} = {value}";
							keyFound = true;
							break;
						}
					}
				}

				if (!keyFound)
				{
					// Key 不存在，添加到 Section 末尾
					int insertIndex = sectionIndex + 1;
					while (insertIndex < lines.Count && !lines[insertIndex].Trim().StartsWith("[") && !string.IsNullOrWhiteSpace(lines[insertIndex].Trim()))
					{
						insertIndex++;
					}
					lines.Insert(insertIndex, $"{key} = {value}");
				}
			}

			// 将修改后的内容写回文件，以 UTF-8 编码
			File.WriteAllLines(filePath, lines, new UTF8Encoding(false));
		}
	}
}
