using ProductManagement.Application.Services;
using ProductManagement.Domain.Models;
using ProductManagement.Infrastructure.Data;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;

using System.Transactions;
namespace ProductManagement.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly ProductService _productService;
        private readonly ApplicationDbContext _context;
        private readonly TransactionScope _transaction;
        public ProductServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseMySql("Server=localhost;Database=ProductManagementDb;User=root;Password=123456;",
              new MySqlServerVersion(new Version(8, 0, 40))) // MySQL versiyonunu uygun şekilde belirtin
          .Options;

            _context = new ApplicationDbContext(options);
            _productService = new ProductService(_context);
            _transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        }
        // public ProductServiceTests()
        // {
        //     var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        //         .UseInMemoryDatabase(databaseName: "TestDatabase")
        //         .Options;
        //     _context = new ApplicationDbContext(options);
        //     _productService = new ProductService(_context);
        //     _context.Products.RemoveRange(_context.Products); // Verileri temizler
        //     _context.SaveChanges();

        //     _context.Products.AddRange(
        //         new Product { Name = "Product 1", Price = 100 },
        //         new Product { Name = "Product 2", Price = 200 }
        //     );
        //     _context.SaveChanges();
        // }
        [Fact]
        public void UpdateProduct_ShouldUpdateProduct()
        {
            var product = new Product { Id = 2, Name = "Updated Product", Price = 500 };
            _productService.UpdateProduct(product);
            var result = _productService.GetProductById(2);
            Assert.Equal("Updated Product", result.Name); // Güncellenecek ürün kontrolü
        }

        [Fact]
        public void GetAllProducts_ShouldReturnAllProducts()
        {
            var result = _productService.GetAllProducts();
            // Assert.Equal(8, result.Count); // Beklenen ürün sayısı
            Assert.Equal("1", result[0].Name); // İlk ürün kontrolü
            Assert.Equal("1", result[1].Name); // İkinci ürün kontrolü
        }

        [Fact]
        public void GetProductById_ShouldReturnProduct()
        {
            var result = _productService.GetProductById(2);
            Assert.Equal("1", result.Name); // Beklenen ürün kontrolü
        }

        [Fact]
        public void AddProduct_ShouldAddProduct()
        {
            var product = new Product { Name = "Product 3", Price = 300 };
            _productService.AddProduct(product);
            var result = _productService.GetProductById(product.Id);
            Assert.Equal("Product 3", result.Name); // Eklenecek ürün kontrolü
        }



        [Fact]
        public void DeleteProduct_ShouldDeleteProduct()
        {
            _productService.DeleteProduct(1);
            var result = _productService.GetProductById(1);
            Assert.Null(result); // Silinecek ürün kontrolü
        }

        [Fact]
        public void GetProductById_ShouldReturnNull_WhenProductNotFound()
        {
            var result = _productService.GetProductById(999); // Var olmayan bir ID

            // Assert
            Assert.Null(result); // Sonuç null olmalı
        }
        [Fact]
        public void GetAllProducts_ShouldReturnInLessThan1Second()
        {
            // Act
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var result = _productService.GetAllProducts();
            watch.Stop();

            // Assert
            Assert.True(watch.ElapsedMilliseconds < 1000); // 1 saniyeden az olmalı
        }
        [Theory]
        [InlineData("Product A")]
        [InlineData("Product B")]
        public void AddProduct_ShouldAddProduct_WhenNameIsValid(string productName)
        {
            var product = new Product { Name = productName, Price = 300 };
            _productService.AddProduct(product);
            var result = _productService.GetProductById(product.Id);
            Assert.Equal(productName, result.Name); // Eklenecek ürün kontrolü
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
    }


}
