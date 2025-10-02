using FireSharp.Config;
using FireSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FireSharp;
using FireSharp.Response;
using Newtonsoft.Json;
using server.Models;

namespace server.Services
{
    class Connection
    {
        public IFirebaseConfig fc = new FirebaseConfig()
        {
            // Read Realtime DB settings from environment variables or configuration
            AuthSecret = Environment.GetEnvironmentVariable("FIREBASE_REALTIME_AUTH_SECRET") ?? "",
            BasePath = Environment.GetEnvironmentVariable("FIREBASE_REALTIME_BASEPATH") ?? ""
        };

        public IFirebaseClient client;
        public Connection()
        {
            try
            {
                client = new FireSharp.FirebaseClient(fc);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed Connection");
            }
        }
    }

    public static class Crud
    {
        static Connection conn = new Connection();

        public static async Task SetDataAsync(string path, ProjectModel projectModel)
        {
            try
            {
                projectModel.SprintList ??= new List<SprintModel>();

                var response = await conn.client.SetAsync(path, projectModel);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("Data saved successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to set data. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set data: {ex.Message}");
            }
        }

        public static async Task<T?> GetDataAsync<T>(string path) where T : class
        {
            try
            {
                T jsonObject = null;

                var response = await conn.client.GetAsync(path);
                if (response != null && !string.IsNullOrEmpty(response.Body))
                {
                    jsonObject = JsonConvert.DeserializeObject<T>(response.Body);
                }
                return jsonObject;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get data: {ex.Message}");
                return null;
            }
        

        public static async Task DeleteDataAsync(string path)
        {
            try
            {
                var response = await conn.client.DeleteAsync(path);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("Data deleted successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to delete data. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete data: {ex.Message}");
            }
        }

        //set datas to database


        //Update datas


        //List of the datas

    }
    
}
