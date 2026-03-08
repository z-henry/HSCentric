using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;

namespace HSCentric
{
	internal sealed class HSApiClient
	{
		public const string EndpointPass = "hscentric/pass";
		public const string EndpointMercenary = "hscentric/mercenary";
		public const string EndpointBattlegrounds = "hscentric/battlegrounds";
		public const string EndpointConstructed = "hscentric/constructed";
		public const string EndpointControlConcedeClose = "hscentric/control/concede-close";

		public sealed class PassInfo
		{
			public int? Level { get; set; }
			public int? ProgressXp { get; set; }
			public bool? IsMax { get; set; }
		}

		public sealed class ConstructedInfo
		{
			public string ClassicRate { get; set; } = "";
			public string StandardText { get; set; } = "";
			public string WildText { get; set; } = "";
		}

		public HSApiClient(string unitId, int hsModPort)
		{
			m_unitId = unitId ?? "";
			m_hsModPort = hsModPort;
		}

		public bool TryGetPassInfo(out PassInfo info)
		{
			info = null;
			if (!TryGetJson(EndpointPass, out JToken root))
				return false;

			var data = GetObjectLikeRoot(root);
			var traditional = GetChild(data, "traditional", "traditionalPass", "classicPass") ?? data;
			info = new PassInfo
			{
				Level = GetInt(traditional, "level", "passLevel", "lvl"),
				ProgressXp = GetInt(traditional, "xp", "progressXp", "progress", "currentXp"),
				IsMax = GetBool(traditional, "isMax", "maxed", "isFull", "full")
			};
			return true;
		}

		public bool TryGetMercenaryPvp(out int? pvpScore)
		{
			return TryGetPvpScore(EndpointMercenary, out pvpScore);
		}

		public bool TryGetBattlegroundsPvp(out int? pvpScore)
		{
			return TryGetPvpScore(EndpointBattlegrounds, out pvpScore);
		}

		public bool TryGetConstructedInfo(out ConstructedInfo info)
		{
			info = null;
			if (!TryGetJson(EndpointConstructed, out JToken root))
				return false;

			var data = GetObjectLikeRoot(root);
			var standard = GetChild(data, "standard", "std");
			var wild = GetChild(data, "wild", "wy");
			string standardText = GetString(standard, "text", "rankText", "tier");
			string wildText = GetString(wild, "text", "rankText", "tier");
			int? standardScore = GetInt(standard, "score", "rating", "rankScore", "points");
			int? wildScore = GetInt(wild, "score", "rating", "rankScore", "points");
			if (string.IsNullOrWhiteSpace(standardText) && standardScore.HasValue)
				standardText = standardScore.Value.ToString();
			if (string.IsNullOrWhiteSpace(wildText) && wildScore.HasValue)
				wildText = wildScore.Value.ToString();

			info = new ConstructedInfo
			{
				ClassicRate = GetString(data, "classicRate", "constructedRate", "rankText"),
				StandardText = standardText,
				WildText = wildText
			};
			return true;
		}

		public bool TryCallConcedeAndClose()
		{
			return TryGetJson(EndpointControlConcedeClose, out _);
		}

		private bool TryGetPvpScore(string endpoint, out int? pvpScore)
		{
			pvpScore = null;
			if (!TryGetJson(endpoint, out JToken root))
				return false;

			var data = GetObjectLikeRoot(root);
			pvpScore = GetInt(data, "pvpScore", "pvp_rate", "pvpRate", "rating", "mmr", "score");
			return true;
		}

		private bool TryGetJson(string endpoint, out JToken root)
		{
			root = null;
			try
			{
				string url = BuildApiUrl(endpoint);
				var response = s_httpClient.GetAsync(url).GetAwaiter().GetResult();
				if (!response.IsSuccessStatusCode)
				{
					Out.Error($"[{m_unitId}] 接口请求失败 {url} http:{(int)response.StatusCode}");
					return false;
				}

				string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				if (string.IsNullOrWhiteSpace(content))
					return false;

				root = JToken.Parse(content);
				return true;
			}
			catch (Exception ex)
			{
				Out.Error($"[{m_unitId}] 接口请求异常 {endpoint}: {ex.Message}");
				return false;
			}
		}

		private static JToken GetObjectLikeRoot(JToken root)
		{
			if (root == null)
				return null;
			return root["data"] ?? root["result"] ?? root;
		}

		private static JToken GetChild(JToken parent, params string[] names)
		{
			if (!(parent is JObject obj))
				return null;
			foreach (var name in names)
			{
				var prop = obj.Properties().FirstOrDefault(
					p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
				if (prop != null)
					return prop.Value;
			}
			return null;
		}

		private static int? GetInt(JToken parent, params string[] names)
		{
			var token = GetChild(parent, names);
			if (token == null)
				return null;
			if (token.Type == JTokenType.Integer)
				return token.Value<int>();
			if (int.TryParse(token.ToString(), out int value))
				return value;
			return null;
		}

		private static bool? GetBool(JToken parent, params string[] names)
		{
			var token = GetChild(parent, names);
			if (token == null)
				return null;
			if (token.Type == JTokenType.Boolean)
				return token.Value<bool>();
			if (bool.TryParse(token.ToString(), out bool value))
				return value;
			return null;
		}

		private static string GetString(JToken parent, params string[] names)
		{
			var token = GetChild(parent, names);
			if (token == null)
				return "";
			return token.ToString();
		}

		private string BuildApiUrl(string endpoint)
		{
			string normalized = (endpoint ?? "").Trim().TrimStart('/');
			return $"http://127.0.0.1:{m_hsModPort}/{normalized}";
		}

		private readonly string m_unitId;
		private readonly int m_hsModPort;

		private static readonly HttpClient s_httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(3)
		};
	}
}
