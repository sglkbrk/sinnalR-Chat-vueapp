using ProductManagement.Application.Services;
using ProductManagement.Domain.Models;
using ProductManagement.Infrastructure.Data;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ProductManagement.Tests.Services
{
    public class ProductServiceTests
    {
        private ProductService _productService;
        private ApplicationDbContext _context;
        // public ProductServiceTests()
        // {

        //     var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        //         .UseInMemoryDatabase(databaseName: "TestDatabase")
        //         .Options;

        //     var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        //     .UseSqlServer("YourConnectionStringHere") // Gerçek veritabanı bağlantısı
        //     .Options;

        //     _context = new ApplicationDbContext(options);
        //     _productService = new ProductService(_context);
        // }
        public void UseInMemoryDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

            _context = new ApplicationDbContext(options);
            _productService = new ProductService(_context);
        }

        public void UseMySqlDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySql("Server=localhost;Database=ProductManagementDb;User=root;Password=123456;",
                    new MySqlServerVersion(new Version(8, 0, 40))) // MySQL versiyonunu uygun şekilde belirtin
                .Options;

            _context = new ApplicationDbContext(options);
            _productService = new ProductService(_context);
        }
        // private void InitializeDatabase()
        // {
        //     _context.Products.RemoveRange(_context.Products); // Verileri temizler
        //     _context.SaveChanges();

        //     _context.Products.AddRange(
        //         new Product { Id = 1, Name = "Product 1", Price = 100 },
        //         new Product { Id = 2, Name = "Product 2", Price = 200 }
        //     );
        //     _context.SaveChanges();
        // }

        private void SeedDatabase()
        {
            if (_context.Products.Any())
            {
                return; // Veritabanı zaten doldurulmuş
            }

            _context.Products.AddRange(
                new Product { Id = 1, Name = "Product 1" },
                new Product { Id = 2, Name = "Product 2" }
            );

            _context.SaveChanges();
        }
        public void Dispose()
        {
            _context?.Database.EnsureDeleted(); // Testlerden sonra veritabanını sil
            _context?.Dispose();
        }
        [Fact]
        public void GetAllProducts_ShouldReturnAllProducts()
        {
            UseInMemoryDatabase();
            SeedDatabase();
            var result = _productService.GetAllProducts();
            Assert.Equal(2, result.Count); // Beklenen ürün sayısı
            Assert.Equal("Product 1", result[0].Name); // İlk ürün kontrolü
            Assert.Equal("Product 2", result[1].Name); // İkinci ürün kontrolü
        }

        [Fact]
        public void GetProductById_ShouldReturnProduct()
        {
            UseInMemoryDatabase();
            SeedDatabase();
            var result = _productService.GetProductById(1);
            Assert.Equal("Product 1", result.Name); // Beklenen ürün kontrolü
        }

        [Fact]
        public void AddProduct_ShouldAddProduct()
        {
            UseInMemoryDatabase();
            var product = new Product { Id = 3, Name = "Product 3", Price = 300 };
            _productService.AddProduct(product);
            var result = _productService.GetProductById(3);
            Assert.Equal("Product 3", result.Name); // Eklenecek ürün kontrolü
        }

        [Fact]
        public void UpdateProduct_ShouldUpdateProduct()
        {
            UseInMemoryDatabase();
            var product = new Product { Id = 1, Name = "Updated Product", Price = 500 };
            _productService.UpdateProduct(product);
            var result = _productService.GetProductById(1);
            Assert.Equal("Updated Product", result.Name); // Güncellenecek ürün kontrolü
        }

        [Fact]
        public void DeleteProduct_ShouldDeleteProduct()
        {
            UseInMemoryDatabase();
            _productService.DeleteProduct(1);
            var result = _productService.GetProductById(1);
            Assert.Null(result); // Silinecek ürün kontrolü
        }

        [Fact]
        public void GetProductById_ShouldReturnNull_WhenProductNotFound()
        {
            UseInMemoryDatabase();
            var result = _productService.GetProductById(999); // Var olmayan bir ID

            // Assert
            Assert.Null(result); // Sonuç null olmalı
        }
        [Fact]
        public void GetAllProducts_ShouldReturnInLessThan1Second()
        {
            UseInMemoryDatabase();
            // Act
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var result = _productService.GetAllProducts();
            watch.Stop();

            // Assert
            Assert.True(watch.ElapsedMilliseconds < 1000); // 1 saniyeden az olmalı
        }
        [Theory]
        [InlineData(4, "Product A")]
        [InlineData(5, "Product B")]
        public void AddProduct_ShouldAddProduct_WhenNameIsValid(int id, string productName)
        {
            UseInMemoryDatabase();
            var product = new Product { Id = id, Name = productName, Price = 300 };
            _productService.AddProduct(product);
            var result = _productService.GetProductById(product.Id);
            Assert.Equal(productName, result.Name); // Eklenecek ürün kontrolü
        }


    }

}
