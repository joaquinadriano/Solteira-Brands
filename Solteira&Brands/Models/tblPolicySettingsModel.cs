using System;

namespace Solteira_Brands.Models
{
    public class tblPolicySettingsModel
    {
        public int Id { get; set; }
        public string PolicyType { get; set; }
        public string Content { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
