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
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public StudentsController(ApplicationDbContext db) => _db = db;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Tạo học sinh trong một lớp cụ thể
        public async Task<IActionResult> Create(int classroomId)
        {
            // chắn chắn lớp thuộc về giáo viên hiện tại
            var owns = await _db.Classrooms.AnyAsync(c => c.Id == classroomId && c.TeacherId == CurrentUserId);
            if (!owns) return NotFound();

            return View(new StudentFormVM { ClassroomId = classroomId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentFormVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var owns = await _db.Classrooms.AnyAsync(c => c.Id == vm.ClassroomId && c.TeacherId == CurrentUserId);
            if (!owns) return NotFound();

            var entity = new Student
            {
                FullName = vm.FullName,
                Email = vm.Email,
                ClassroomId = vm.ClassroomId
            };
            _db.Students.Add(entity);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Thêm học sinh thành công.";
            return RedirectToAction("Details", "Classrooms", new { id = vm.ClassroomId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var student = await _db.Students
                .Include(s => s.Classroom)
                .Where(s => s.Id == id && s.Classroom.TeacherId == CurrentUserId)
                .FirstOrDefaultAsync();
            if (student == null) return NotFound();

            var vm = new StudentFormVM
            {
                Id = student.Id,
                FullName = student.FullName,
                Email = student.Email,
                ClassroomId = student.ClassroomId
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentFormVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var student = await _db.Students
                .Include(s => s.Classroom)
                .Where(s => s.Id == id && s.Classroom.TeacherId == CurrentUserId)
                .FirstOrDefaultAsync();
            if (student == null) return NotFound();

            student.FullName = vm.FullName;
            student.Email = vm.Email;
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Cập nhật học sinh thành công.";
            return RedirectToAction("Details", "Classrooms", new { id = student.ClassroomId });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var student = await _db.Students
                .Include(s => s.Classroom)
                .Where(s => s.Id == id && s.Classroom.TeacherId == CurrentUserId)
                .FirstOrDefaultAsync();
            if (student == null) return NotFound();

            return View(student);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _db.Students
                .Include(s => s.Classroom)
                .Where(s => s.Id == id && s.Classroom.TeacherId == CurrentUserId)
                .FirstOrDefaultAsync();
            if (student == null) return NotFound();

            var classId = student.ClassroomId;
            _db.Students.Remove(student);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Xóa học sinh thành công.";
            return RedirectToAction("Details", "Classrooms", new { id = classId });
        }
    }
}
