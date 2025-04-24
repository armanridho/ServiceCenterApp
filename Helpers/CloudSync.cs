using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Data.SQLite;
using Dapper;
using Newtonsoft.Json;
using System.Windows;
using ServiceCenterApp.Models;

namespace ServiceCenterApp.Helpers
{
    public static class CloudSync
    {
        public static string CloudDbUrl = "http://127.0.0.1/database/cloud_data.db";
        public static string CloudUploadUrl = "http://127.0.0.1/database/api";
        public static string LocalCloudDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "cloud_data.db");
        private static string dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "database.db");

        public static void UploadModifiedRowsToCloud()
        {
            try
            {
                string lastSync = File.Exists("last_sync.txt")
                    ? File.ReadAllText("last_sync.txt")
                    : "2000-01-01 00:00:00";

                using var conn = new SQLiteConnection($"Data Source={dbFile}");
                conn.Open();

                var modifiedRows = conn.Query<ServiceEntry>(
                    "SELECT * FROM Services WHERE LastUpdated > @LastSyncTime",
                    new { LastSyncTime = lastSync });

                if (!modifiedRows.Any())
                {
                    MessageBox.Show("Tidak ada data yang perlu diupload.");
                    return;
                }

                string jsonPayload = JsonConvert.SerializeObject(modifiedRows);

                using var client = new WebClient();
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.UploadString(CloudUploadUrl, "POST", jsonPayload);

                MessageBox.Show("Upload data ke cloud berhasil.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal upload data ke cloud: " + ex.Message);
            }
        }

        public static void DownloadCloudDatabase()
        {
            try
            {
                using var client = new WebClient();

                var folder = Path.GetDirectoryName(LocalCloudDbPath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                client.DownloadFile(CloudDbUrl, LocalCloudDbPath);

                File.WriteAllText("last_sync.txt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal download database cloud: " + ex.Message);
            }
        }
    }
}
