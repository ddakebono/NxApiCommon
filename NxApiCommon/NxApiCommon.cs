using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NxApiCommon
{
    public class NxApiClient
    {
		private RestClient client;
		private CookieContainer cookies;
		private string apiToken;
		private double accessTokenExpires = 0;
		private readonly String restDomain = "https://api.nexon.io";
		private readonly String userAgent = "NexonLauncher.nxl-18.13.13-62-f86a181-coreapp-2.1.0";
        private readonly string clientID = "7853644408";
		public string passportToken;
		private string password;
		private string username;
		private string appID;
        public string manifestLocation;
        Regex matcher = new Regex("\\d{5}\\.(\\d+)R", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
            ProductInfoJson prodinfo = JsonConvert.DeserializeObject<ProductInfoJson>(response.Content);
            manifestLocation = prodinfo.manifest_url;
			return int.Parse(matcher.Matches(manifestLocation)[0].Groups[1].Value);
		}

        private void trustDevicePost()
        {
            string code = Microsoft.VisualBasic.Interaction.InputBox("Please enter the 2 factor verification code sent to your email.", "Trust Device");
            if (!String.IsNullOrEmpty(code))
            {
                var apiClient = new RestClient("https://www.nexon.com");
                apiClient.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) NexonLauncher/2.1.0 Chrome/66.0.3359.181 Electron/3.0.4 Safari/537.36";
                var request = new RestRequest("account-webapi/trusted_devices", Method.PUT);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var requestBody = new TrustDevice
                {
                    email = username,
                    verification_code = code,
                    device_id = NxUtil.getUUID(),
                    remember_me = true
                };

                request.AddJsonBody(requestBody);
                IRestResponse response = apiClient.Execute(request);
                if(response.StatusCode != HttpStatusCode.OK)
                {
                    MessageBox.Show("That code wasn't accepted!");
                }
                else
                {
                    //Retry login with trusted device
                    getAPIToken();
                }
            } 
            else
            {
                MessageBox.Show("You must enter something into the 2FA code prompt");
            }
        }

		public void getAPIToken()
		{
            if (apiToken == null || DateTimeOffset.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds >= accessTokenExpires)
            {
                var apiClient = new RestClient("https://www.nexon.com");
                apiClient.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) NexonLauncher/2.1.0 Chrome/66.0.3359.181 Electron/3.0.4 Safari/537.36";
                var request = new RestRequest("account-webapi/login/launcher", Method.POST);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
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
                if (body["code"]!=null && ((String)body["code"]).Equals("TRUST_DEVICE_REQUIRED", StringComparison.InvariantCultureIgnoreCase))
                {
                    trustDevicePost();
                }
                else
                {
                    TimeSpan span = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                    apiToken = body["access_token"];
                    string expireTime = body["access_token_expires_in"];
                    accessTokenExpires = span.TotalSeconds + Double.Parse(expireTime);
                    //add to rest client cookies
                    cookies.Add(new Cookie("nxtk", apiToken) { Domain = ".nexon.net" });
                }
			}
		}

		private struct AccountLoginJson
		{
			public string id { get; set; }

			public string password { get; set; }

            public string client_id { get; set; }

            public string device_id { get; set; }

            public string scope { get; set; }

            public bool auto_login { get; set; }

		}

        private struct ProductInfoJson
        {
            public int product_id { get; set; }

            public string client_id { get; set; }

            public string display_name { get; set; }

            public string manifest_url { get; set; }

        }

        private struct TrustDevice
        {
            public string email { get; set; }

            public string verification_code { get; set; }

            public string device_id { get; set; }

            public bool remember_me { get; set; }
        }
	}
}
