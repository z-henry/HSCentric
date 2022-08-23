using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace HSCentric
{
	public class HSUnitSection : ConfigurationSection
	{

		// Declare the UrlsCollection collection property.
		[ConfigurationProperty("HSUnit", IsRequired = true)]
		public HSUnitCollection HSUnit
		{
			get { return (HSUnitCollection)base["HSUnit"]; }
			set { base["HSUnit"] = value; }
		}
	}
	public class HSUnitCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new HSUnitElement();
		}

		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((HSUnitElement)element).ID;
		}
		public void Add(HSUnitElement setting)
		{
			this.BaseAdd(setting);
		}

		public void Clear()
		{
			base.BaseClear();
		}

		public void Remove(string name)
		{
			base.BaseRemove(name);
		}
	}
	public class HSUnitElement : System.Configuration.ConfigurationElement
	{

		[ConfigurationProperty("id", IsRequired = true)]
		public int ID
		{
			get { return (int)this["id"]; }
			set { this["id"] = value; }
		}
		[ConfigurationProperty("path", IsRequired = true)]
		public string Path
		{
			get { return (string)this["path"]; }
			set { this["path"] = value; }
		}
		[ConfigurationProperty("enable", IsRequired = true)]
		public bool Enable
		{
			get { return (bool)this["enable"]; }
			set { this["enable"] = value; }
		}
		[ConfigurationProperty("tasks", IsRequired = true)]
		public TaskCollection Tasks
		{
			get { return (TaskCollection)base["tasks"]; }
			set { base["tasks"] = value; }
		}
		[ConfigurationProperty("level", IsRequired = true)]
		public int Level
		{
			get { return (int)this["level"]; }
			set { this["level"] = value; }
		}
		[ConfigurationProperty("xp", IsRequired = true)]
		public int XP
		{
			get { return (int)this["xp"]; }
			set { this["xp"] = value; }
		}
	}
	public class TaskCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new TaskElement();
		}

		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((TaskElement)element).ID;
		}
		public void Add(TaskElement setting)
		{
			this.BaseAdd(setting);
		}

		public void Clear()
		{
			base.BaseClear();
		}

		public void Remove(string name)
		{
			base.BaseRemove(name);
		}
	}
	public class TaskElement : System.Configuration.ConfigurationElement
	{
		[ConfigurationProperty("id", IsRequired = true)]
		public int ID
		{
			get { return (int)this["id"]; }
			set { this["id"] = value; }
		}
		[ConfigurationProperty("mode", IsRequired = true)]
		public string Mode
		{
			get { return (string)this["mode"]; }
			set { this["mode"] = value; }
		}
		[ConfigurationProperty("teamname", IsRequired = true)]
		public string TeamName
		{
			get { return (string)this["teamname"]; }
			set { this["teamname"] = value; }
		}
		[ConfigurationProperty("strategyname", IsRequired = true)]
		public string StrategyName
		{
			get { return (string)this["strategyname"]; }
			set { this["strategyname"] = value; }
		}
		[ConfigurationProperty("starttime", IsRequired = true)]
		public string StartTime
		{
			get { return (string)this["starttime"]; }
			set { this["starttime"] = value; }
		}
		[ConfigurationProperty("stoptime", IsRequired = true)]
		public string StopTime
		{
			get { return (string)this["stoptime"]; }
			set { this["stoptime"] = value; }
		}
	}
}