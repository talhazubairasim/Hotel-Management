using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Models;

public partial class HotelManagementContext : DbContext
{
    public HotelManagementContext()
    {
    }

    public HotelManagementContext(DbContextOptions<HotelManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Hotel> Hotels { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=HotelManagementDb;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
