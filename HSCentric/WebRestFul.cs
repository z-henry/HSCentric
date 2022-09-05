using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace HSCentric
{
	// 定义两种方法，1、GetInputFunc：input，返回input；2、PostInputFunc：通过POST请求，传入input，返回input
	[ServiceContract(Name = "GetInputService")]
	public interface IRestFulServices
	{
		//说明：GET请求
		//WebGet默认请求是GET方式
		//UriTemplate(URL Routing)input必须要方法的参数名必须一致不区分大小写）
		//curl "http://124.221.7.52:9000/hscentric/v1/Hearthstone" -X GET
// 		[OperationContract]
// 		[WebGet(UriTemplate = "hscentric/v1/{input}", BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
// 		ResultInfo GetInputFunc(string input);

		//说明：POST请求
		//WebInvoke请求方式有POST、PUT、DELETE等，所以需要明确指定Method是哪种请求的，这里我们设置POST请求。
		//UriTemplate(URL Routing)input必须要方法的参数名必须一致不区分大小写）
		//RequestFormat规定客户端必须是什么数据格式请求的（JSon或者XML），不设置默认为XML
		// ResponseFormat规定服务端返回给客户端是以是什么数据格返回的（JSon或者XML）
		//>curl "http://124.221.7.52:9000/hscentric/v1" -H "Content-Type:application/json" -d "{\"enable\":false,\"name\":\"Hearthstone\"}" -X POST
		[OperationContract]
		[WebInvoke(Method = "POST", UriTemplate = "hscentric", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		ResultInfo PostInputFunc(RequestInfo requestInfo);
	}

	[DataContract]
	public class UnitInfo
	{
		[DataMember]
		public bool enable { set; get; }
		[DataMember]
		public string name { set; get; }
		[DataMember]
		public string level { set; get; }
		[DataMember]
		public int xp { set; get; }
		[DataMember]
		public int pvp_rate { set; get; }

		[DataMember]
		public string classic_rate { set; get; }

	}
	[DataContract]
	public class ResultInfo
	{
		[DataMember]
		public int result { set; get; }
		[DataMember]
		public List<UnitInfo> units { set; get; }
	}
	[DataContract]
	public class RequestInfo
	{
		[DataMember]
		public string func { set; get; }
		[DataMember]
		public UnitInfo unit { set; get; }
	}
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
	public class MyRestFulServices : IRestFulServices
	{
		public ResultInfo PostInputFunc(RequestInfo requestInfo)
		{
			ResultInfo resultInfo = new ResultInfo() { result = 1, units = new List<UnitInfo>()};
			switch (requestInfo.func)
			{
				case "query":
					foreach (var iter in HSUnitManager.GetHSUnits())
					{
						resultInfo.units.Add(new UnitInfo()
						{
							enable = iter.Enable,
							name = iter.ID,
							level = string.Format("等级:{0} 经验{1}", iter.XP.Level, iter.XP.ProgressXP),
							xp = iter.XP.TotalXP,
							pvp_rate = iter.MercPvpRate,
							classic_rate = iter.ClassicRate,
						});
					}
					break;

				case "switch":
					HSUnitManager.SetEnable(requestInfo.unit.name, requestInfo.unit.enable);
					Out.Log(string.Format("post修改[{0}][{1}]", requestInfo.unit.name, requestInfo.unit.enable));
					break;

				default:
					resultInfo.result = 0;
					break;
			}
			return resultInfo;
		}
	}

	public static class MyRestFul
	{
		public static void Init(string url)
		{
			if (string.IsNullOrEmpty(url))
				return;
			MyRestFulServices demoServices = new MyRestFulServices();
			m_serviceHost = new WebServiceHost(demoServices, new Uri("http://" + url + "/"));
			m_serviceHost.Open();
		}
		public static void Rlease()
		{
			m_serviceHost.Close();
		}

		static WebServiceHost m_serviceHost;
	}
}