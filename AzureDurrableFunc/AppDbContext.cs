using durrableShop.models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace durrableShop
{
    internal class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => new { oi.OrderId, oi.ProductId });

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany<OrderItem>()
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasMany<Order>()
                .WithOne(o => o.Customer)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .Property(c => c.PaymentMethods)
                .HasConversion(
                    v => string.Join(',', v.Select(e => e.ToString())),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(e => Enum.Parse<PaymentMethods>(e))
                          .ToList()
                );

            modelBuilder.Entity<Customer>().HasData(
                 new Customer
                 {
                     Id = 1,
                     UserName = "cookie_lover",
                     Email = "igor19benderuk@gmail.com",
                     FirstName = "John",
                     LastName = "Doe",
                     PaymentMethods = new List<PaymentMethods>() {PaymentMethods.MasterCard, PaymentMethods.Visa },
                     BankAccount = "some John number"

                 },
                 new Customer
                 {
                     Id = 2,
                     UserName = "baker_fan",
                     Email = "igor19benderuk@gmail.com",
                     FirstName = "Alice",
                     LastName = "Smith",
                     PaymentMethods = new List<PaymentMethods>() { PaymentMethods.UniversalBank, PaymentMethods.Visa },
                     BankAccount = "some Alice number"
                 }
             );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Chocolate Chip",
                    Description = "Classic cookie with chocolate chips.",
                    Price = 8,
                    StockQuantity = 20,
                    CreatedAt = DateTime.UtcNow,
                    PaymentMethod = PaymentMethods.MasterCard,
                    Weight = 0.5f
                },
                new Product
                {
                    Id = 2,
                    Name = "Oatmeal Raisin",
                    Description = "Oatmeal cookie with juicy raisins.",
                    Price = 3,
                    StockQuantity = 25,
                    CreatedAt = DateTime.UtcNow,
                    PaymentMethod = PaymentMethods.Visa,
                    Weight = 0.4f
                },
                new Product
                {
                    Id = 3,
                    Name = "Peanut Butter",
                    Description = "Rich peanut butter flavor cookie.",
                    Price = 5,
                    StockQuantity = 30,
                    CreatedAt = DateTime.UtcNow,
                    PaymentMethod = PaymentMethods.UniversalBank,
                    Weight = 0.2f
                }
            );
        }
    }
}
