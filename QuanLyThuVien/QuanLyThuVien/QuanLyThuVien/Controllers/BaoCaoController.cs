using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;
using System.Linq;

namespace QuanLyThuVien.Controllers
{
    public class BaoCaoController : Controller
    {
        private readonly LibraryDbContext _context;

        public BaoCaoController(LibraryDbContext context) { _context = context; }

        public IActionResult Index()
        {
            // Thống kê số lượng sách theo từng thể loại
            ViewBag.BooksByCategory = _context.Books
                .GroupBy(b => b.Category)
                .Select(g => new { Category = string.IsNullOrEmpty(g.Key) ? "Chưa phân loại" : g.Key, Count = g.Sum(b => b.Quantity) })
                .ToDictionary(k => k.Category, v => v.Count);

            // Thống kê tình trạng mượn trả
            ViewBag.BorrowStats = _context.BorrowRecords
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(k => k.Status, v => v.Count);

            return View();
        }
    }
}