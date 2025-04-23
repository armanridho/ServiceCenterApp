using System;

namespace ServiceCenterApp.Models
{
    public class ServiceEntry
    {
        public int Id { get; set; }
        public int RowNumber { get; set; } // untuk No.
        public string CustomerName { get; set; }
        public string Item { get; set; } // jadi "Unit"
        public string SerialNumber { get; set; } // kolom baru
        public string Problem { get; set; }
        public string Status { get; set; }
        public DateTime? DateIn { get; set; }
        public DateTime? ServiceDate { get; set; }
        public DateTime? DateOut { get; set; }
        public string ServiceLocation { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? localLastUpdate { get; set; }
        public string WarrantyStatus { get; set; }
        public string Accessories { get; set; }

    }


}
