using System;

namespace Solteira_Brands.Models
{
    public class tblMeetupSettingsModel
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
