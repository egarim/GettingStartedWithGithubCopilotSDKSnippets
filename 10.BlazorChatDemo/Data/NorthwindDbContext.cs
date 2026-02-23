using BlazorChatDemo.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatDemo.Data;

public class NorthwindDbContext : DbContext
{
    public NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeTerritory> EmployeeTerritories => Set<EmployeeTerritory>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Shipper> Shippers => Set<Shipper>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Territory> Territories => Set<Territory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Region -> Territories
        modelBuilder.Entity<Region>()
            .HasMany(r => r.Territories)
            .WithOne(t => t.Region)
            .HasForeignKey(t => t.RegionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Employee self-referencing (ReportsTo)
        modelBuilder.Entity<Employee>()
            .HasMany(e => e.DirectReports)
            .WithOne(e => e.ReportsTo)
            .HasForeignKey(e => e.ReportsToId)
            .OnDelete(DeleteBehavior.SetNull);

        // EmployeeTerritory composite unique index
        modelBuilder.Entity<EmployeeTerritory>()
            .HasIndex(et => new { et.EmployeeId, et.TerritoryId })
            .IsUnique();

        modelBuilder.Entity<EmployeeTerritory>()
            .HasOne(et => et.Employee)
            .WithMany(e => e.Territories)
            .HasForeignKey(et => et.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmployeeTerritory>()
            .HasOne(et => et.Territory)
            .WithMany(t => t.EmployeeTerritories)
            .HasForeignKey(et => et.TerritoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Product -> Category, Supplier
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        // Order -> Customer, Employee, Shipper, Invoice
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Employee)
            .WithMany(e => e.Orders)
            .HasForeignKey(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Shipper)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.ShipperId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Invoice)
            .WithMany(i => i.Orders)
            .HasForeignKey(o => o.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Order -> OrderItems
        modelBuilder.Entity<Order>()
            .HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItem -> Product
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Invoice unique number
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();
    }
}
