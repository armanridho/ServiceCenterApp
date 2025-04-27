using System;
using System.Windows;
using Dapper;
using System.Data.SQLite;
using ServiceCenterApp.Models;
using System.Linq;
using System.Windows.Controls;
using System.Collections.Generic;

namespace ServiceCenterApp.Views
{
    public partial class AddService : Window
    {
        private ServiceEntry _editEntry;
        private bool _isEditMode = false;

        public AddService()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        public AddService(ServiceEntry entry) : this()
        {
            _editEntry = entry;
            _isEditMode = true;
            Title = "Edit Data Servis";

            if (_editEntry != null)
            {
                cmbCustomer.Text = _editEntry.CustomerName;
                cmbUnit.Text = _editEntry.Item;
                txtSerialNumber.Text = _editEntry.SerialNumber;
                txtCnPn.Text = _editEntry.CnPn;
                cmbWarranty.Text = _editEntry.WarrantyStatus;
                txtProblem.Text = _editEntry.Problem;
                txtHardwareSoftwareProblem.Text = _editEntry.HardwareSoftwareProblem;
                cmbStatus.Text = _editEntry.Status;
                cmbUnitLocationStatus.Text = _editEntry.UnitLocationStatus;
                cmbLocation.Text = _editEntry.ServiceLocation;
                txtShippingAddress.Text = _editEntry.ShippingAddress;
                txtAccessories.Text = _editEntry.Accessories;
                txtAdditionalNotes.Text = _editEntry.AdditionalNotes;

                if (_editEntry.DateIn.HasValue) dpDateIn.SelectedDate = _editEntry.DateIn.Value;
                if (_editEntry.ServiceDate.HasValue) dpServiceDate.SelectedDate = _editEntry.ServiceDate.Value;
                if (_editEntry.DateOut.HasValue) dpDateOut.SelectedDate = _editEntry.DateOut.Value;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var conn = DbHelper.GetConnection();
                conn.Open();

                // Load customer names
                var customers = conn.Query<string>("SELECT DISTINCT CustomerName FROM Services WHERE CustomerName IS NOT NULL ORDER BY CustomerName").ToList();
                cmbCustomer.ItemsSource = customers;

                // Load items
                var units = conn.Query<string>("SELECT DISTINCT Item FROM Services WHERE Item IS NOT NULL ORDER BY Item").ToList();
                cmbUnit.ItemsSource = units;

                // Load service locations
                var locations = conn.Query<string>("SELECT DISTINCT ServiceLocation FROM Services WHERE ServiceLocation IS NOT NULL ORDER BY ServiceLocation").ToList();
                cmbLocation.ItemsSource = locations;

                // Load warranty statuses (with default values)
                var warrantyStatuses = new List<string> { "Ya", "Tidak", "Expired", "Masih Garansi" };
                var dbWarrantyStatuses = conn.Query<string>("SELECT DISTINCT WarrantyStatus FROM Services WHERE WarrantyStatus IS NOT NULL").ToList();
                warrantyStatuses.AddRange(dbWarrantyStatuses.Except(warrantyStatuses));
                cmbWarranty.ItemsSource = warrantyStatuses;

                // Load service statuses (with default values)
                var statuses = new List<string> { "Masuk", "Service", "Keluar", "Pending", "Cancel" };
                var dbStatuses = conn.Query<string>("SELECT DISTINCT Status FROM Services WHERE Status IS NOT NULL").ToList();
                statuses.AddRange(dbStatuses.Except(statuses));
                cmbStatus.ItemsSource = statuses;

                // Load unit location statuses
                var locationStatuses = new List<string> { "Di Tempat", "Di Bawa Customer", "Di Gudang", "Di Service Center" };
                var dbLocationStatuses = conn.Query<string>("SELECT DISTINCT UnitLocationStatus FROM Services WHERE UnitLocationStatus IS NOT NULL").ToList();
                locationStatuses.AddRange(dbLocationStatuses.Except(locationStatuses));
                cmbUnitLocationStatus.ItemsSource = locationStatuses;

                // Set default values if not in edit mode
                if (!_isEditMode)
                {
                    cmbStatus.SelectedIndex = 0;
                    cmbWarranty.SelectedIndex = 0;
                    cmbUnitLocationStatus.SelectedIndex = 0;
                    dpDateIn.SelectedDate = DateTime.Today;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(cmbCustomer.Text))
                {
                    MessageBox.Show("Nama Customer harus diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCustomer.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cmbUnit.Text))
                {
                    MessageBox.Show("Unit harus dipilih!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbUnit.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtProblem.Text))
                {
                    MessageBox.Show("Kerusakan harus diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtProblem.Focus();
                    return;
                }

                if (cmbStatus.SelectedItem == null)
                {
                    MessageBox.Show("Status Servis harus dipilih!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbStatus.Focus();
                    return;
                }

                // Create entry
                var entry = new ServiceEntry
                {
                    CustomerName = cmbCustomer.Text,
                    Item = cmbUnit.Text,
                    SerialNumber = txtSerialNumber.Text,
                    CnPn = txtCnPn.Text,
                    WarrantyStatus = cmbWarranty.Text,
                    Problem = txtProblem.Text,
                    HardwareSoftwareProblem = txtHardwareSoftwareProblem.Text,
                    Status = cmbStatus.Text,
                    UnitLocationStatus = cmbUnitLocationStatus.Text,
                    ServiceLocation = cmbLocation.Text,
                    ShippingAddress = txtShippingAddress.Text,
                    Accessories = txtAccessories.Text,
                    AdditionalNotes = txtAdditionalNotes.Text,
                    LastUpdated = DateTime.Now
                };

                // Set dates based on status or date pickers
                entry.DateIn = dpDateIn.SelectedDate;
                entry.ServiceDate = dpServiceDate.SelectedDate;
                entry.DateOut = dpDateOut.SelectedDate;

                // If not in edit mode or dates not set, set them based on status
                if (!_isEditMode || entry.DateIn == null)
                {
                    switch (cmbStatus.Text)
                    {
                        case "Masuk":
                        case "Service":
                        case "Keluar":
                            entry.DateIn = DateTime.Now;
                            break;
                    }
                }

                if (!_isEditMode || entry.ServiceDate == null)
                {
                    if (cmbStatus.Text == "Service" || cmbStatus.Text == "Keluar")
                    {
                        entry.ServiceDate = DateTime.Now;
                    }
                }

                if (!_isEditMode || entry.DateOut == null)
                {
                    if (cmbStatus.Text == "Keluar")
                    {
                        entry.DateOut = DateTime.Now;
                    }
                }

                using var conn = DbHelper.GetConnection();
                conn.Open();

                if (_isEditMode)
                {
                    // EDIT mode
                    entry.Id = _editEntry.Id;

                    conn.Execute(@"
                        UPDATE Services SET
                            CustomerName = @CustomerName,
                            Item = @Item,
                            SerialNumber = @SerialNumber,
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
                        WHERE Id = @Id", entry);

                    MessageBox.Show("Data servis berhasil diperbarui!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // ADD mode
                    conn.Execute(@"
                        INSERT INTO Services 
                            (CustomerName, Item, SerialNumber, CnPn, WarrantyStatus, Problem, 
                             HardwareSoftwareProblem, Status, UnitLocationStatus, DateIn, ServiceDate, 
                             DateOut, ServiceLocation, ShippingAddress, Accessories, AdditionalNotes, LastUpdated) 
                        VALUES 
                            (@CustomerName, @Item, @SerialNumber, @CnPn, @WarrantyStatus, @Problem, 
                             @HardwareSoftwareProblem, @Status, @UnitLocationStatus, @DateIn, @ServiceDate, 
                             @DateOut, @ServiceLocation, @ShippingAddress, @Accessories, @AdditionalNotes, @LastUpdated)", entry);

                    MessageBox.Show("Data servis berhasil ditambahkan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}