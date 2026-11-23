using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Models;

public partial class HotelManagementContext : IdentityDbContext<AppUser>
{
    public HotelManagementContext(DbContextOptions<HotelManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Hotel> Hotels { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    // The connection string comes from configuration (Web.config or appsettings.json); see Program.cs.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity tables

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Phone);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoice");
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.Property(e => e.InvoiceNumber).HasMaxLength(30);
            foreach (var p in new[] { nameof(Invoice.Nights), nameof(Invoice.RoomRate), nameof(Invoice.Subtotal), nameof(Invoice.Tax), nameof(Invoice.Total) })
                entity.Property(p).HasPrecision(18, 2);
            entity.Property(e => e.TaxRate).HasPrecision(5, 4);
            entity.HasOne(e => e.Reservation).WithOne(r => r.Invoice)
                .HasForeignKey<Invoice>(e => e.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Hotel>(entity =>
        {
            entity.HasKey(e => e.HotelId).HasName("PK__Hotel__46023BDF362F3D19");

            entity.ToTable("Hotel");

            entity.Property(e => e.HotelId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Address).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Rating).HasMaxLength(10);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId).HasName("PK__Reservat__B7EE5F2489CF5DDD");

            entity.ToTable("Reservation");

            entity.Property(e => e.ReservationId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.DecisionBy).HasMaxLength(256);
            entity.Property(e => e.DecisionNote).HasMaxLength(500);
            entity.HasIndex(e => new { e.RoomId, e.From, e.To });
            entity.HasIndex(e => e.Status);

            entity.HasOne(d => d.Customer).WithMany(c => c.Reservations)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Room).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservation_Room");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__32863939BD6A8BB0");

            entity.ToTable("Room");

            entity.Property(e => e.RoomId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(20);

            entity.HasOne(d => d.Hotel).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_Hotel");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
