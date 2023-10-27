using code_add_bp_sl.Model;
using code_add_bp_sl.Model.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace code_add_bp_sl
{
    internal class Program
    {
        private static string baseUrl = "http://localhost:50001/b1s/v1"; // Replace with your SAP Service Layer base URL


        static void Main(string[] args)
        {
            var sessionId = Login();

            if (!string.IsNullOrEmpty(sessionId))
            {
                CreateBusinessPartner(sessionId);
                Logout(sessionId);
            }
            else
            {
                Console.WriteLine("Login failed.");
            }

            Console.ReadLine();
        }

        private static string Login()
        {
            // Request details
            string url = $"{baseUrl}/Login";
            LoginRequest loginRequest = new LoginRequest()
            {
                UserName = "manager",
                Password = "corexray",
                CompanyDB = "SBODemoUS"
            };

            // Serialize request body to JSON
            string jsonRequestBody = JsonConvert.SerializeObject(loginRequest);

            // Make the request
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            httpWebRequest.ServicePoint.Expect100Continue = false;

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(jsonRequestBody);
            }

            try
            {
                // Call Service Layer
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();

                    // Deserialize success response
                    var responseInstance = JsonConvert.DeserializeObject<LoginResponse>(result);

                    Console.WriteLine("Logged in successfully.");

                    return responseInstance.SessionId;
                }
            }
            catch (Exception ex)
            {
                // Unauthorized, etc.
                Console.WriteLine("Unexpected: " + ex.Message);
            }

            return null;
        }

        private static void CreateBusinessPartner(string sessionId)
        {
            string businessPartnerUrl = $"{baseUrl}/BusinessPartners";

            // Create a JSON payload for the new Business Partner
            JObject businessPartnerData = new JObject
            {
                { "CardCode", "DEMO1" }, // Provide a unique Business Partner code
                { "CardName", "Maxi-Teq" },
                { "CardType", "C" }, // C for Customer, S for Supplier, etc. Modify as needed
                // Add more fields as required
            };

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(businessPartnerUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(businessPartnerData.ToString());
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        Console.WriteLine("Business Partner created successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to create Business Partner. Status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while creating the Business Partner: " + ex.Message);
            }
        }

        private static void Logout(string sessionId)
        {
            string logoutUrl = $"{baseUrl}/Logout";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(logoutUrl);
                request.Method = "POST";
                request.Accept = "application/json";
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.NoContent
                        || response.StatusCode == HttpStatusCode.OK)
                    {
                        Console.WriteLine("Logged out successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Logout failed. Status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during logout: " + ex.Message);
            }
        }
    }
}
