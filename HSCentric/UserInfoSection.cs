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
		public string ID
		{
			get { return (string)this["id"]; }
			set { this["id"] = value; }
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
		[ConfigurationProperty("tasks_spec", IsRequired = false)]
		public TaskCollection TasksSpec
		{
			get { return (TaskCollection)base["tasks_spec"]; }
			set { base["tasks_spec"] = value; }
		}

		[ConfigurationProperty("level", IsRequired = false, DefaultValue = 2)]
		public int Level
		{
			get { return (int)this["level"]; }
			set { this["level"] = value; }
		}
		[ConfigurationProperty("xp", IsRequired = false, DefaultValue = 0)]
		public int XP
		{
			get { return (int)this["xp"]; }
			set { this["xp"] = value; }
		}
		[ConfigurationProperty("hbpath", IsRequired = false, DefaultValue = "")]
		public string HBPath
		{
			get { return (string)this["hbpath"]; }
			set { this["hbpath"] = value; }
		}
		[ConfigurationProperty("hspath", IsRequired = false, DefaultValue = "")]
		public string HSPath
		{
			get { return (string)this["hspath"]; }
			set { this["hspath"] = value; }
		}

		[ConfigurationProperty("token", IsRequired = true)]
		public string Token
		{
			get { return (string)this["token"]; }
			set { this["token"] = value; }
		}
		[ConfigurationProperty("pvp_rate", IsRequired = false, DefaultValue = 0)]
		public int PvpRate
		{
			get { return (int)this["pvp_rate"]; }
			set { this["pvp_rate"] = value; }
		}
		[ConfigurationProperty("classic_rate", IsRequired = false, DefaultValue = "")]
		public string ClassicRate
		{
			get { return (string)this["classic_rate"]; }
			set { this["classic_rate"] = value; }
		}
		[ConfigurationProperty("hsmod_port", IsRequired = false, DefaultValue = 58744)]
		public int HSModPort
		{
			get { return (int)this["hsmod_port"]; }
			set { this["hsmod_port"] = value; }
		}
		[ConfigurationProperty("switch_task", IsRequired = false, DefaultValue = false)]
		public bool SwitchTask
		{
			get { return (bool)this["switch_task"]; }
			set { this["switch_task"] = value; }
		}
		[ConfigurationProperty("stats_month", IsRequired = false, DefaultValue = -1)]
		public int StatsMonth
		{
			get { return (int)this["stats_month"]; }
			set { this["stats_month"] = value; }
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
		[ConfigurationProperty("scale", IsRequired = false, DefaultValue = false)]
		public bool Scale
		{
			get { return (bool)this["scale"]; }
			set { this["scale"] = value; }
		}
		[ConfigurationProperty("map", IsRequired = false, DefaultValue = "2-5")]
		public string Map
		{
			get { return (string)this["map"]; }
			set { this["map"] = value; }
		}
		[ConfigurationProperty("numtotal", IsRequired = false, DefaultValue = 6)]
		public int NumTotal
		{
			get { return (int)this["numtotal"]; }
			set { this["numtotal"] = value; }
		}
		[ConfigurationProperty("numcore", IsRequired = false, DefaultValue = 0)]
		public int NumCore
		{
			get { return (int)this["numcore"]; }
			set { this["numcore"] = value; }
		}

	}
}