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
using System.Diagnostics;
using System.Text;
using ServiceCenterApp.Helpers;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ServiceCenterApp
{
    public partial class MainWindow : Window
    {   
        private DateTime? localLastUpdated = null;
        public MainWindow()
        {

            try
            {
                InitializeComponent();
                this.Loaded += (s, e) =>
                {
                    LoadServices();
                };

                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initalizing window: {ex.Message}");
            }

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
                using (var conn = DbHelper.GetConnectionCloud())
                {
                    conn.Open();
                    DbHelper.EnsureTableExists(conn); // Cek & buat tabel kalau belum ada

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


        public List<ServiceEntry> OriginalServices = new List<ServiceEntry>(); // Ini buat data asli
        public List<ServiceEntry> AllServices = new List<ServiceEntry>(); // Ini buat data aktif/tampil
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

            dgServices.ItemsSource = null;
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
            try
            {
                DbHelper.InitializeDatabase();
                using var conn = DbHelper.GetConnection();
                conn.Open();
                var tableExists = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Services'");
                var services = conn.Query<ServiceEntry>("SELECT * FROM Services ORDER BY LastUpdated DESC").ToList();

                // Reset filter
                SearchBox.Text = "";
                FilterServices("");

                if (tableExists == 0)
                {
                    MessageBox.Show("Services table doesn't exist. Creating it now...");
                    DbHelper.InitializeDatabase();
                    return;
                }

                // Get list of available columns for debugging
                var availableColumns = conn.Query<string>(
                    "SELECT name FROM pragma_table_info('Services')").ToList();

                Debug.WriteLine("Available columns: " + string.Join(", ", availableColumns));

                // Build dynamic query based on available columns
                var queryBuilder = new StringBuilder("SELECT ");
                queryBuilder.Append(string.Join(", ", availableColumns));
                queryBuilder.Append(" FROM Services ORDER BY LastUpdated DESC");

                // Assign row numbers and handle null values
                for (int i = 0; i < services.Count; i++)
                {
                    var service = services[i];
                    service.RowNumber = i + 1;

                    // Ensure required fields have defaults
                    service.CustomerName ??= string.Empty;
                    service.Item ??= string.Empty;

                    // Handle potential missing columns
                    if (!availableColumns.Contains("WarrantyStatus")) service.WarrantyStatus = string.Empty;
                    if (!availableColumns.Contains("Status")) service.Status = string.Empty;
                    if (!availableColumns.Contains("SerialNumber")) service.SerialNumber = string.Empty;
                }

                OriginalServices = LoadAllServicesFromDatabase(); // atau apapun cara lo load
                AllServices = new List<ServiceEntry>(OriginalServices); // clone data
                dgServices.ItemsSource = AllServices;
                localLastUpdated = AllServices.Max(s => s.LastUpdated);

                // Cloud sync check with separate error handling
                //CheckCloudSync();

                dgServices.ItemsSource = AllServices;
                FilterServices("");
            }
            catch (SQLiteException sqlEx)
            {
                HandleDatabaseError(sqlEx);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading services: {ex.Message}");
                Debug.WriteLine($"ERROR DETAILS: {ex}");
            }
        }

        private void CheckCloudSync()
        {
            try
            {
                var cloudLastUpdated = CheckLastUpdatedCloud();
                if (CompareLastUpdated(localLastUpdated, cloudLastUpdated))
                {
                    var result = MessageBox.Show("Data di Cloud lebih baru. Apakah Anda ingin melakukan sinkronisasi sekarang?",
                        "Sinkronisasi Data", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        SyncToLocal();
                        // Refresh after sync
                        LoadServices();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memeriksa pembaruan cloud: {ex.Message}");
            }
        }

        private void HandleDatabaseError(SQLiteException ex)
        {
            if (ex.Message.Contains("no such table"))
            {
                MessageBox.Show("Database table missing. Attempting to recreate...");
                DbHelper.InitializeDatabase();
                LoadServices(); // Retry
            }
            else if (ex.Message.Contains("no such column"))
            {
                MessageBox.Show("Database schema outdated. Attempting to update...");
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    DbHelper.AddMissingColumns(conn); // Pass the required 'conn' parameter
                }
                LoadServices(); // Retry
            }
            else
            {
                MessageBox.Show($"Database error: {ex.Message}");
            }
        }

        private void FilterServices(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                dgServices.ItemsSource = AllServices;
                return;
            }

            var filtered = AllServices
                .Where(x => (x.CustomerName != null && x.CustomerName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                         || (x.Item != null && x.Item.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                         || (x.Status != null && x.Status.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                         || (x.SerialNumber != null && x.SerialNumber.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
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
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Services");

                // Header with all columns
                worksheet.Cell(1, 1).Value = "Customer Name";
                worksheet.Cell(1, 2).Value = "Item";
                worksheet.Cell(1, 3).Value = "Serial Number";
                worksheet.Cell(1, 4).Value = "CN/PN";
                worksheet.Cell(1, 5).Value = "Warranty Status";
                worksheet.Cell(1, 6).Value = "Problem";
                worksheet.Cell(1, 7).Value = "Kerusakan H/S";
                worksheet.Cell(1, 8).Value = "Status";
                worksheet.Cell(1, 9).Value = "Status Unit Berada";
                worksheet.Cell(1, 10).Value = "Date In";
                worksheet.Cell(1, 11).Value = "Service Date";
                worksheet.Cell(1, 12).Value = "Date Out";
                worksheet.Cell(1, 13).Value = "Service Location";
                worksheet.Cell(1, 14).Value = "Alamat Pengiriman";
                worksheet.Cell(1, 15).Value = "Accessories";
                worksheet.Cell(1, 16).Value = "Catatan Tambahan";
                worksheet.Cell(1, 17).Value = "Last Updated";

                using var conn = DbHelper.GetConnection();
                var services = conn.Query<ServiceEntry>("SELECT * FROM Services").ToList();

                int row = 2;
                foreach (var s in services)
                {
                    worksheet.Cell(row, 1).Value = s.CustomerName;
                    worksheet.Cell(row, 2).Value = s.Item;
                    worksheet.Cell(row, 3).Value = s.SerialNumber;
                    worksheet.Cell(row, 4).Value = s.CnPn;
                    worksheet.Cell(row, 5).Value = s.WarrantyStatus;
                    worksheet.Cell(row, 6).Value = s.Problem;
                    worksheet.Cell(row, 7).Value = s.HardwareSoftwareProblem;
                    worksheet.Cell(row, 8).Value = s.Status;
                    worksheet.Cell(row, 9).Value = s.UnitLocationStatus;
                    worksheet.Cell(row, 10).Value = s.DateIn?.ToString("dd-MM-yyyy");
                    worksheet.Cell(row, 11).Value = s.ServiceDate?.ToString("dd-MM-yyyy");
                    worksheet.Cell(row, 12).Value = s.DateOut?.ToString("dd-MM-yyyy");
                    worksheet.Cell(row, 13).Value = s.ServiceLocation;
                    worksheet.Cell(row, 14).Value = s.ShippingAddress;
                    worksheet.Cell(row, 15).Value = s.Accessories;
                    worksheet.Cell(row, 16).Value = s.AdditionalNotes;
                    worksheet.Cell(row, 17).Value = s.LastUpdated?.ToString("dd-MM-yyyy HH:mm:ss");
                    row++;
                }

                // Format date columns
                for (int i = 10; i <= 12; i++)
                {
                    worksheet.Column(i).Style.NumberFormat.Format = "dd-MM-yyyy";
                }
                worksheet.Column(17).Style.NumberFormat.Format = "dd-MM-yyyy HH:mm:ss";

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(saveFileDialog.FileName);
                MessageBox.Show("Data berhasil diexport ke Excel!");
            }
        }

        private string SafeGetString(IXLCell cell)
        {
            return cell?.GetString() ?? string.Empty;
        }
        private async void ImportData_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var imported = new List<ServiceEntry>();
                    int duplicateCount = 0;
                    int emptyDateCount = 0;
                    int newCount = 0;

                    using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RowsUsed().Skip(1).ToList();
                    int totalRows = rows.Count;
                    int currentRow = 0;

                    progressBar.Visibility = Visibility.Visible;
                    progressBar.Value = 0;
                    progressText.Text = "Progress: 0%";

                    await Task.Run(() =>
                    {
                        foreach (var row in rows)
                        {
                            var entry = new ServiceEntry
                            {
                                CustomerName = SafeGetString(row.Cell(1)),
                                Item = SafeGetString(row.Cell(2)),
                                SerialNumber = SafeGetString(row.Cell(3)),
                                CnPn = SafeGetString(row.Cell(4)),
                                WarrantyStatus = SafeGetString(row.Cell(5)),
                                Problem = SafeGetString(row.Cell(6)),
                                HardwareSoftwareProblem = SafeGetString(row.Cell(7)),
                                Status = SafeGetString(row.Cell(8)),
                                UnitLocationStatus = SafeGetString(row.Cell(9)),
                                DateIn = ParseDateWithMultipleFormats(row.Cell(10)),
                                ServiceDate = ParseDateWithMultipleFormats(row.Cell(11)),
                                DateOut = ParseDateWithMultipleFormats(row.Cell(12)),
                                ServiceLocation = SafeGetString(row.Cell(13)),
                                ShippingAddress = SafeGetString(row.Cell(14)),
                                Accessories = SafeGetString(row.Cell(15)),
                                AdditionalNotes = SafeGetString(row.Cell(16)),
                                LastUpdated = DateTime.Now
                            };

                            if (entry.DateIn == null) emptyDateCount++;
                            if (entry.ServiceDate == null) emptyDateCount++;
                            if (entry.DateOut == null) emptyDateCount++;

                            imported.Add(entry);

                            currentRow++;
                            Dispatcher.Invoke(() =>
                            {
                                double percent = (currentRow / (double)totalRows) * 100;
                                progressBar.Value = percent;
                                progressText.Text = $"Progress: {Math.Round(percent)}%";
                            });
                        }
                    });

                    using var conn = DbHelper.GetConnection();
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        foreach (var service in imported)
                        {
                            try
                            {
                                var existing = conn.QueryFirstOrDefault<int>(
                                    "SELECT COUNT(*) FROM Services WHERE SerialNumber = @SerialNumber",
                                    new { service.SerialNumber },
                                    transaction: transaction);

                                if (existing == 0)
                                {
                                    newCount++;
                                    conn.Execute(@"INSERT INTO Services 
                                (CustomerName, Item, SerialNumber, CnPn, WarrantyStatus, Problem, 
                                HardwareSoftwareProblem, Status, UnitLocationStatus, DateIn, ServiceDate, 
                                DateOut, ServiceLocation, ShippingAddress, Accessories, AdditionalNotes, LastUpdated)
                                VALUES 
                                (@CustomerName, @Item, @SerialNumber, @CnPn, @WarrantyStatus, @Problem, 
                                @HardwareSoftwareProblem, @Status, @UnitLocationStatus, @DateIn, @ServiceDate, 
                                @DateOut, @ServiceLocation, @ShippingAddress, @Accessories, @AdditionalNotes, @LastUpdated)",
                                        service, transaction: transaction);
                                }
                                else
                                {
                                    duplicateCount++;
                                    conn.Execute(@"UPDATE Services SET
                                CustomerName = @CustomerName,
                                Item = @Item,
                                CnPn = @CnPn,
                                WarrantyStatus = @WarrantyStatus,
                                Problem = @Problem,
                                HardwareSoftwareProblem = @HardwareSoftwareProblem,
                                Status = @Status,
                                UnitLocationStatus = @UnitLocationStatus,
                                DateIn = @DateIn,
                                ServiceDate = @ServiceDate,
                                DateOut = @DateOut,
                                ServiceLocation = @ServiceLocation,
                                ShippingAddress = @ShippingAddress,
                                Accessories = @Accessories,
                                AdditionalNotes = @AdditionalNotes,
                                LastUpdated = @LastUpdated
                                WHERE SerialNumber = @SerialNumber",
                                        service, transaction: transaction);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error processing row: {ex.Message}");
                            }
                        }
                        transaction.Commit();
                    }

                    // Detailed report
                    string report = $"Total data diproses: {imported.Count}\n" +
                                  $"Data baru ditambahkan: {newCount}\n" +
                                  $"Data diupdate: {duplicateCount}\n" +
                                  $"Kolom tanggal kosong: {emptyDateCount}";

                    MessageBox.Show($"Import selesai!\n\n{report}", "Laporan Import",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadServices();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal import: " + ex.Message);
                }
                finally
                {
                    progressBar.Visibility = Visibility.Collapsed;
                    progressBar.Value = 0;
                    progressText.Text = "Progress: 0%";
                }
            }
        }

        private DateTime? ParseDateWithMultipleFormats(IXLCell cell)
        {
            if (cell == null || cell.IsEmpty()) return null;

            string[] supportedFormats = new[]
            {
        "dd-MM-yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "dd-MM-yyyy",
        "dd/MM/yyyy",
        "MM/dd/yyyy HH:mm:ss", // Fallback untuk format US
        "MM-dd-yyyy HH:mm:ss"  // Fallback lainnya
    };

            // Coba parse sebagai DateTime langsung
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();

            // Coba parse sebagai string dengan berbagai format
            if (DateTime.TryParseExact(cell.GetString(),
                                     supportedFormats,
                                     CultureInfo.InvariantCulture,
                                     DateTimeStyles.None,
                                     out var date))
                return date;

            // Coba parse numeric (Excel date value)
            if (cell.DataType == XLDataType.Number)
                return DateTime.FromOADate(cell.GetDouble());

            return null;
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
        private bool CompareLastUpdated(DateTime? local, DateTime? cloud)
        {
            if (cloud == null) return false;
            if (local == null) return true;

            return cloud > local;
        }

        private void SyncToLocal()
        {
            try
            {
                using var cloudConn = DbHelper.GetConnectionCloud();
                using var localConn = DbHelper.GetConnection();
                cloudConn.Open();
                localConn.Open();

                var cloudData = cloudConn.Query<ServiceEntry>("SELECT * FROM Services").ToList();

                foreach (var service in cloudData)
                {
                    var existing = localConn.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM Services WHERE Id = @Id", new { service.Id });

                    if (existing == 0)
                    {
                        localConn.Execute(@"INSERT INTO Services 
                            (Id, RowNumber, CustomerName, Item, SerialNumber, WarrantyStatus, Problem, Status,
                             DateIn, ServiceDate, DateOut, ServiceLocation, Accessories, LastUpdated)
                            VALUES 
                            (@Id, @RowNumber, @CustomerName, @Item, @SerialNumber, @WarrantyStatus, @Problem, @Status,
                             @DateIn, @ServiceDate, @DateOut, @ServiceLocation, @Accessories, @LastUpdated)", service);
                    }
                    else
                    {
                        localConn.Execute(@"UPDATE Services SET
                            RowNumber = @RowNumber,
                            CustomerName = @CustomerName,
                            Item = @Item,
                            SerialNumber = @SerialNumber,
                            WarrantyStatus = @WarrantyStatus,
                            Problem = @Problem,
                            Status = @Status,
                            DateIn = @DateIn,
                            ServiceDate = @ServiceDate,
                            DateOut = @DateOut,
                            ServiceLocation = @ServiceLocation,
                            Accessories = @Accessories,
                            LastUpdated = @LastUpdated
                            WHERE Id = @Id", service);
                    }
                }

                LoadServices();
                MessageBox.Show("Sinkronisasi dari Cloud berhasil!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal sinkronisasi: " + ex.Message);
            }
        }

        private void dgServices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void UploadDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CloudSync.UploadModifiedRowsToCloud();
                MessageBox.Show("Upload ke cloud berhasil.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal upload database ke cloud: " + ex.Message);
            }
        }

        private void DownloadDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CloudSync.DownloadCloudDatabase();
                LoadServices();
                MessageBox.Show("Download dari cloud berhasil.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal download database dari cloud: " + ex.Message);
            }
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            var settingWindow = new SettingWindow();
            settingWindow.Owner = this;
            settingWindow.ShowDialog();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterWindow filterWindow = new FilterWindow(OriginalServices.ToList()); // <= ganti ini bro
            if (filterWindow.ShowDialog() == true)
            {
                var filtered = OriginalServices.Where(s => // <= ini juga dari OriginalServices
                    (filterWindow.SelectedCustomers.Count == 0 || filterWindow.SelectedCustomers.Contains(s.CustomerName)) &&
                    (filterWindow.SelectedItems.Count == 0 || filterWindow.SelectedItems.Contains(s.Item)) &&
                    (filterWindow.SelectedSerialNumbers.Count == 0 || filterWindow.SelectedSerialNumbers.Contains(s.SerialNumber)) &&
                    (filterWindow.SelectedWarranty.Count == 0 || filterWindow.SelectedWarranty.Contains(s.WarrantyStatus)) &&
                    (filterWindow.SelectedStatus.Count == 0 || filterWindow.SelectedStatus.Contains(s.Status)) &&
                    (filterWindow.SelectedLocations.Count == 0 || filterWindow.SelectedLocations.Contains(s.ServiceLocation))
                ).ToList();

                AllServices = filtered;
                dgServices.ItemsSource = AllServices;
            }
        }

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ResetDataGrid();
        }

        private void ResetDataGrid()
        {
            AllServices = new List<ServiceEntry>(OriginalServices); // Copy ulang dari original
            dgServices.ItemsSource = AllServices;
        }


        private void dgServices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgServices.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed)
                dgServices.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
            else
                dgServices.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
        }
        private List<ServiceEntry> LoadAllServicesFromDatabase()
        {
            try
            {
                using var conn = DbHelper.GetConnection();
                conn.Open();
                var services = conn.Query<ServiceEntry>("SELECT * FROM Services ORDER BY LastUpdated DESC").ToList();

                // Tambahin inisialisasi RowNumber di sini!
                for (int i = 0; i < services.Count; i++)
                {
                    services[i].RowNumber = i + 1;
                }

                return services;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading services from database: {ex.Message}");
                return new List<ServiceEntry>();
            }
        }

    }


    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null; // Return true if not null
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
