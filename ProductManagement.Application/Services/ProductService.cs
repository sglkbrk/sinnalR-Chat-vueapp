using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Models;
using ProductManagement.Infrastructure.Data;

namespace ProductManagement.Application.Services
{
    public class ProductService
    {
        private readonly ApplicationDbContext _context;



        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Products> GetAllProducts()
        {
            return _context.Products.Include(p => p.Category).OrderBy(p => p.Id).ToList();
        }

        public Products GetProductById(int id)
        {
            return _context.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
        }

        public void AddProduct(Products product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
        }

        public void UpdateProduct(Products product)
        {
            _context.Products.Update(product);
            _context.SaveChanges();
        }

        public void DeleteProduct(int id)
        {
            var product = GetProductById(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
        }

    }
}
