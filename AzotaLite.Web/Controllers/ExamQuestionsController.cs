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
    public class ExamQuestionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ExamQuestionsController(ApplicationDbContext db) => _db = db;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Danh sách câu hỏi của 1 Exam (chỉ owner xem/sửa)
        [HttpGet]
        public async Task<IActionResult> Index(int examId, CancellationToken ct)
        {
            var exam = await _db.Exams
                .FirstOrDefaultAsync(e => e.Id == examId && e.OwnerTeacherId == CurrentUserId, ct);
            if (exam == null) return NotFound();

            var questions = await _db.Questions
                .Where(q => q.ExamId == examId)
                .Include(q => q.Choices)
                .OrderBy(q => q.Order)
                .ToListAsync(ct);

            ViewBag.Exam = exam;
            return View(questions);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int examId, CancellationToken ct)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId && e.OwnerTeacherId == CurrentUserId, ct);
            if (exam == null) return NotFound();

            var vm = new QuestionFormVM { ExamId = examId, Type = QuestionType.MultipleChoice, Order = 1 };
            // khởi tạo 4 lựa chọn trống (MCQ)
            vm.Choices = new List<ChoiceItemVM> {
                new(), new(), new(), new()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuestionFormVM vm, CancellationToken ct)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == vm.ExamId && e.OwnerTeacherId == CurrentUserId, ct);
            if (exam == null) return NotFound();

            if (!ModelState.IsValid) return View(vm);

            var q = new Question
            {
                ExamId = vm.ExamId,
                Text = vm.Text,
                Type = vm.Type,
                Order = vm.Order
            };
            _db.Questions.Add(q);
            await _db.SaveChangesAsync(ct);

            // Choices (nếu MCQ và có nhập)
            if (vm.Type == QuestionType.MultipleChoice && vm.Choices?.Any() == true)
            {
                foreach (var c in vm.Choices.Where(c => !string.IsNullOrWhiteSpace(c.Text)))
                {
                    _db.Choices.Add(new Choice
                    {
                        QuestionId = q.Id,
                        Text = c.Text,
                        IsCorrect = c.IsCorrect
                    });
                }
                await _db.SaveChangesAsync(ct);
            }

            TempData["Msg"] = "Thêm câu hỏi thành công.";
            return RedirectToAction(nameof(Index), new { examId = vm.ExamId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var q = await _db.Questions
                .Include(x => x.Exam)
                .Include(x => x.Choices)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (q == null || q.Exam.OwnerTeacherId != CurrentUserId) return NotFound();

            var vm = new QuestionFormVM
            {
                Id = q.Id,
                ExamId = q.ExamId,
                Text = q.Text,
                Type = q.Type,
                Order = q.Order,
                Choices = q.Choices.Select(c => new ChoiceItemVM
                {
                    Id = c.Id,
                    Text = c.Text,
                    IsCorrect = c.IsCorrect
                }).ToList()
            };
            // đảm bảo có 4 ô cho MCQ
            while (vm.Choices.Count < 4) vm.Choices.Add(new ChoiceItemVM());
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(QuestionFormVM vm, CancellationToken ct)
        {
            var q = await _db.Questions
                .Include(x => x.Exam)
                .Include(x => x.Choices)
                .FirstOrDefaultAsync(x => x.Id == vm.Id, ct);
            if (q == null || q.Exam.OwnerTeacherId != CurrentUserId) return NotFound();

            if (!ModelState.IsValid) return View(vm);

            q.Text = vm.Text;
            q.Type = vm.Type;
            q.Order = vm.Order;

            // cập nhật choices: xoá hết rồi thêm lại (đơn giản cho người mới)
            var old = _db.Choices.Where(c => c.QuestionId == q.Id);
            _db.Choices.RemoveRange(old);
            await _db.SaveChangesAsync(ct);

            if (vm.Type == QuestionType.MultipleChoice)
            {
                foreach (var c in vm.Choices.Where(c => !string.IsNullOrWhiteSpace(c.Text)))
                {
                    _db.Choices.Add(new Choice
                    {
                        QuestionId = q.Id,
                        Text = c.Text,
                        IsCorrect = c.IsCorrect
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
            TempData["Msg"] = "Cập nhật câu hỏi thành công.";
            return RedirectToAction(nameof(Index), new { examId = q.ExamId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var q = await _db.Questions.Include(x => x.Exam).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (q == null || q.Exam.OwnerTeacherId != CurrentUserId) return NotFound();
            return View(q);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
        {
            var q = await _db.Questions.Include(x => x.Exam).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (q == null || q.Exam.OwnerTeacherId != CurrentUserId) return NotFound();

            _db.Questions.Remove(q);
            await _db.SaveChangesAsync(ct);

            TempData["Msg"] = "Xóa câu hỏi thành công.";
            return RedirectToAction(nameof(Index), new { examId = q.ExamId });
        }

        // SEED demo 20 câu trắc nghiệm cho 1 Exam (Teacher owner)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedDemo(int examId, CancellationToken ct)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId && e.OwnerTeacherId == CurrentUserId, ct);
            if (exam == null) return NotFound();

            // Xoá câu cũ (tuỳ bạn quyết định)
            var oldQs = _db.Questions.Where(q => q.ExamId == examId);
            _db.Questions.RemoveRange(oldQs);
            await _db.SaveChangesAsync(ct);

            // Thêm 20 câu MCQ (A đúng)
            for (int i = 1; i <= 20; i++)
            {
                var q = new Question
                {
                    ExamId = examId,
                    Text = $"Câu {i}: 2 + 2 = ?",
                    Type = QuestionType.MultipleChoice,
                    Order = i
                };
                _db.Questions.Add(q);
                await _db.SaveChangesAsync(ct);

                _db.Choices.AddRange(
                    new Choice { QuestionId = q.Id, Text = "4", IsCorrect = true },
                    new Choice { QuestionId = q.Id, Text = "3", IsCorrect = false },
                    new Choice { QuestionId = q.Id, Text = "5", IsCorrect = false },
                    new Choice { QuestionId = q.Id, Text = "22", IsCorrect = false }
                );
                await _db.SaveChangesAsync(ct);
            }

            TempData["Msg"] = "Seed 20 câu trắc nghiệm demo thành công.";
            return RedirectToAction(nameof(Index), new { examId });
        }
    }
}
