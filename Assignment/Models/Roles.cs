namespace Assignment.Models;

/// <summary>Application roles. Admins manage hotels, rooms and approvals; receptionists run the front desk.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string AdminOrReceptionist = Admin + "," + Receptionist;
}
