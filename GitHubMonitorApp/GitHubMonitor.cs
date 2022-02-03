using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.IO;
using RestSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Linq;

namespace GitHubMonitorApp
{
    public static class GitHubMonitor
    {
        [FunctionName("GitHubMonitor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var client = new RestClient("https://login.microsoftonline.com/664df9af-6e9b-4fbc-99f2-ec07955c6b09/oauth2/v2.0/token");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials", ParameterType.GetOrPost);
            request.AddParameter("client_id", "53c60ca6-a726-4921-91a0-3b5ab035fdcf", ParameterType.GetOrPost);
            request.AddParameter("client_secret", "2wM7Q~3WBOKqY4KJUYproLT5aweew40mNGrrB", ParameterType.GetOrPost);
            request.AddParameter("scope", "https://management.azure.com/.default", ParameterType.GetOrPost);
            IRestResponse response = client.Execute(request);
            String responseBody = response.Content;
            AuthenticationRecord AuthenticationRecord = JsonConvert.DeserializeObject<AuthenticationRecord>(response.Content);
            String accessToken = AuthenticationRecord.access_token;
            String get_responseBody = "";
            if (accessToken != null)
            {
                var get_client = new RestClient("https://management.azure.com//subscriptions/c5117ac3-6d25-48cd-b4ec-13e7c8918cb7//providers/Microsoft.Consumption/usageDetails?api-version=2021-10-01");
                var get_request = new RestRequest(Method.GET);
                get_request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                get_request.AddHeader("Authorization", "Bearer " + accessToken);
                get_request.AddParameter("grant_type", "client_credentials", ParameterType.GetOrPost);
                get_request.AddParameter("client_id", "53c60ca6-a726-4921-91a0-3b5ab035fdcf", ParameterType.GetOrPost);
                get_request.AddParameter("client_secret", "2wM7Q~3WBOKqY4KJUYproLT5aweew40mNGrrB", ParameterType.GetOrPost);
                get_request.AddParameter("scope", "https://management.azure.com/.default", ParameterType.GetOrPost);
                IRestResponse get_response = get_client.Execute(get_request);
                get_responseBody = get_response.Content;
            }
            string text = get_responseBody;
            string path = @"C:\Users\chrixu\source\repos\AzureFunctionDemo\GitHubMonitorApp\BillingData.txt";
            int count = 0;
            foreach (char c in text)
            {
                count += 1;
            }
            using (var tw = new StreamWriter(path, true))
            {
                tw.Write(text);
            }
            var config = GetConfiguration();
            var files = GetFiles(config["AzureStorage:SourceFolder"]);
            
            if (!files.Any())
            {
                Console.WriteLine("Nothing to process");
                return null;
            }
            UploadFiles(files, config["AzureStorage:ConnectionString"], config["AzureStorage:Container"]);
            return null;

        }

        
        static IConfigurationRoot GetConfiguration()
            => new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json")
                .Build();

        static IEnumerable<FileInfo> GetFiles(string sourceFolder)
            => new DirectoryInfo(sourceFolder)
                .GetFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden));

        static void UploadFiles(
            IEnumerable<FileInfo> files,
            string connectionString,
            string container)
        {
            var containerClient = new BlobContainerClient(connectionString, container);

            Console.WriteLine("Uploading files to blob storage");

            foreach (var file in files)
            {
                try
                {
                    var blobClient = containerClient.GetBlobClient(file.Name);
                    using (var fileStream = File.OpenRead(file.FullName))
                    {
                        blobClient.Upload(fileStream);
                    }

                    Console.WriteLine($"{file.Name} uploaded");
                    File.Delete(file.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }


    public class AuthenticationRecord
    {
       public string token_type { get; set; }
       public int expire_in { get; set; }
       public int ext_expires_in { get; set; }
       public string access_token { get; set; } 
    }

}
