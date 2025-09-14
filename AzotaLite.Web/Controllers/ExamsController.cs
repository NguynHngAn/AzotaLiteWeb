using System.Security.Claims;
using AzotaLite.Web.Data;
using AzotaLite.Web.Models;
using AzotaLite.Web.Services;
using AzotaLite.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzotaLite.Web.Controllers
{
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorage _storage;
        private readonly ILogger<ExamsController> _logger;

        public ExamsController(ApplicationDbContext db, IFileStorage storage, ILogger<ExamsController> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User must be authenticated.");

        // -----------------------------
        // LIST (Teacher's own exams)
        // -----------------------------
        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Index(string? q, CancellationToken ct)
        {
            var query = _db.Exams
                .Where(e => e.OwnerTeacherId == CurrentUserId);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(e => e.Title.Contains(q) || (e.Subject ?? "").Contains(q));

            var items = await query
                .OrderByDescending(e => e.Id)
                .ToListAsync(ct);

            ViewBag.Query = q;
            return View(items);
        }

        // -----------------------------
        // CREATE
        // -----------------------------
        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public IActionResult Create() => View(new ExamFormVM());

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamFormVM vm, CancellationToken ct)
        {
            if (vm.ExamFile == null || vm.ExamFile.Length == 0)
                ModelState.AddModelError(nameof(vm.ExamFile), "Vui lòng chọn file đề (PDF/DOCX).");

            if (!ModelState.IsValid) return View(vm);

            try
            {
                var saved = await _storage.SaveExamAsync(
                    vm.ExamFile!.OpenReadStream(),
                    vm.ExamFile.FileName,
                    vm.ExamFile.ContentType!,
                    ct);

                var exam = new Exam
                {
                    Title = vm.Title,
                    Subject = vm.Subject,
                    DurationMinutes = vm.DurationMinutes,
                    FilePath = saved.storedName,
                    FileSize = saved.size,
                    ContentType = vm.ExamFile.ContentType!,
                    OriginalFileName = vm.ExamFile.FileName!,
                    OwnerTeacherId = CurrentUserId
                };

                _db.Exams.Add(exam);
                await _db.SaveChangesAsync(ct);

                TempData["Msg"] = "Tạo đề thi thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create exam failed");
                ModelState.AddModelError(nameof(vm.ExamFile), $"Upload không hợp lệ: {ex.Message}");
                return View(vm);
            }
        }

        // -----------------------------
        // EDIT (metadata + optional file replace)
        // -----------------------------
        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var e = await _db.Exams
                .Where(x => x.Id == id && x.OwnerTeacherId == CurrentUserId)
                .FirstOrDefaultAsync(ct);

            if (e == null) return NotFound();

            var vm = new ExamFormVM
            {
                Id = e.Id,
                Title = e.Title,
                Subject = e.Subject,
                DurationMinutes = e.DurationMinutes
            };
            return View(vm);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExamFormVM vm, CancellationToken ct)
        {
            _logger.LogInformation("POST /Exams/Edit start. vm.Id={Id}, fileLen={Len}",
                vm.Id, vm.ExamFile?.Length);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid: {Errors}",
                    string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(vm);
            }

            if (vm.Id is null) return NotFound();

            var e = await _db.Exams
                .Where(x => x.Id == vm.Id && x.OwnerTeacherId == CurrentUserId)
                .FirstOrDefaultAsync(ct);

            if (e == null) return NotFound();

            // Update metadata
            e.Title = vm.Title;
            e.Subject = vm.Subject;
            e.DurationMinutes = vm.DurationMinutes;

            // Optional: replace file
            if (vm.ExamFile != null && vm.ExamFile.Length > 0)
            {
                try
                {
                    // Lưu file mới TRƯỚC
                    var saved = await _storage.SaveExamAsync(
                        vm.ExamFile.OpenReadStream(),
                        vm.ExamFile.FileName,
                        vm.ExamFile.ContentType!,
                        ct);

                    // Xóa file cũ SAU khi file mới ok
                    await _storage.DeleteExamAsync(e.FilePath, ct);

                    e.FilePath = saved.storedName;
                    e.FileSize = saved.size;
                    e.ContentType = vm.ExamFile.ContentType!;
                    e.OriginalFileName = vm.ExamFile.FileName!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Replace file failed for Exam Id={Id}", vm.Id);
                    ModelState.AddModelError(nameof(vm.ExamFile), $"Upload không hợp lệ: {ex.Message}");
                    return View(vm);
                }
            }

            await _db.SaveChangesAsync(ct);
            TempData["Msg"] = "Cập nhật đề thi thành công.";
            _logger.LogInformation("POST /Exams/Edit done. Id={Id}", vm.Id);
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------
        // DELETE
        // -----------------------------
        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var e = await _db.Exams
                .Where(x => x.Id == id && x.OwnerTeacherId == CurrentUserId)
                .FirstOrDefaultAsync(ct);

            if (e == null) return NotFound();
            return View(e);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
        {
            var e = await _db.Exams
                .Where(x => x.Id == id && x.OwnerTeacherId == CurrentUserId)
                .FirstOrDefaultAsync(ct);

            if (e == null) return NotFound();

            _db.Exams.Remove(e);
            await _db.SaveChangesAsync(ct);
            await _storage.DeleteExamAsync(e.FilePath, ct);

            TempData["Msg"] = "Xóa đề thi thành công.";
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------
        // DETAILS (metadata) – cho user đã đăng nhập (Teacher/Student)
        // -----------------------------
        [Authorize] // bất kỳ user đã đăng nhập
        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var e = await _db.Exams
                .Include(x => x.OwnerTeacher)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (e == null) return NotFound();

            ViewBag.IsOwner = (User.FindFirstValue(ClaimTypes.NameIdentifier) == e.OwnerTeacherId);
            return View(e);
        }

        // -----------------------------
        // DOWNLOAD – chỉ Teacher owner
        // -----------------------------
        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Download(int id, CancellationToken ct)
        {
            var e = await _db.Exams
                .Where(x => x.Id == id && x.OwnerTeacherId == CurrentUserId)
                .FirstOrDefaultAsync(ct);

            if (e == null) return Forbid();

            var stream = await _storage.OpenExamAsync(e.FilePath, ct);
            if (stream == null) return NotFound("File không tồn tại.");

            return File(stream, e.ContentType, e.OriginalFileName);
        }
    }
}
