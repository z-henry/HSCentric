using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace HSCentric
{
	// 	public class UserInfoSection : IConfigurationSectionHandler
	// 	{
	// 		public object Create(object parent, object configContext, XmlNode section)
	// 		{
	// 			List<HSUnit> myConfigObject = new List<HSUnit>();
	// 
	// 			foreach (XmlNode childNode in section.ChildNodes)
	// 			{
	// 				string path = "", starttime = "", stoptime = "";
	// 				foreach (XmlAttribute attrib in childNode.Attributes)
	// 				{
	// 					switch (attrib.Name)
	// 					{
	// 						case "path":
	// 							path = attrib.Value;
	// 							break;
	// 						case "starttime":
	// 							starttime = attrib.Value;
	// 							break;
	// 						case "stoptime":
	// 							stoptime = attrib.Value;
	// 							break;
	// 						default:
	// 							break;
	// 					}
	// 				}
	// 				myConfigObject.Add(new HSUnit(path, false, Convert.ToDateTime(starttime), Convert.ToDateTime(stoptime)));
	// 			}
	// 			return myConfigObject;
	// 		}
	// 	}


	public class HSUnitCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new HSUnitElement();
		}

		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((HSUnitElement)element).Path;
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
		[ConfigurationProperty("path", IsRequired = true)]
		public string Path
		{
			get { return (string)this["path"]; }
			set { this["path"] = value; }
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
	public class HSUnitSection : ConfigurationSection
	{

		// Declare the UrlsCollection collection property.
		[ConfigurationProperty("HSUnit", IsRequired = true)]
		public HSUnitCollection HSUnit
		{
			get	{ return (HSUnitCollection)base["HSUnit"]; }
			set	{ base["HSUnit"] = value;}
		}
	}
}