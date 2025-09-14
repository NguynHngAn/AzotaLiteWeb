using System.Security.Claims;
using AzotaLite.Web.Data;
using AzotaLite.Web.Models;
using AzotaLite.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzotaLite.Web.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class ClassroomsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ClassroomsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User must be authenticated.");

        private IQueryable<Classroom> OwnedClassroomsQuery() =>
            _db.Classrooms.Where(c => c.TeacherId == CurrentUserId);

        // GET: /Classrooms
        [HttpGet]
        public async Task<IActionResult> Index(string? q, CancellationToken ct)
        {
            var query = OwnedClassroomsQuery();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(c => c.Name.Contains(q));

            var items = await query
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct);

            ViewBag.Query = q;
            return View(items);
        }

        // GET: /Classrooms/Create
        [HttpGet]
        public IActionResult Create() => View(new ClassroomFormVM());

        // POST: /Classrooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassroomFormVM vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var entity = new Classroom
            {
                Name = vm.Name,
                Grade = vm.Grade,
                TeacherId = CurrentUserId
            };

            _db.Classrooms.Add(entity);
            try
            {
                await _db.SaveChangesAsync(ct);
                TempData["Msg"] = "Tạo lớp thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Trường hợp trùng tên lớp (nếu bạn tạo unique index) hoặc lỗi DB khác
                ModelState.AddModelError(string.Empty, $"Không thể lưu lớp. Chi tiết: {ex.GetBaseException().Message}");
                return View(vm);
            }
        }

        // GET: /Classrooms/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var entity = await OwnedClassroomsQuery()
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync(ct);

            if (entity == null) return NotFound();

            var vm = new ClassroomFormVM
            {
                Id = entity.Id,
                Name = entity.Name,
                Grade = entity.Grade
            };
            return View(vm);
        }

        // POST: /Classrooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClassroomFormVM vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var entity = await OwnedClassroomsQuery()
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync(ct);

            if (entity == null) return NotFound();

            entity.Name = vm.Name;
            entity.Grade = vm.Grade;

            try
            {
                await _db.SaveChangesAsync(ct);
                TempData["Msg"] = "Cập nhật lớp thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, $"Không thể cập nhật lớp. Chi tiết: {ex.GetBaseException().Message}");
                return View(vm);
            }
        }

        // GET: /Classrooms/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var entity = await OwnedClassroomsQuery()
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync(ct);

            if (entity == null) return NotFound();
            return View(entity);
        }

        // POST: /Classrooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
        {
            var entity = await OwnedClassroomsQuery()
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync(ct);

            if (entity == null) return NotFound();

            _db.Classrooms.Remove(entity);
            try
            {
                await _db.SaveChangesAsync(ct);
                TempData["Msg"] = "Xóa lớp thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TempData["Msg"] = $"Không thể xóa lớp. Chi tiết: {ex.GetBaseException().Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Classrooms/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var entity = await _db.Classrooms
                .Include(c => c.Students)
                .Where(c => c.TeacherId == CurrentUserId && c.Id == id)
                .FirstOrDefaultAsync(ct);

            if (entity == null) return NotFound();
            return View(entity);
        }
    }
}
