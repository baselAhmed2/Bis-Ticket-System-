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
                await SeedAdminAsync();
            }

            // =====================
            // 1️⃣ Seed Roles
            // =====================
            private async Task SeedRolesAsync()
            {
                string[] roles = { "Admin", "Doctor", "Student" };

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
                    // لو الدكتور موجود خليه
                    if (await _userManager.FindByIdAsync(dto.Id) is not null) continue;

                    var doctor = new ApplicationUser
                    {
                        Id = dto.Id,
                        UserName = dto.Id,
                        Name = dto.Name,
                    };

                    // ✅ تحقق من نجاح العملية
                    var result = await _userManager.CreateAsync(doctor, dto.Ssn);
                    
                    if (!result.Succeeded)
                    {
                        // لو فشل، اطبع الأخطاء واستمر
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Error creating doctor {dto.Id}: {error.Description}");
                        }
                        continue;
                    }

                    // ✅ ضيف الـ Role بعد التأكد من إنشاء الـ User
                    await _userManager.AddToRoleAsync(doctor, "Doctor");

                    // Assign Subjects
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
                    };

                    // ✅ تحقق من نجاح العملية
                    var result = await _userManager.CreateAsync(student, dto.Ssn);
                    
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Error creating student {dto.Id}: {error.Description}");
                        }
                        continue;
                    }

                    // ✅ ضيف الـ Role بعد التأكد من إنشاء الـ User
                    await _userManager.AddToRoleAsync(student, "Student");
                }
            }

            // =====================
            // 5️⃣ Seed Admin
            // =====================
            private async Task SeedAdminAsync()
            {
                var adminId = "ADMIN001";

                if (await _userManager.FindByIdAsync(adminId) is not null) return;

                var admin = new ApplicationUser
                {
                    Id = adminId,
                    UserName = "admin",
                    Name = "System Admin",
                };

                // ✅ تحقق من نجاح العملية
                var result = await _userManager.CreateAsync(admin, "Admin@123456");
                
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, "Admin");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating admin: {error.Description}");
                    }
                }
            }

            // =====================
            // Helper Method للمسارات
            // =====================
            private static string GetJsonFilePath(string fileName)
            {
                // جرب الأول في الـ Output Directory
                var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Data", "DataSeeding", "Json", fileName);

                if (File.Exists(outputPath))
                    return outputPath;

                // لو مش موجود، جرب في الـ Project Directory
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
        }

        file class DoctorSeedDto
        {
            public required string Id { get; set; }
            public required string Ssn { get; set; }
            public required string Name { get; set; }
            public required List<string> Subjects { get; set; }
        }
    }
