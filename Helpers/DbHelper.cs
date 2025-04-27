using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using Dapper;
using Newtonsoft.Json;
using ServiceCenterApp.Models;

public static class DbHelper
{
    private static readonly object _initLock = new object();
    private static bool _databaseInitialized = false;
    private static readonly string dbDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
    private static readonly string dbFile = Path.Combine(dbDirectory, "servicecenter.db");

    // Path & URL untuk cloud DB
    public static string CloudDbUrl = "http://127.0.0.1/database/cloud_data.db";
    public static string CloudUploadUrl = "http://127.0.0.1/database/api";
    public static string LocalCloudDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "cloud_data.db");

    // Inisialisasi awal: bikin DB lokal kalau belum ada
    static DbHelper()
    {
        if (!Directory.Exists(dbDirectory))
            Directory.CreateDirectory(dbDirectory);

        InitializeDatabase();
    }

    public static void InitializeDatabase()
    {
        lock (_initLock)
        {
            if (_databaseInitialized) return;

            try
            {
                bool isNewDatabase = !File.Exists(dbFile);

                if (isNewDatabase)
                {
                    SQLiteConnection.CreateFile(dbFile);
                }

                using var conn = GetConnection();
                conn.Open();

                // Create tables if they don't exist
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS Services (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RowNumber INTEGER DEFAULT 0,
                    CustomerName TEXT NOT NULL,
                    Item TEXT NOT NULL,
                    SerialNumber TEXT,
                    CnPn TEXT,
                    WarrantyStatus TEXT,
                    Accessories TEXT,
                    Problem TEXT,
                    HardwareSoftwareProblem TEXT,
                    Status TEXT,
                    UnitLocationStatus TEXT,
                    DateIn DATETIME,
                    ServiceDate DATETIME,
                    DateOut DATETIME,
                    ServiceLocation TEXT,
                    ShippingAddress TEXT,
                    AdditionalNotes TEXT,
                    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
    )");

                // Add any missing columns to existing databases
                if (!isNewDatabase)
                {
                    AddMissingColumns(conn);
                }

                _databaseInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize database: {ex.Message}");
                throw;
            }
        }
    }

    public static SQLiteConnection GetConnection()
    {
        return new SQLiteConnection($"Data Source={dbFile};Version=3;");
    }

    public static SQLiteConnection GetConnectionCloud()
    {
        return new SQLiteConnection($"Data Source={LocalCloudDbPath};Version=3;");
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

            // Simpan waktu sync terakhir
            File.WriteAllText("last_sync.txt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Gagal download database cloud: " + ex.Message);
        }
    }

    public static void UploadModifiedRowsToCloud()
    {
        try
        {
            string lastSync = File.Exists("last_sync.txt")
                ? File.ReadAllText("last_sync.txt")
                : "2000-01-01 00:00:00"; // default awal sync

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

            // Serialize data jadi JSON (gunakan Newtonsoft.Json atau System.Text.Json)
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

    public static void AddMissingColumns(SQLiteConnection conn)
    {
        var requiredColumns = new Dictionary<string, string>
        {
            {"WarrantyStatus", "TEXT"},
            {"RowNumber", "INTEGER DEFAULT 0"},
            {"Status", "TEXT"},
            {"Accessories", "TEXT"}, // Tambahkan ini
            {"Problem", "TEXT"},     // Ini juga, kalo kamu pakai field Problem
            {"ServiceLocation", "TEXT"} // Tambahkan semua yang diperlukan
        };

        foreach (var column in requiredColumns)
        {
            try
            {
                var columnExists = conn.ExecuteScalar<int>(
                    $"SELECT COUNT(*) FROM pragma_table_info('Services') WHERE name='{column.Key}'");

                if (columnExists == 0)
                {
                    conn.Execute($"ALTER TABLE Services ADD COLUMN {column.Key} {column.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding column {column.Key}: {ex.Message}");
            }
        }
    }

        public static void EnsureTableExists(SQLiteConnection conn)
    {
        var tableExists = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Services'");

        if (tableExists == 0)
        {
            conn.Execute(@"
            CREATE TABLE Services (
                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RowNumber INTEGER DEFAULT 0,
                    CustomerName TEXT NOT NULL,
                    Item TEXT NOT NULL,
                    SerialNumber TEXT,
                    CnPn TEXT,
                    WarrantyStatus TEXT,
                    Accessories TEXT,
                    Problem TEXT,
                    HardwareSoftwareProblem TEXT,
                    Status TEXT,
                    UnitLocationStatus TEXT,
                    DateIn DATETIME,
                    ServiceDate DATETIME,
                    DateOut DATETIME,
                    ServiceLocation TEXT,
                    ShippingAddress TEXT,
                    AdditionalNotes TEXT,
                    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
            )");
        }
    }

}


