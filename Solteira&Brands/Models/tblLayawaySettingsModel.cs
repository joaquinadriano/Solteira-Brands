using System;

namespace Solteira_Brands.Models
{
    public class tblLayawaySettingsModel
    {
        public int Id { get; set; }
        public decimal DownpaymentPercentage { get; set; }
        public int MonthsAllowed { get; set; }
        public int? ResubmitWindowDays { get; set; }
        public int? GracePeriodDays { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
