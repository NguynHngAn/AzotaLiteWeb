using AzotaLite.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace AzotaLite.Web.Data
{
    public class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = new[] { "Teacher", "Student" };
            foreach(var r in roles)
            {
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));
            }

            // Teacher
            var teacherEmail = "teacher@demo.local";
            var teacher = await userMgr.FindByEmailAsync(teacherEmail);
            if (teacher == null)
            {
                teacher = new ApplicationUser { UserName = teacherEmail, Email = teacherEmail, EmailConfirmed = true };
                await userMgr.CreateAsync(teacher, "Passw0rd!");
                await userMgr.AddToRoleAsync(teacher, "Teacher");
            }

            // Student
            var studentEmail = "student@demo.local";
            var student = await userMgr.FindByEmailAsync(studentEmail);
            if(student == null)
            {
                student = new ApplicationUser { UserName = studentEmail, Email = studentEmail, EmailConfirmed = true };
                await userMgr.CreateAsync(student, "Passw0rd!");
                await userMgr.AddToRoleAsync(student, "Student");
            }
        }
    }
}
