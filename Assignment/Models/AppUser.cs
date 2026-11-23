using Microsoft.AspNetCore.Identity;

namespace Assignment.Models;

/// <summary>A staff member who signs in to the system.</summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
