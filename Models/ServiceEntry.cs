using System;

namespace ServiceCenterApp.Models
{
    public class ServiceEntry
    {
        public int Id { get; set; }
        public int RowNumber { get; set; }
        public string CustomerName { get; set; }
        public string Item { get; set; }
        public string SerialNumber { get; set; }
        public string CnPn { get; set; } // New property
        public string WarrantyStatus { get; set; }
        public string Accessories { get; set; }
        public string Problem { get; set; }
        public string HardwareSoftwareProblem { get; set; } // New property
        public string Status { get; set; }
        public string UnitLocationStatus { get; set; } // New property
        public DateTime? DateIn { get; set; }
        public DateTime? ServiceDate { get; set; }
        public DateTime? DateOut { get; set; }
        public string ServiceLocation { get; set; }
        public string ShippingAddress { get; set; } // New property
        public string AdditionalNotes { get; set; } // New property
        public DateTime? LastUpdated { get; set; }
    }


}
