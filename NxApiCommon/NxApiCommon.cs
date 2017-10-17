using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NxApiCommon
{
    public class NxApiClient
    {
		private RestClient client;
		private CookieContainer cookies;
		private string apiToken;
		private double accessTokenExpires = 0;
		private readonly String restDomain = "https://api.nexon.io";
		private readonly String userAgent = "NexonLauncher.nxl-17.05.05-479-4a3d51e";
		private readonly string clientID = "7853644408";
		public string passportToken;
		private string password;
		private string username;
		private string appID;
		SHA512 sha = new SHA512Managed();

		public NxApiClient(String username, String password, String appID)
		{
			this.username = username;
			this.password = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-", "").ToLower();
			this.appID = appID;
			client = new RestClient(restDomain);
			client.UserAgent = userAgent;
			cookies = new CookieContainer();
			client.CookieContainer = cookies;
		}

		public string getNxPassport()
		{
			getAPIToken();
			var request = new RestRequest("users/me/passport", Method.GET);
			request.AddHeader("Authorization", "Bearer " + apiToken);
			var response = client.Execute(request);
			var body = JsonConvert.DeserializeObject<dynamic>(response.Content);
			return body["passport"];
		}

		public int getVersion()
		{
			getAPIToken();
			var request = new RestRequest("products/" + appID, Method.GET);
			request.AddHeader("Authorization", "Bearer " + apiToken);
			var response = client.Execute(request);
			Console.WriteLine("GET VERSION: " + response.Content);
			return 0;
		}

		public void getAPIToken()
		{
			if (apiToken == null || DateTimeOffset.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds >= accessTokenExpires)
			{
				var apiClient = new RestClient("https://accounts.nexon.net");
				apiClient.UserAgent = userAgent;
				var request = new RestRequest("account/login/launcher", Method.POST);
				var requestBody = new AccountLoginJson
				{
					id = username,
					password = password,
					auto_login = false,
					client_id = clientID,
					scope = "us.launcher.all",
					device_id = NxUtil.getUUID(),
				};
				request.AddJsonBody(requestBody);
				IRestResponse response = apiClient.Execute(request);
				var body = JsonConvert.DeserializeObject<dynamic>(response.Content);
				TimeSpan span = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
				apiToken = body["access_token"];
				string expireTime = body["access_token_expires_in"];
				accessTokenExpires = span.TotalSeconds + Double.Parse(expireTime);
				//add to rest client cookies
				cookies.Add(new Cookie("nxtk", apiToken) { Domain = ".nexon.net" });
			}
		}

		private struct AccountLoginJson
		{
			public string id { get; set; }

			public string password { get; set; }

			public bool auto_login { get; set; }

			public string client_id { get; set; }

			public string scope { get; set; }

			public string device_id { get; set; }
		}
	}
}
