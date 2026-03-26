using System;

namespace Solteira_Brands.Models
{
    public class tblVerificationTokenModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenValue { get; set; }
        public string TokenType { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual tblUsersModel User { get; set; }
    }
}
