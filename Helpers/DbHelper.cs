using System;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using Dapper;

public static class DbHelper
{
    private static readonly string dbDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
    private static readonly string dbFile = Path.Combine(dbDirectory, "servicecenter.db");

    // Path & URL untuk cloud DB
    public static string CloudDbUrl = "http://127.0.0.1/database/cloud_data.db";
    public static string LocalCloudDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "cloud_data.db");

    // Inisialisasi awal: bikin DB lokal kalau belum ada
    static DbHelper()
    {
        if (!Directory.Exists(dbDirectory))
            Directory.CreateDirectory(dbDirectory);

        if (!File.Exists(dbFile))
        {
            SQLiteConnection.CreateFile(dbFile);
            InitDatabase();
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
        }
        catch (Exception ex)
        {
            MessageBox.Show("Gagal download database cloud: " + ex.Message);
        }
    }

    private static void InitDatabase()
    {
        using var conn = GetConnection();
        conn.Open();

        string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS Services (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RowNumber INTEGER,
    CustomerName TEXT,
    Item TEXT,
    SerialNumber TEXT,
    WarrantyStatus TEXT,
    Accessories TEXT,
    Problem TEXT,
    Status TEXT,
    DateIn TEXT,
    ServiceDate TEXT,
    DateOut TEXT,
    ServiceLocation TEXT,
    LastUpdated DATETIME
);

        ";
        conn.Execute(createTableQuery);
        Console.WriteLine("Tabel Services telah dibuat.");
    }
}
