using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using System.IO;
using RestSharp.Authenticators;
using RestSharp;
using RestSharp.Extensions;
using System.Reflection;
using System.Net.Sockets;
using System.Net;
using RestSharp.IntegrationTests.Helpers;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Data.SqlClient;


namespace ConsoleApplication3
{
    public class AccessToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public string resource_uri { get; set; }
        public string user { get; set; }
        public string scopes { get; set; }
    }

    class HxkAuthenticator
    {
        /// The client information used to get the OAuth2 Access Token from the server.

        private const string sLoopbackCallback = "https://localhost:44300/";
        private string sAuthorizationCallBackURL;
        protected AccessToken m_accessToken = null;

        static string mc_apiKey = "SXKSvKcEEKX7uRgP";
        static string mc_appsecret = "jsc0MG44NcNstKAuv6w9JBpqvvIIhR2Y10E1PgtU";


        static string baseUrl = "https://api.hexoskin.com/api/connect/oauth2/";


        // this will hold the Access Token returned from the server.
        static AccessToken accessToken = null;

        private static int auth_GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
        static void Main(string[] args)
        {
            string scope = "readonly";
            string state = "SuKaUunEhBUpYlBhYllrBMVxHl1UE2";

            var client = new RestClient(baseUrl);


            // Create a Callback URL
            string sAuthorizationCallBackURL = string.Format(
                    sLoopbackCallback,
                    auth_GetRandomUnusedPort(), Assembly.GetEntryAssembly().GetName().Name
                    );

            var request = new RestRequest(
                   string.Format(
                   "auth/?response_type=code&client_id={0}&redirect_uri={1}&scope={2}&state={3}",
                   mc_apiKey, HttpUtility.UrlEncode(sAuthorizationCallBackURL), scope, state
                   ), Method.POST);

            bool bHasUserGrantedAccess = false;

            var url = client.BuildUri(request).ToString();
            var url1 = baseUrl + request.Resource;

            // Set up a local HTTP server to accept authetization callback

            string auth_code = null;
            var resetEvent = new ManualResetEvent(false);
            using (var svr = SimpleServer.Create(sAuthorizationCallBackURL, context =>
            {
                var qs = HttpUtility.ParseQueryString(context.Request.RawUrl);
                auth_code = qs["code"];

                if (!string.IsNullOrEmpty(auth_code))
                {
                    // The user has granted access
                    bHasUserGrantedAccess = true;
                }

                // Resume execution...
                resetEvent.Set();

            }))
            {
                // Launch a default browser to get the user's approval
                System.Diagnostics.Process.Start(url1);

                // Wait until the user decides whether to grant access
                resetEvent.WaitOne();

            }

            if (false == bHasUserGrantedAccess)
            {
                // The user has not granded access
                // break;
            }

            string authorizationCode = auth_code;

            request = new RestRequest("token/", Method.POST);
            request.AddParameter("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mc_apiKey + ":" + mc_appsecret)), ParameterType.HttpHeader);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", authorizationCode);
            request.AddParameter("redirect_uri", sAuthorizationCallBackURL);


           // var response = client.Execute(request);
            var response = client.Execute<AccessToken>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                //  break;
            }

            // Extract the Access Token

            accessToken = response.Data;

            if (string.IsNullOrEmpty(accessToken.access_token) ||
                string.IsNullOrEmpty(accessToken.refresh_token) ||
                (0 == accessToken.expires_in))
            {
                // break;
            }
            //  Test and insert user credentials / PatientConnector
            bool IsAuthenticated = false;

            RefreshAccessToken(accessToken.refresh_token);


            IsAuthenticated = TestUserCredentials(accessToken.access_token);

            //if (IsAuthenticated)
            //{
            //    UpdatePatientConnector(ref accessToken);
            //}
        }

        public static bool TestUserCredentials(string access_Token)
        {
            bool IsAuthenticated = false;

            baseUrl = "https://api.hexoskin.com/api/v1/";

            var client = new RestClient(baseUrl);

            var request = new RestRequest("account/", Method.GET);

            request.AddParameter(
                "Authorization",
                string.Format("Bearer {0}", accessToken.access_token), ParameterType.HttpHeader);

            var response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                //break;
            }

            String strJSON__2 = response.Content;
            JObject ser = JObject.Parse(strJSON__2);
            List<JToken> data = ser.Children().ToList();

            foreach (JProperty item in data)
            {

                item.CreateReader();
                if (item.Name == "objects")
                {
                    foreach (JObject msg in item.Values())
                    {
                        IsAuthenticated = true;
                        break;
                    }
                }
            }

            return IsAuthenticated;
        }

        public static void RefreshAccessToken( string refresh_token)
        {
            var client = new RestClient(baseUrl);

            // Create a Callback URL
            string sAuthorizationCallBackURL = string.Format(
                    sLoopbackCallback,
                    auth_GetRandomUnusedPort(), Assembly.GetEntryAssembly().GetName().Name
                    );

            var request = new RestRequest("token/", Method.POST);
            request.AddParameter("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mc_apiKey + ":" + mc_appsecret)), ParameterType.HttpHeader);
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", refresh_token);


            var response = client.Execute<AccessToken>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                //  break;
            }

            // Extract the Access Token

            accessToken = response.Data;

            if (string.IsNullOrEmpty(accessToken.access_token) ||
                string.IsNullOrEmpty(accessToken.refresh_token) ||
                (0 == accessToken.expires_in))
            {
                // break;
            }
            //  Test and insert user credentials / PatientConnector
            bool IsAuthenticated = false;
            IsAuthenticated = TestUserCredentials(accessToken.access_token);

            if (IsAuthenticated)
            {
                UpdatePatientConnector(ref accessToken);
            }


        }
        public static void UpdatePatientConnector(ref AccessToken access_Token)
        {
            string sConnString = "Server=tcp:abfn2htvtg.database.windows.net,1433;Database=M2M-LabsFoundation_db;User ID=M2M-Data@abfn2htvtg;Password=QMeFit321!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";//@"Data Source=myServerName\myInstanceName;Initial Catalog=myDataBase;User ID=myUsername;Password=myPassword;";
            string cmdText;

            SqlConnection sConn = new SqlConnection(sConnString);

            sConn.Open();
                 
            cmdText = "insert into dbo.PatientConnectors " +
                      " ( PatientID, ConnectorID, DateCreation, ConnectorName, "  +
                      " Description, AccessTokenKey, AccessTokenSecret, RefreshTokenkey, " +
                      " AccessUserID, Registration_Date, SourceConnector_ID ) " +
                      " values ('" + "renatapensa" + "', '"
                                + 2 + "', '"
                                + DateTime.Today + "', '"
                                + "Hexoskin" + "', '"
                                + "Hexoskin" + "', '"
                                + access_Token.access_token + "', '"
                                + "" + "', '"
                                + "" + "', '"
                                + accessToken.resource_uri + "', '"
                                + DateTime.Today + "', '"
                                + 2 + "' )";

            SqlCommand myCmd = new SqlCommand(cmdText);
            myCmd.Connection = sConn;

            myCmd.ExecuteNonQuery();
        }

    }
}


