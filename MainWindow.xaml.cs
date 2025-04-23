using System.Windows;
using Dapper;
using System.Data.SQLite;
using ServiceCenterApp.Models;
using ServiceCenterApp.Views;
using System.Collections.Generic;
using System.Windows.Controls;
using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using System.Collections.ObjectModel;

namespace ServiceCenterApp
{
    public partial class MainWindow : Window
    {
        private DateTime? localLastUpdated = null;
        public MainWindow()
        {
            InitializeComponent();
            LoadServices();
            // Bandingkan dengan data Cloud
            DbHelper.DownloadCloudDatabase();
            DateTime? cloudLastUpdated = CheckLastUpdatedCloud();


            if (cloudLastUpdated > localLastUpdated)
            {
                MessageBox.Show("Ada data baru di Cloud! Silakan lakukan sync manual atau otomatis.");
            }
            else
            {
                MessageBox.Show("Data lokal sudah paling update.");
            }

            // Set window maximized, tapi tanpa menghilangkan taskbar
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.SingleBorderWindow;

            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saat load data: " + ex.Message);
            }
        }

        private DateTime? CheckLastUpdatedCloud()
        {
            DateTime? cloudLastUpdated = null;
            try
            {
                using (var conn = DbHelper.GetConnectionCloud()) // ganti kalau nama function beda
                {
                    conn.Open();
                    var query = "SELECT MAX(LastUpdated) FROM Services";
                    cloudLastUpdated = conn.ExecuteScalar<DateTime?>(query);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal cek pembaruan dari Cloud: " + ex.Message);
            }

            return cloudLastUpdated;
        }


        // Nah ini dipindah ke luar
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterServices(SearchBox.Text);
        }


        private ObservableCollection<ServiceEntry> AllServices = new ObservableCollection<ServiceEntry>();
        private ObservableCollection<ServiceEntry> FilteredServices = new ObservableCollection<ServiceEntry>();
        private void LoadData()
        {
            using var conn = DbHelper.GetConnection();
            conn.Open();
            var data = conn.Query<ServiceEntry>("SELECT * FROM Services").ToList();

            for (int i = 0; i < data.Count; i++)
            {
                data[i].RowNumber = i + 1;  // Assign nomor urut
            }

            dgServices.ItemsSource = new ObservableCollection<ServiceEntry>(data);  // Pastikan data di-convert ke ObservableCollection
        }


        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            var addForm = new AddService();
            addForm.ShowDialog();
            LoadData(); // refresh


        }
        private void LoadServices()
        {
            using var conn = DbHelper.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Services";
            using var reader = cmd.ExecuteReader();

            AllServices.Clear();
            while (reader.Read())
            {
                AllServices.Add(new ServiceEntry
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    RowNumber = int.TryParse(reader["RowNumber"]?.ToString(), out var row) ? row : 0,
                    CustomerName = reader["CustomerName"].ToString(),
                    Item = reader["Item"].ToString(),
                    SerialNumber = reader["SerialNumber"].ToString(),
                    Problem = reader["Problem"].ToString(),
                    Status = reader["Status"].ToString(),
                    DateIn = DateTime.TryParse(reader["DateIn"]?.ToString(), out var dIn) ? dIn : (DateTime?)null,
                    ServiceDate = DateTime.TryParse(reader["ServiceDate"]?.ToString(), out var dService) ? dService : (DateTime?)null,
                    DateOut = DateTime.TryParse(reader["DateOut"]?.ToString(), out var dOut) ? dOut : (DateTime?)null,
                    ServiceLocation = reader["ServiceLocation"]?.ToString(),
                    LastUpdated = DateTime.TryParse(reader["LastUpdated"]?.ToString(), out var dLast) ? dLast : (DateTime?)null,
                });
            }

            localLastUpdated = AllServices.Max(s => s.LastUpdated); // global var
            var cloudLastUpdated = CheckLastUpdatedCloud();

            if (CompareLastUpdated(localLastUpdated, cloudLastUpdated))
            {
                var result = MessageBox.Show("Data di Cloud lebih baru. Apakah Anda ingin melakukan sinkronisasi sekarang?", "Sinkronisasi Data", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    SyncToLocal();
                }
            }
            dgServices.ItemsSource = AllServices; // <- ini diganti dari 'list' ke 'AllServices'
            FilterServices(""); // tampilkan semua awalnya
        }


        private void FilterServices(string keyword)
        {
            var filtered = AllServices
    .Where(x => x.CustomerName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
             || x.Item.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
             || x.Status.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
    .ToList();


            FilteredServices.Clear();
            foreach (var entry in filtered)
                FilteredServices.Add(entry);

            dgServices.ItemsSource = FilteredServices;
        }

        private void EditService_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedEntry = button?.Tag as ServiceEntry;
            var editWindow = new AddService(selectedEntry); // lempar data
            editWindow.ShowDialog();
            LoadServices(); // reload setelah edit
            DateTime? localLastUpdated = AllServices.Max(s => s.LastUpdated);

        }
        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "services_export.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Services");

                // Header
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "RowNumber";
                worksheet.Cell(1, 3).Value = "Customer Name";
                worksheet.Cell(1, 4).Value = "Serial Number";  // Menambahkan header serial number
                worksheet.Cell(1, 5).Value = "Item";
                worksheet.Cell(1, 6).Value = "Problem";
                worksheet.Cell(1, 7).Value = "Status";
                worksheet.Cell(1, 8).Value = "Date In";
                worksheet.Cell(1, 9).Value = "Service Date";
                worksheet.Cell(1, 10).Value = "Date Out";
                worksheet.Cell(1, 11).Value = "Service Location";
                worksheet.Cell(1, 12).Value = "Last Updated";

                int row = 2;
                foreach (ServiceEntry entry in dgServices.ItemsSource)
                {
                    worksheet.Cell(1, 1).Value = "Id";
                    worksheet.Cell(1, 2).Value = "RowNumber";
                    worksheet.Cell(1, 3).Value = "Customer Name";
                    worksheet.Cell(1, 4).Value = "Serial Number";  // Menambahkan header serial number
                    worksheet.Cell(1, 5).Value = "Item";
                    worksheet.Cell(1, 6).Value = "Problem";
                    worksheet.Cell(1, 7).Value = "Status";
                    worksheet.Cell(1, 8).Value = "Date In";
                    worksheet.Cell(1, 9).Value = "Service Date";
                    worksheet.Cell(1, 10).Value = "Date Out";
                    worksheet.Cell(1, 11).Value = "Service Location";
                    worksheet.Cell(1, 12).Value = "Last Updated";
                    row++;
                }

                workbook.SaveAs(saveFileDialog.FileName);
                MessageBox.Show("Data berhasil diexport ke Excel!");
            }
        }

        private void ImportData_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var imported = new List<ServiceEntry>();

                using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1); // ambil sheet pertama

                DateTime? GetDateSafe(IXLCell cell)
                {
                    if (cell.DataType == XLDataType.DateTime)
                        return cell.GetDateTime();

                    if (DateTime.TryParse(cell.GetString(), out var parsed))
                        return parsed;

                    return null; // atau bisa juga DateTime.MinValue kalau kamu butuh default
                }

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    imported.Add(new ServiceEntry
                    {
                        CustomerName = row.Cell(2).GetString(),
                        RowNumber = row.RowNumber(),
                        Item = row.Cell(4).GetString(),
                        SerialNumber = row.Cell(5).GetString(),
                        Problem = row.Cell(6).GetString(),
                        Status = row.Cell(7).GetString(),
                        DateIn = GetDateSafe(row.Cell(8)),
                        ServiceDate = GetDateSafe(row.Cell(9)),
                        DateOut = GetDateSafe(row.Cell(10)),
                        ServiceLocation = row.Cell(11).GetString(),
                        LastUpdated = GetDateSafe(row.Cell(12))
                    });
                }

                using var conn = DbHelper.GetConnection();
                conn.Open();
                foreach (var entry in imported)
                {
                    conn.Execute(@"
        INSERT INTO Services 
        (RowNumber, CustomerName, Item, Problem, Status, DateIn, ServiceDate, DateOut, ServiceLocation, LastUpdated)
        VALUES 
        (@RowNumber, @CustomerName, @Item, @Problem, @Status, @DateIn, @ServiceDate, @DateOut, @ServiceLocation, @LastUpdated)", entry);
                }


                MessageBox.Show("Data berhasil diimport dari Excel!");
                LoadData();
            }
        }
        private void dgServices_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ServiceEntry entry)
            {
                var result = MessageBox.Show($"Yakin ingin menghapus data untuk {entry.CustomerName}?", "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using var conn = DbHelper.GetConnection();
                    conn.Open();
                    conn.Execute("DELETE FROM Services WHERE Id = @Id", new { entry.Id });
                    MessageBox.Show("Data berhasil dihapus!");
                    LoadData();
                }
            }
        }
        private bool CompareLastUpdated(DateTime? localLastUpdated, DateTime? cloudLastUpdated)
        {
            if (cloudLastUpdated > localLastUpdated)
            {
                return true; // Cloud lebih baru, butuh sinkronisasi
            }
            return false; // Data lokal sudah paling update
        }
        private void SyncToLocal()
        {
            try
            {
                using (var conn = DbHelper.GetConnectionCloud())
                {
                    conn.Open();
                    var query = "SELECT * FROM Services"; // Query yang sesuai dengan struktur cloud
                    var data = conn.Query<ServiceEntry>(query).ToList();

                    using (var localConn = DbHelper.GetConnection())
                    {
                        localConn.Open();
                        foreach (var entry in data)
                        {
                            // Check apakah entry sudah ada di local, jika tidak insert
                            var exists = localConn.ExecuteScalar<int>("SELECT COUNT(1) FROM Services WHERE Id = @Id", new { entry.Id });
                            if (exists == 0)
                            {
                                localConn.Execute(@"INSERT INTO Services (Id, RowNumber, CustomerName, Item, SerialNumber, Problem, Status, DateIn, ServiceDate, DateOut, ServiceLocation, LastUpdated)
                                             VALUES (@Id, @RowNumber, @CustomerName, @Item, @SerialNumber, @Problem, @Status, @DateIn, @ServiceDate, @DateOut, @ServiceLocation, @LastUpdated)", entry);
                            }
                            else
                            {
                                // Update data lokal jika ada perubahan
                                localConn.Execute(@"UPDATE Services SET 
                                             RowNumber = @RowNumber,
                                             CustomerName = @CustomerName,
                                             Item = @Item,
                                             SerialNumber = @SerialNumber,
                                             Problem = @Problem,
                                             Status = @Status,
                                             DateIn = @DateIn,
                                             ServiceDate = @ServiceDate,
                                             DateOut = @DateOut,
                                             ServiceLocation = @ServiceLocation,
                                             LastUpdated = @LastUpdated
                                             WHERE Id = @Id", entry);
                            }
                        }
                    }
                }
                MessageBox.Show("Sinkronisasi selesai!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Terjadi error saat sinkronisasi: " + ex.Message);
            }
        }

    }
}
