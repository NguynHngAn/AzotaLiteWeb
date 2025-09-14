using System.Security.Claims;
using AzotaLite.Web.Data;
using AzotaLite.Web.Models;
using AzotaLite.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzotaLite.Web.Controllers
{
    [Authorize(Roles = "Student")]
    public class SubmissionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SubmissionsController> _logger;

        public SubmissionsController(ApplicationDbContext db, ILogger<SubmissionsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User must be authenticated.");

        // --------------------------------------------------------------------
        // START: Tạo (hoặc lấy lại) submission cho Student hiện tại rồi chuyển tới Do/{id}
        // Gọi từ Exams/Details bằng form POST (đã hướng dẫn ở bước trước).
        // --------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int examId, CancellationToken ct)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, ct);
            if (exam == null) return NotFound();

            // Nếu đã có submission cho exam này và student hiện tại → dùng lại
            var exist = await _db.Submissions
                .FirstOrDefaultAsync(s => s.ExamId == examId && s.StudentId == CurrentUserId, ct);

            if (exist != null)
            {
                TempData["Msg"] = "Bạn đã bắt đầu bài thi này trước đó.";
                return RedirectToAction(nameof(Do), new { id = exist.Id });
            }

            var sb = new Submission
            {
                ExamId = examId,
                StudentId = CurrentUserId,
                StartedAt = DateTime.UtcNow
            };
            _db.Submissions.Add(sb);
            await _db.SaveChangesAsync(ct);

            return RedirectToAction(nameof(Do), new { id = sb.Id });
        }

        // --------------------------------------------------------------------
        // DO: Trang làm bài (render câu hỏi + đồng hồ; View = Views/Submissions/Do.cshtml)
        // --------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Do(int id, CancellationToken ct)
        {
            var sb = await _db.Submissions
                .Include(s => s.Exam)
                .FirstOrDefaultAsync(s => s.Id == id && s.StudentId == CurrentUserId, ct);

            if (sb == null) return NotFound();

            var exam = sb.Exam;

            // Lấy câu hỏi + lựa chọn
            var questions = await _db.Questions
                .Where(q => q.ExamId == exam.Id)
                .OrderBy(q => q.Order)
                .Select(q => new TakeExamVM.QuestionItem
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type,
                    Order = q.Order,
                    Choices = q.Choices
                        .OrderBy(c => c.Id)
                        .Select(c => new TakeExamVM.ChoiceItem { Id = c.Id, Text = c.Text })
                        .ToList()
                })
                .ToListAsync(ct);

            // Câu trả lời đã lưu (nếu có)
            var answers = await _db.SubmissionAnswers
                .Where(a => a.SubmissionId == sb.Id)
                .ToDictionaryAsync(
                    a => a.QuestionId,
                    a => new TakeExamVM.AnswerValue { ChoiceId = a.ChoiceId, TextAnswer = a.TextAnswer },
                    ct);

            var vm = new TakeExamVM
            {
                SubmissionId = sb.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                DurationMinutes = exam.DurationMinutes,
                StartedAtUtc = sb.StartedAt,
                EndAtUtc = sb.StartedAt.AddMinutes(exam.DurationMinutes),
                Questions = questions,
                ExistingAnswers = answers,
                IsSubmitted = sb.SubmittedAt.HasValue
            };

            return View(vm);
        }

        // --------------------------------------------------------------------
        // SAVE ANSWER: API autosave từng câu (Fetch từ Do.cshtml)
        // Route: POST /submissions/{id}/answers
        // --------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("submissions/{id:int}/answers")]
        public async Task<IActionResult> SaveAnswer(int id, [FromBody] SaveAnswerDto dto, CancellationToken ct)
        {
            // Kiểm tra submission thuộc student hiện tại
            var sb = await _db.Submissions
                .Include(s => s.Exam)
                .FirstOrDefaultAsync(s => s.Id == id && s.StudentId == CurrentUserId, ct);

            if (sb == null) return NotFound();
            if (sb.SubmittedAt.HasValue) return BadRequest("Bài đã nộp.");

            // Hạn thời gian
            var end = sb.StartedAt.AddMinutes(sb.Exam.DurationMinutes);
            if (DateTime.UtcNow > end.AddSeconds(10)) // grace 10s
                return BadRequest("Hết thời gian làm bài.");

            // Câu hỏi phải thuộc exam
            var q = await _db.Questions.FirstOrDefaultAsync(x => x.Id == dto.QuestionId && x.ExamId == sb.ExamId, ct);
            if (q == null) return BadRequest("Câu hỏi không hợp lệ.");

            // Với MCQ: ChoiceId phải thuộc Question
            if (dto.ChoiceId.HasValue)
            {
                var okChoice = await _db.Choices.AnyAsync(c => c.Id == dto.ChoiceId.Value && c.QuestionId == q.Id, ct);
                if (!okChoice) return BadRequest("Lựa chọn không hợp lệ.");
            }

            // Upsert Answer
            var ans = await _db.SubmissionAnswers
                .FirstOrDefaultAsync(a => a.SubmissionId == sb.Id && a.QuestionId == q.Id, ct);

            if (ans == null)
            {
                ans = new SubmissionAnswer
                {
                    SubmissionId = sb.Id,
                    QuestionId = q.Id,
                    ChoiceId = dto.ChoiceId,
                    TextAnswer = dto.TextAnswer
                };
                _db.SubmissionAnswers.Add(ans);
            }
            else
            {
                ans.ChoiceId = dto.ChoiceId;
                ans.TextAnswer = dto.TextAnswer;
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { saved = true, at = DateTime.UtcNow });
        }

        // --------------------------------------------------------------------
        // FINALIZE: Nộp bài (auto-submit khi hết giờ hoặc bấm Nộp)
        // Route: POST /submissions/{id}/finalize
        // --------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("submissions/{id:int}/finalize")]
        public async Task<IActionResult> Finalize(int id, CancellationToken ct)
        {
            var sb = await _db.Submissions
                .Include(s => s.Exam)
                .FirstOrDefaultAsync(s => s.Id == id && s.StudentId == CurrentUserId, ct);

            if (sb == null) return NotFound();
            if (sb.SubmittedAt.HasValue) return Ok(new { submitted = true });

            // Cho phép nộp ngay cả khi quá giờ
            sb.SubmittedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            // Bước 10 sẽ enqueue job chấm điểm tự động
            TempData["Msg"] = "Đã nộp bài.";
            return Ok(new { submitted = true });
        }

        // --------------------------------------------------------------------
        // (TÙY CHỌN – chỉ dùng khi dev) Bỏ comment nếu muốn test nhanh:
        // GET /submissions/dev-start/{examId}
        // Tự tạo/tìm submission rồi chuyển tới Do/{id}.
        // --------------------------------------------------------------------
        /*
        [HttpGet("/submissions/dev-start/{examId:int}")]
        public async Task<IActionResult> DevStart(int examId, CancellationToken ct)
        {
            var exist = await _db.Submissions
                .FirstOrDefaultAsync(s => s.ExamId == examId && s.StudentId == CurrentUserId, ct);

            if (exist != null)
                return RedirectToAction(nameof(Do), new { id = exist.Id });

            var sb = new Submission
            {
                ExamId = examId,
                StudentId = CurrentUserId,
                StartedAt = DateTime.UtcNow
            };
            _db.Submissions.Add(sb);
            await _db.SaveChangesAsync(ct);

            return RedirectToAction(nameof(Do), new { id = sb.Id });
        }
        */
    }
}
