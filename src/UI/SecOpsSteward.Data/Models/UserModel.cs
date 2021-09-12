using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SecOpsSteward.Data.Models
{
    public class UserModel
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        public string Username { get; set; }

        public string DisplayName { get; set; }

        public bool IsAdmin { get; set; }

        public ICollection<AgentPermissionModel> Permissions { get; set; }
        public ICollection<AgentGrantModel> AgentPackageGrants { get; set; }
    }
}
