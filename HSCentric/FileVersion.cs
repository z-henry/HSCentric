using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCentric
{
	public class FileVerison
	{
		public FileVerison(int _major, int _minor, int _build, int _private)
		{
			m_FileMajorPart = _major;
			m_FileMinorPart = _minor;
			m_FileBuildPart = _build;
			m_FilePrivatePart = _private;
		}
		public override string ToString()
		{
			return string.Format("{0}.{1}.{2}.{3}", m_FileMajorPart, m_FileMinorPart, m_FileBuildPart, m_FilePrivatePart);
		}
		public static bool operator <(FileVerison lhs, FileVerison rhs)
		{
			if (lhs.m_FileMajorPart < rhs.m_FileMajorPart)
				return true;
			else if (lhs.m_FileMajorPart == rhs.m_FileMajorPart)
			{
				if (lhs.m_FileMinorPart < rhs.m_FileMinorPart)
					return true;
				else if (lhs.m_FileMinorPart == rhs.m_FileMinorPart)
				{
					if (lhs.m_FileBuildPart < rhs.m_FileBuildPart)
						return true;
					else if (lhs.m_FileBuildPart == rhs.m_FileBuildPart)
					{
						if (lhs.m_FilePrivatePart < rhs.m_FilePrivatePart)
							return true;
					}
				}
			}
			return false;
		}
		public static bool operator >(FileVerison lhs, FileVerison rhs)
		{
			if (lhs.m_FileMajorPart > rhs.m_FileMajorPart)
				return true;
			else if (lhs.m_FileMajorPart == rhs.m_FileMajorPart)
			{
				if (lhs.m_FileMinorPart > rhs.m_FileMinorPart)
					return true;
				else if (lhs.m_FileMinorPart == rhs.m_FileMinorPart)
				{
					if (lhs.m_FileBuildPart > rhs.m_FileBuildPart)
						return true;
					else if (lhs.m_FileBuildPart == rhs.m_FileBuildPart)
					{
						if (lhs.m_FilePrivatePart > rhs.m_FilePrivatePart)
							return true;
					}
				}
			}
			return false;
		}
		private int m_FileMajorPart;
		private int m_FileMinorPart;
		private int m_FileBuildPart;
		private int m_FilePrivatePart;
	}
}
