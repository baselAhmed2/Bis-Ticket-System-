using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketsDomain.Models;
using TicketsPerstince.Data.DataSeeding;
using TicketsPerstince.Data.DbContexts;

namespace TicketsPerstince.Data
{
    public class DataInitializer : IDataInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DataInitializer(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InitializeAsync()
        {
            await SeedRolesAsync();
            await SeedSubjectsAsync();
            await SeedDoctorsAsync();
            await SeedStudentsAsync();
            await SeedSubAdminsAsync();
            await SeedSuperAdminAsync();
        }

        // =====================
        // 1️⃣ Seed Roles

        // =====================
        private async Task SeedRolesAsync()
        {
            string[] roles = { "SuperAdmin", "SubAdmin", "Doctor", "Student" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // =====================
        // 2️⃣ Seed Subjects
        // =====================
        private async Task SeedSubjectsAsync()
        {
            if (_context.Subjects.Any()) return;

            var path = GetJsonFilePath("subjects.json");
            var json = await File.ReadAllTextAsync(path);

            var subjects = JsonSerializer.Deserialize<List<Subject>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (subjects is null) return;

            await _context.Subjects.AddRangeAsync(subjects);
            await _context.SaveChangesAsync();
        }

        // =====================
        // 3️⃣ Seed Doctors
        // =====================
        private async Task SeedDoctorsAsync()
        {
            var path = GetJsonFilePath("doctors.json");
            var json = await File.ReadAllTextAsync(path);

            var doctorDtos = JsonSerializer.Deserialize<List<DoctorSeedDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (doctorDtos is null) return;

            foreach (var dto in doctorDtos)
            {
                if (await _userManager.FindByIdAsync(dto.Id) is not null) continue;

                var doctor = new ApplicationUser
                {
                    Id = dto.Id,
                    UserName = dto.Id,
                    Name = dto.Name,
                    Program = dto.Program,
                };

                var result = await _userManager.CreateAsync(doctor, dto.Ssn);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating doctor {dto.Id}: {error.Description}");
                    }
                    continue;
                }

                await _userManager.AddToRoleAsync(doctor, "Doctor");

                foreach (var subjectId in dto.Subjects)
                {
                    var subject = await _context.Subjects.FindAsync(subjectId);
                    if (subject is null) continue;

                    _context.DoctorSubjects.Add(new DoctorSubject
                    {
                        DoctorId = doctor.Id,
                        SubjectId = subjectId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        // =====================
        // 4️⃣ Seed Students
        // =====================
        private async Task SeedStudentsAsync()
        {
            var path = GetJsonFilePath("students.json");
            var json = await File.ReadAllTextAsync(path);

            var studentDtos = JsonSerializer.Deserialize<List<StudentSeedDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (studentDtos is null) return;

            foreach (var dto in studentDtos)
            {
                if (await _userManager.FindByIdAsync(dto.Id) is not null) continue;

                var student = new ApplicationUser
                {
                    Id = dto.Id,
                    UserName = dto.Id,
                    Name = dto.Name,
                    Program = dto.Program,
                };

                var result = await _userManager.CreateAsync(student, dto.Ssn);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating student {dto.Id}: {error.Description}");
                    }
                    continue;
                }

                await _userManager.AddToRoleAsync(student, "Student");
            }
        }

        // =====================
        // 5️⃣ Seed SubAdmins (واحد لكل برنامج)
        // =====================
        private async Task SeedSubAdminsAsync()
        {
            var subAdmins = new[]
            {
                new { Id = "SUBADMIN_BIS", UserName = "subadmin_bis", Name = "BIS Sub Admin", Program = "BIS", Password = "SubBis@123456" },
                new { Id = "SUBADMIN_FMI", UserName = "subadmin_fmi", Name = "FMI Sub Admin", Program = "FMI", Password = "SubFmi@123456" },
                new { Id = "SUBADMIN_CS",  UserName = "subadmin_cs",  Name = "CS Sub Admin",  Program = "CS",  Password = "SubCs@123456"  },
            };

            foreach (var sa in subAdmins)
            {
                if (await _userManager.FindByIdAsync(sa.Id) is not null) continue;

                var admin = new ApplicationUser
                {
                    Id = sa.Id,
                    UserName = sa.UserName,
                    Name = sa.Name,
                    Program = sa.Program,
                };

                var result = await _userManager.CreateAsync(admin, sa.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, "SubAdmin");
                    // Give SubAdmin the Doctor role as well so they can be assigned to subjects
                    if (!await _roleManager.RoleExistsAsync("Doctor")) continue;
                        await _roleManager.CreateAsync(new IdentityRole("Doctor"));
                    await _userManager.AddToRoleAsync(admin, "Doctor");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating SubAdmin {sa.Id}: {error.Description}");
                    }
                }
            }
        }

        // =====================
        // 6️⃣ Seed SuperAdmin
        // =====================
        private async Task SeedSuperAdminAsync()
        {
            var adminId = "SUPERADMIN001";

            if (await _userManager.FindByIdAsync(adminId) is not null) return;

            var admin = new ApplicationUser
            {
                Id = adminId,
                UserName = "superadmin",
                Name = "Super Admin",
                Program = null, // SuperAdmin يشوف كل البرامج
            };

            var result = await _userManager.CreateAsync(admin, "Super@123456");

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, "SuperAdmin");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Error creating SuperAdmin: {error.Description}");
                }
            }
        }

        // =====================
        // Helper Method للمسارات
        // =====================
        private static string GetJsonFilePath(string fileName)
        {
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Data", "DataSeeding", "Json", fileName);

            if (File.Exists(outputPath))
                return outputPath;

            var projectPath = Path.Combine(Directory.GetCurrentDirectory(),
                "..", "TicketsPerstince", "Data", "DataSeeding", "Json", fileName);

            if (File.Exists(projectPath))
                return projectPath;

            throw new FileNotFoundException($"Could not find {fileName} in expected locations.");
        }
    }

    // =====================
    // Helper DTOs
    // =====================
    file class StudentSeedDto
    {
        public required string Id { get; set; }
        public required string Ssn { get; set; }
        public required string Name { get; set; }
        public required string Program { get; set; }
    }

    file class DoctorSeedDto
    {
        public required string Id { get; set; }
        public required string Ssn { get; set; }
        public required string Name { get; set; }
        public required string Program { get; set; }
        public required List<string> Subjects { get; set; }
    }
}
