using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
// using Microsoft.EntityFrameworkCore;

namespace ProductManagement.Application.Services
{
    public class CategoryService
    {
        private readonly ApplicationDbContext _context;
        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Category> GetAllCategories()
        {
            return _context.Category.ToList();
        }

        public Category? GetCategoryById(int id)
        {
            return _context.Category.FirstOrDefault(c => c.Id == id);
        }

        public void AddCategory(Category category)
        {
            _context.Category.Add(category);
            _context.SaveChanges();
        }

        public void UpdateCategory(Category category)
        {
            _context.Category.Update(category);
            _context.SaveChanges();
        }

        public void DeleteCategory(int id)
        {
            var category = GetCategoryById(id);
            if (category != null)
            {
                _context.Category.Remove(category);
                _context.SaveChanges();
            }
        }
    }
}