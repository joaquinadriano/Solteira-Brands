using System;

namespace Solteira_Brands.Models
{
    public class tblActivityLogModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }
        public string Action { get; set; }
        public string Module { get; set; }
        public int? EntityId { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
