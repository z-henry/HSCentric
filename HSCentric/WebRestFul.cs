using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Runtime.Serialization;

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
		[OperationContract]
		[WebGet(UriTemplate = "hscentric/v1/{input}", BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
		ResultInfo GetInputFunc(string input);

		//说明：POST请求
		//WebInvoke请求方式有POST、PUT、DELETE等，所以需要明确指定Method是哪种请求的，这里我们设置POST请求。
		//UriTemplate(URL Routing)input必须要方法的参数名必须一致不区分大小写）
		//RequestFormat规定客户端必须是什么数据格式请求的（JSon或者XML），不设置默认为XML
		// ResponseFormat规定服务端返回给客户端是以是什么数据格返回的（JSon或者XML）
		//>curl "http://124.221.7.52:9000/hscentric/v1" -H "Content-Type:application/json" -d "{\"enable\":false,\"name\":\"Hearthstone\"}" -X POST
		[OperationContract]
		[WebInvoke(Method = "POST", UriTemplate = "hscentric/v1", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		ResultInfo PostInputFunc(UnitInfo unitinfo);
	}

	[DataContract]
	public class UnitInfo
	{
		[DataMember]
		public bool enable { set; get; }
		[DataMember]
		public string name { set; get; }
	}
	[DataContract]
	public class ResultInfo
	{
		[DataMember]
		public UnitInfo unit { set; get; }
		[DataMember]
		public int result { set; get; }
	}
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
	public class MyRestFulServices : IRestFulServices
	{
		public ResultInfo GetInputFunc(string Input)
		{
			foreach (var iter in HSUnitManager.GetHSUnits())
			{
				if (iter.NickName == Input)
					return new ResultInfo()
					{
						result = 1,
						unit = new UnitInfo()
						{
							enable = iter.Enable,
							name = iter.NickName,
						},
					};
			}

			return new ResultInfo()
			{
				result = 0,
			};
		}
		public ResultInfo PostInputFunc(UnitInfo unitinfo)
		{
			if (unitinfo == null)
				return new ResultInfo()
				{
					result = 0,
				};
			HSUnitManager.SetEnable(unitinfo.name, unitinfo.enable);
			return new ResultInfo()
			{
				result = 1,
			};
		}
	}

	public static class MyRestFul
	{
		public static void Init()
		{
			MyRestFulServices demoServices = new MyRestFulServices();
			m_serviceHost = new WebServiceHost(demoServices, new Uri("http://124.221.7.52:9000/"));
			m_serviceHost.Open();
		}
		public static void Rlease()
		{
			m_serviceHost.Close();
		}

		static WebServiceHost m_serviceHost;
	}
}