using Microsoft.AspNetCore.Identity;

namespace ExchangeBroker.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string AspNetRoleId { get; set; }
    }
}
