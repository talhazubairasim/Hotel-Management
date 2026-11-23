using Assignment.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Services;

public class SeedUser
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

/// <summary>Creates roles, the configured staff accounts and (optionally) demo hotels, rooms and guests.</summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Admin, Roles.Receptionist })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        var users = services.GetRequiredService<UserManager<AppUser>>();
        await EnsureUserAsync(users, config.GetSection("Seed:Admin").Get<SeedUser>(), Roles.Admin);
        await EnsureUserAsync(users, config.GetSection("Seed:Receptionist").Get<SeedUser>(), Roles.Receptionist);

        if (config.GetValue("Seed:DemoData", false))
            await SeedDemoDataAsync(services.GetRequiredService<HotelManagementContext>());
    }

    private static async Task EnsureUserAsync(UserManager<AppUser> users, SeedUser? seed, string role)
    {
        if (seed == null || string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password)) return;
        AppUser? user = await users.FindByEmailAsync(seed.Email);
        if (user == null)
        {
            user = new AppUser { UserName = seed.Email, Email = seed.Email, EmailConfirmed = true, FullName = seed.FullName };
            var result = await users.CreateAsync(user, seed.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not create {role} account: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        if (!await users.IsInRoleAsync(user, role))
            await users.AddToRoleAsync(user, role);
    }

    /// <summary>A small demo dataset so the app is usable straight away.</summary>
    public static async Task SeedDemoDataAsync(HotelManagementContext db)
    {
        if (await db.Hotels.AnyAsync()) return;

        var hotel = new Hotel
        {
            HotelId = Guid.NewGuid(), Name = "Grand Plaza", Address = "Blue Area, Islamabad",
            Rating = "5", TotalFloors = 3, TotalRooms = 9, RoomsPerFloor = 3, IsActive = true,
        };
        var hotel2 = new Hotel
        {
            HotelId = Guid.NewGuid(), Name = "Lakeside Inn", Address = "Rawal Lake Road, Islamabad",
            Rating = "3", TotalFloors = 2, TotalRooms = 4, RoomsPerFloor = 2, IsActive = true,
        };
        db.Hotels.AddRange(hotel, hotel2);

        string[] types = { "Standard", "Deluxe", "Luxury" };
        double[] rates = { 80, 140, 260 };
        for (int floor = 1; floor <= 3; floor++)
            for (int i = 0; i < 3; i++)
                db.Rooms.Add(new Room
                {
                    RoomId = Guid.NewGuid(), HotelId = hotel.HotelId, Name = $"{floor}0{i + 1}", Floor = floor,
                    Type = types[i], Charges = rates[i], IsActive = true,
                });
        for (int floor = 1; floor <= 2; floor++)
            for (int i = 0; i < 2; i++)
                db.Rooms.Add(new Room
                {
                    RoomId = Guid.NewGuid(), HotelId = hotel2.HotelId, Name = $"L{floor}{i + 1}", Floor = floor,
                    Type = types[i], Charges = rates[i] * 0.7, IsActive = true,
                });

        db.Customers.AddRange(
            new Customer { CustomerId = Guid.NewGuid(), FullName = "Ayesha Khan", Email = "ayesha@example.com", Phone = "+92 300 1234567" },
            new Customer { CustomerId = Guid.NewGuid(), FullName = "John Smith", Email = "john@example.com", Phone = "+1 555 0100" });

        await db.SaveChangesAsync();
    }
}
