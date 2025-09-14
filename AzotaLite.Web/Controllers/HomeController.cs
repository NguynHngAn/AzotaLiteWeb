using System.Diagnostics;
using System.Security.Claims;
using AzotaLite.Web.Data;
using AzotaLite.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzotaLite.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        /// <summary>
        /// Trang chủ: hiển thị đếm lớp học (nếu có DB).
        /// Nếu người dùng đăng nhập là Teacher, có thể đếm theo TeacherId.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            int count;
            try
            {
                // Nếu muốn chỉ đếm lớp của riêng giáo viên hiện tại:
                // nếu đã đăng nhập và có role Teacher → đếm theo TeacherId
                if (User.Identity?.IsAuthenticated == true && User.IsInRole("Teacher"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    count = await _db.Classrooms
                                     .Where(c => c.TeacherId == currentUserId!)
                                     .CountAsync(ct);
                }
                else
                {
                    // Mặc định: đếm tổng số Classroom (có thể đổi thành 0 nếu không muốn lộ số liệu)
                    count = await _db.Classrooms.CountAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đếm số Classroom ở Home/Index");
                count = 0; // Fallback an toàn để không làm vỡ trang
            }

            sw.Stop();
            _logger.LogInformation("Home/Index visited. ClassroomCount={Count}. Elapsed={Elapsed}ms",
                count, sw.ElapsedMilliseconds);

            ViewBag.ClassroomCount = count;
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Privacy()
        {
            _logger.LogWarning("Home/Privacy visited - sample warning");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet]
        public IActionResult Error()
        {
            // Giữ nguyên model lỗi mặc định
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
