using handyCookiesShop.models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace handyCookiesShop
{
    internal class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().HasData(
                 new Customer
                 {
                     Id = 1,
                     UserName = "cookie_lover",
                     Email = "user1@mail.com",
                     FirstName = "John",
                     LastName = "Doe",
                     Address = "Sweet St 12"
                 },
                 new Customer
                 {
                     Id = 2,
                     UserName = "baker_fan",
                     Email = "user2@mail.com",
                     FirstName = "Alice",
                     LastName = "Smith",
                     Address = "Sugar Ave 5"
                 }
             );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Chocolate Chip",
                    Description = "Classic cookie with chocolate chips.",
                    Price = 2.50m,
                    StockQuantity = 20,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 2,
                    Name = "Oatmeal Raisin",
                    Description = "Oatmeal cookie with juicy raisins.",
                    Price = 2.00m,
                    StockQuantity = 25,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 3,
                    Name = "Peanut Butter",
                    Description = "Rich peanut butter flavor cookie.",
                    Price = 2.20m,
                    StockQuantity = 30,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
