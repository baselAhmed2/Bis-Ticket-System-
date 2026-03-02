using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO.Compression;
using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsDomain.Specifications.TicketSpecs;
using TicketsDomain.Specifications.UserSpecs;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Admin;
using TicketsShared.DTO.Common;
using TicketsShared.DTO.Tickets;

namespace TicketsServies
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            DbContext context,
            IUnitOfWork unitOfWork,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _context = context;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        // =====================
        // User Management
        // =====================
        public async Task<PagedResultDto<UserDto>> GetAllUsersAsync(
            int pageIndex, int pageSize, string? searchTerm = null, string? program = null, string? role = null)
        {
            var query = _userManager.Users.AsQueryable();

            // Filter by Program (SubAdmin يشوف بس البرنامج بتاعه)
            if (!string.IsNullOrEmpty(program))
            {
                query = query.Where(u => u.Program == program);
            }

            // ✅ Filter by Role على مستوى الداتابيز
            if (!string.IsNullOrEmpty(role))
            {
                var roleEntity = await _context.Set<IdentityRole>()
                    .FirstOrDefaultAsync(r => r.Name == role);

                if (roleEntity != null)
                {
                    var userIdsInRole = _context.Set<IdentityUserRole<string>>()
                        .Where(ur => ur.RoleId == roleEntity.Id)
                        .Select(ur => ur.UserId);

                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }
                else
                {
                    // الدور مش موجود — ارجع نتيجة فاضية
                    return new PagedResultDto<UserDto>
                    {
                        Data = [],
                        TotalCount = 0,
                        PageIndex = pageIndex,
                        PageSize = pageSize
                    };
                }
            }

            // Apply Search
            query = UserSearchSpec.ApplySearch(query, searchTerm);

            var totalCount = await query.CountAsync();

            // Apply Paging
            query = UserSearchSpec.ApplyPaging(query, pageIndex, pageSize);

            var users = await query.ToListAsync();

            // Batch load roles
            var userIds = users.Select(u => u.Id).ToList();
            var userRoles = await _context.Set<IdentityUserRole<string>>()
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Set<IdentityRole>(),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, RoleName = r.Name })
                .ToListAsync();

            var roleMap = userRoles.ToDictionary(x => x.UserId, x => x.RoleName ?? "Unknown");

            var userDtos = users.Select(user => new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Id,
                Name = user.Name,
                Role = roleMap.GetValueOrDefault(user.Id, "Unknown"),
                Program = user.Program
            }).ToList();

            return new PagedResultDto<UserDto>
            {
                Data = userDtos,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? user.Id,
                Name = user.Name,
                Role = roles.FirstOrDefault() ?? "Unknown",
                Program = user.Program
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new ApplicationUser
            {
                Id = dto.Id,
                UserName = dto.Id,
                Name = dto.Name,
                Program = dto.Program
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, dto.Role);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Role = dto.Role,
                Program = dto.Program
            };
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        // =====================
        // Doctor Subject Management
        // =====================
        public async Task<bool> AssignSubjectsToDoctorAsync(AssignSubjectsDto dto)
        {
            var existing = await _context.Set<DoctorSubject>()
                .Where(ds => ds.DoctorId == dto.DoctorId)
                .ToListAsync();

            var existingSubjectIds = existing.Select(ds => ds.SubjectId).ToHashSet();
            var newSubjectIds = dto.SubjectIds.ToHashSet();

            // المواد التي يجب حذفها (موجودة في القديم وليست في الجديد)
            var toRemove = existing.Where(ds => !newSubjectIds.Contains(ds.SubjectId)).ToList();
            // المواد التي يجب إضافتها (موجودة في الجديد وليست في القديم)
            var toAdd = newSubjectIds.Except(existingSubjectIds)
                .Select(subjectId => new DoctorSubject
                {
                    DoctorId = dto.DoctorId,
                    SubjectId = subjectId
                }).ToList();

            // لو مفيش تغيير — نجاح بالفعل
            if (toRemove.Count == 0 && toAdd.Count == 0)
                return true;

            if (toRemove.Count > 0)
                _context.Set<DoctorSubject>().RemoveRange(toRemove);

            if (toAdd.Count > 0)
                await _context.Set<DoctorSubject>().AddRangeAsync(toAdd);

            // Invalidate caches
            _cache.Remove(CacheKeys.AllSubjects(null));
            _cache.Remove(CacheKeys.DoctorSubjects(dto.DoctorId));
            _cache.Remove(CacheKeys.DoctorSubjectsDetail(dto.DoctorId));

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<SubjectDto>> GetDoctorSubjectsAsync(string doctorId)
        {
            var cacheKey = $"doctor_subjects_{doctorId}";

            // Try get from cache
            if (_cache.TryGetValue(cacheKey, out IEnumerable<SubjectDto>? cachedSubjects))
                return cachedSubjects!;

            // Get from DB
            var result = await _context.Set<DoctorSubject>()
                .Where(ds => ds.DoctorId == doctorId)
                .Include(ds => ds.Subject)
                .Select(ds => new SubjectDto
                {
                    Id = ds.Subject.Id,
                    Name = ds.Subject.Name,
                    Level = ds.Subject.Level,
                    Term = ds.Subject.Term,
                    Program = ds.Subject.Program
                })
                .ToListAsync();

            // Cache for 30 minutes
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

            return result;
        }

        public async Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(string? program = null)
        {
            var cacheKey = string.IsNullOrEmpty(program) ? "all_subjects" : $"subjects_{program}";

            // Try get from cache
            if (_cache.TryGetValue(cacheKey, out IEnumerable<SubjectDto>? cachedSubjects))
                return cachedSubjects!;

            // Get from DB
            var query = _context.Set<Subject>().AsQueryable();

            if (!string.IsNullOrEmpty(program))
            {
                query = query.Where(s => s.Program == program);
            }

            var subjectDtos = await query
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Level = s.Level,
                    Term = s.Term,
                    Program = s.Program
                })
                .ToListAsync();

            // Cache for 1 hour
            _cache.Set(cacheKey, subjectDtos, TimeSpan.FromHours(1));

            return subjectDtos;
        }

        public async Task<SubjectDto> CreateSubjectAsync(CreateSubjectDto dto)
        {
            var exists = await _context.Set<Subject>().AnyAsync(s => s.Id == dto.Id);
            if (exists)
                throw new InvalidOperationException($"Subject with ID '{dto.Id}' already exists.");

            var subject = new Subject
            {
                Id = dto.Id,
                Name = dto.Name,
                Level = dto.Level,
                Term = dto.Term,
                Program = dto.Program
            };

            _context.Set<Subject>().Add(subject);
            await _context.SaveChangesAsync();

            _cache.Remove(CacheKeys.AllSubjects(null));
            _cache.Remove(CacheKeys.AllSubjects(dto.Program));

            return new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Level = subject.Level,
                Term = subject.Term,
                Program = subject.Program
            };
        }

        // =====================
        // Admin Subject Assignment (NEW)
        // =====================
        public async Task<bool> AssignAdminToSubjectAsync(string adminId, string subjectId)
        {
            // تأكد إن مفيش ربط موجود أصلاً
            var exists = await _context.Set<DoctorSubject>()
                .AnyAsync(ds => ds.DoctorId == adminId && ds.SubjectId == subjectId);

            if (exists) return true; // مربوط فعلاً

            _context.Set<DoctorSubject>().Add(new DoctorSubject
            {
                DoctorId = adminId,
                SubjectId = subjectId
            });

            _cache.Remove(CacheKeys.DoctorSubjects(adminId));
            _cache.Remove(CacheKeys.AllSubjects(null));

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveAdminFromSubjectAsync(string adminId, string subjectId)
        {
            var record = await _context.Set<DoctorSubject>()
                .FirstOrDefaultAsync(ds => ds.DoctorId == adminId && ds.SubjectId == subjectId);

            if (record == null) return false;

            _context.Set<DoctorSubject>().Remove(record);

            _cache.Remove($"doctor_subjects_{adminId}");
            _cache.Remove("all_subjects");

            return await _context.SaveChangesAsync() > 0;
        }

        // =====================
        // Admin Messages (NEW)
        // =====================
        public async Task<PagedResultDto<AdminMessageDto>> GetAdminMessagesAsync(
            string adminId, int pageIndex, int pageSize)
        {
            // الرسائل اللي في التذاكر اللي الأدمن رد عليها
            // (التذاكر اللي فيها رسالة من الأدمن ده)
            var ticketIdsWithAdminMessages = _context.Set<Message>()
                .Where(m => m.SenderId == adminId)
                .Select(m => m.TicketId)
                .Distinct();

            var query = _context.Set<Message>()
                .Where(m => ticketIdsWithAdminMessages.Contains(m.TicketId))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new AdminMessageDto
                {
                    MessageId = m.Id,
                    Body = m.Body,
                    SentAt = m.SentAt,
                    SenderName = m.Sender.Name,
                    SenderId = m.SenderId,
                    TicketId = m.TicketId,
                    TicketTitle = m.Ticket.Title,
                    StudentName = m.Ticket.Student.Name,
                    DoctorName = m.Ticket.Doctor.Name
                });

            var totalCount = await query.CountAsync();

            var messages = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<AdminMessageDto>
            {
                Data = messages,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        // =====================
        // Ticket Monitoring
        // =====================
        public async Task<PagedResultDto<TicketDto>> GetAllTicketsFilteredAsync(TicketFilterDto filter)
        {
            var spec = new AdminTicketFilterSpec(
                level: filter.Level,
                term: filter.Term,
                status: filter.Status,
                doctorId: filter.DoctorId,
                subjectId: filter.SubjectId,
                searchTicketId: filter.SearchTicketId,
                isHighPriority: filter.IsHighPriority,
                program: filter.Program,
                pageIndex: filter.PageIndex,
                pageSize: filter.PageSize
            );

            var countSpec = new AdminTicketFilterSpec(
                level: filter.Level,
                term: filter.Term,
                status: filter.Status,
                doctorId: filter.DoctorId,
                subjectId: filter.SubjectId,
                searchTicketId: filter.SearchTicketId,
                isHighPriority: filter.IsHighPriority,
                program: filter.Program
            );

            var ticketRepo = _unitOfWork.GetRepository<Ticket, string>();

            var tickets = await ticketRepo.GetAllAsync(spec);
            var totalCount = await ticketRepo.CountAsync(countSpec);

            return new PagedResultDto<TicketDto>
            {
                Data = tickets.Select(MapToDto),
                TotalCount = totalCount,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize
            };
        }

        public async Task<IEnumerable<TicketDto>> GetHighPriorityTicketsAsync(string? program = null)
        {
            var spec = new HighPriorityTicketsSpec(program);
            var tickets = await _unitOfWork.GetRepository<Ticket, string>()
                .GetAllAsync(spec);

            return tickets.Select(MapToDto);
        }

        public async Task<PagedResultDto<TicketDto>> GetHighPriorityTicketsPagedAsync(
            int pageIndex, int pageSize, string? program = null)
        {
            var spec = new HighPriorityTicketsSpec(pageIndex, pageSize, program);
            var countSpec = new HighPriorityTicketsSpec(program);

            var ticketRepo = _unitOfWork.GetRepository<Ticket, string>();

            var tickets = await ticketRepo.GetAllAsync(spec);
            var totalCount = await ticketRepo.CountAsync(countSpec);

            return new PagedResultDto<TicketDto>
            {
                Data = tickets.Select(MapToDto),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<bool> MarkTicketAsHighPriorityAsync(string ticketId, bool isHighPriority)
        {
            var ticket = await _unitOfWork.GetRepository<Ticket, string>()
                .GetByIdAsync(ticketId);

            if (ticket == null) return false;

            ticket.IsHighPriority = isHighPriority;
            await _unitOfWork.GetRepository<Ticket, string>().UpdateAsync(ticket);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// SuperAdmin only: ينهي الترم — يحذف كل التذاكر والرسائل ويصفر العدادات
        /// </summary>
        public async Task<int> DeleteAllTicketsAsync()
        {
            var doctorIds = await _context.Set<Ticket>()
                .Select(t => t.DoctorId)
                .Distinct()
                .ToListAsync();

            var tickets = await _context.Set<Ticket>().ToListAsync();
            var count = tickets.Count;
            if (count == 0) return 0;

            _context.Set<Ticket>().RemoveRange(tickets);
            await _context.SaveChangesAsync();

            _cache.Remove("admin_analytics");
            foreach (var id in doctorIds)
            {
                _cache.Remove($"doctor_stats_{id}");
                _cache.Remove($"doctor_subjects_detail_{id}");
            }

            return count;
        }


        // =====================
        // Bulk Upload
        // =====================
        public async Task<BulkStudentUploadResultDto> BulkUploadStudentsAsync(IFormFile file)
        {
            var result = new BulkStudentUploadResultDto();

            try
            {
                using var stream = new System.IO.MemoryStream();
                await file.CopyToAsync(stream);
                // Ensure stream is read from the beginning before passing to EPPlus
                stream.Position = 0;

                // Basic validation: non-empty and .xlsx extension
                if (file.Length == 0 || stream.Length == 0)
                {
                    result.Errors.Add("Uploaded file is empty.");
                    return result;
                }

                var ext = Path.GetExtension(file.FileName) ?? string.Empty;
                // Accept .xlsx or .csv. If csv, we will parse it as text; if xlsx, use EPPlus.
                if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add("Only .xlsx or .csv files are supported. Please provide a valid .xlsx or .csv file and try again.");
                    return result;
                }

                // If CSV, parse as text without using EPPlus
                if (ext.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    stream.Position = 0;
                    using var reader = new StreamReader(stream);
                    var text = await reader.ReadToEndAsync();
                    var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();

                    if (lines.Count <= 1)
                    {
                        result.Errors.Add("CSV file contains no data rows (only header or empty).");
                        return result;
                    }

                    var headerCols = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToList();
                    // Expect at least 3 columns: id, ssn, name in any order
                    var idIndex = headerCols.FindIndex(h => h == "id");
                    var ssnIndex = headerCols.FindIndex(h => h == "ssn");
                    var nameIndex = headerCols.FindIndex(h => h == "name");

                    if (idIndex < 0 || ssnIndex < 0 || nameIndex < 0)
                    {
                        result.Errors.Add("CSV header must include 'Id', 'SSN' and 'Name' columns.");
                        return result;
                    }

                    var csvExistingStudents = await _userManager.GetUsersInRoleAsync("Student");
                    var csvExistingSsnSet = new HashSet<string>(csvExistingStudents.Select(s => s.SSN ?? string.Empty));
                    var csvExistingIdSet = new HashSet<string>(csvExistingStudents.Select(s => s.Id ?? string.Empty));

                    var csvNewStudents = new List<ApplicationUser>();

                    for (int i = 1; i < lines.Count; i++)
                    {
                        var row = lines[i];
                        var cols = row.Split(',').Select(c => c.Trim().Trim('"')).ToArray();

                        string id = cols.Length > idIndex ? cols[idIndex] : string.Empty;
                        string ssn = cols.Length > ssnIndex ? cols[ssnIndex] : string.Empty;
                        string name = cols.Length > nameIndex ? cols[nameIndex] : string.Empty;

                        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(ssn) || string.IsNullOrEmpty(name))
                        {
                            result.Skipped++;
                            result.Errors.Add($"Row {i + 1}: Missing required data.");
                            continue;
                        }

                        if (csvExistingSsnSet.Contains(ssn) || csvExistingIdSet.Contains(id))
                        {
                            result.Updated++;
                            continue;
                        }

                        var newStudent = new ApplicationUser
                        {
                            Id = id,
                            UserName = id,
                            SSN = ssn,
                            Name = name,
                            Email = $"{id}@std.teebaa.edu.eg",
                            EmailConfirmed = true,
                            IsActive = true
                        };

                        csvNewStudents.Add(newStudent);
                    }

                    if (csvNewStudents.Any())
                    {
                        foreach (var st in csvNewStudents)
                        {
                            var createResult = await _userManager.CreateAsync(st, "Tb123456*");
                            if (createResult.Succeeded)
                            {
                                await _userManager.AddToRoleAsync(st, "Student");
                                result.Added++;
                            }
                            else
                            {
                                result.Skipped++;
                                result.Errors.Add($"Failed to create user {st.Id}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                            }
                        }
                    }

                    return result;
                }

                // Quick integrity check: try opening the stream as a ZIP (xlsx is a zip archive)
                try
                {
                    stream.Position = 0;
                    using var zip = new ZipArchive(stream, ZipArchiveMode.Read, true);
                    // Try to read first byte of each entry to detect truncated/corrupt blocks
                    foreach (var entry in zip.Entries)
                    {
                        try
                        {
                            using var entryStream = entry.Open();
                            var buffer = new byte[1];
                            // Attempt to read; some legitimate small files might have empty entries but reading should not throw
                            _ = entryStream.Read(buffer, 0, 1);
                        }
                        catch (Exception)
                        {
                            result.Errors.Add("Uploaded .xlsx appears to be corrupted (invalid ZIP entry). Try opening and re-saving the file in Excel and upload again.");
                            return result;
                        }
                    }
                    stream.Position = 0;
                }
                catch (InvalidDataException)
                {
                    result.Errors.Add("Uploaded .xlsx is not a valid ZIP archive or is corrupted. Please open and re-save as .xlsx and try again.");
                    return result;
                }
                catch (Exception)
                {
                    // If Zip check fails unexpectedly, continue and let EPPlus produce a clearer error later
                    stream.Position = 0;
                }

                // Ensure EPPlus license is set. Support both pre-8 and 8+ APIs via reflection.
                try
                {
                    var excelPkgType = typeof(OfficeOpenXml.ExcelPackage);

                    // Try new API: ExcelPackage.License (object) with a setter method
                    var licenseProp = excelPkgType.GetProperty("License", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (licenseProp != null)
                    {
                        var licenseObj = licenseProp.GetValue(null);
                        if (licenseObj != null)
                        {
                            var licenseObjType = licenseObj.GetType();
                            var enumType = licenseObjType.Assembly.GetType("OfficeOpenXml.LicenseContext") ?? excelPkgType.Assembly.GetType("OfficeOpenXml.LicenseContext");
                            if (enumType != null)
                            {
                                var nonCommercial = Enum.Parse(enumType, "NonCommercial");

                                // Try methods on the license object
                                var setMethod = licenseObjType.GetMethod("SetLicense", new[] { enumType })
                                                ?? licenseObjType.GetMethod("SetLicenseContext", new[] { enumType });

                                if (setMethod != null)
                                {
                                    setMethod.Invoke(licenseObj, new[] { nonCommercial });
                                }
                                else
                                {
                                    // Try property 'Context' or similar
                                    var ctxProp = licenseObjType.GetProperty("Context", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                    if (ctxProp != null && ctxProp.CanWrite)
                                        ctxProp.SetValue(licenseObj, nonCommercial);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback to older API: ExcelPackage.LicenseContext
                        var lcProp = excelPkgType.GetProperty("LicenseContext", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (lcProp != null && lcProp.CanWrite)
                        {
                            var enumType = lcProp.PropertyType;
                            var nonCommercial = Enum.Parse(enumType, "NonCommercial");
                            lcProp.SetValue(null, nonCommercial);
                        }
                    }
                }
                catch
                {
                    // ignore and continue; if license isn't set correctly EPPlus will throw at runtime and we will capture that
                }

                try
                {
                    using var package = new OfficeOpenXml.ExcelPackage(stream);

                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        result.Errors.Add("Excel file is empty or formatted incorrectly (no worksheets found).");
                        return result;
                    }

                    if (worksheet.Dimension == null)
                    {
                        result.Errors.Add("Excel worksheet appears empty (no used cells). Ensure the sheet has data starting from row 2.");
                        return result;
                    }

                    int rowCountTemp = 0;
                    // Try to get rows from Dimension, but fallback to scanning cells if Dimension access overflows or is unreliable
                    try
                    {
                        if (worksheet.Dimension != null)
                        {
                            rowCountTemp = worksheet.Dimension.Rows;
                        }
                        else
                        {
                            // No dimension info; compute from non-empty cells
                            rowCountTemp = worksheet.Cells
                                .Where(c => c.Value != null && !string.IsNullOrEmpty(c.Value?.ToString()))
                                .Select(c => c.Start.Row)
                                .DefaultIfEmpty(0)
                                .Max();
                        }
                    }
                    catch (OverflowException)
                    {
                        // Fallback: compute last used row by scanning cells (may be slower but avoids Dimension overflow)
                        try
                        {
                            rowCountTemp = worksheet.Cells
                                .Where(c => c.Value != null && !string.IsNullOrEmpty(c.Value?.ToString()))
                                .Select(c => c.Start.Row)
                                .DefaultIfEmpty(0)
                                .Max();
                        }
                        catch (Exception exScan)
                        {
                            result.Errors.Add("Excel file parsing failed while scanning cells: " + exScan.Message);
                            return result;
                        }
                    }

                    if (rowCountTemp <= 1)
                    {
                        result.Errors.Add("Excel file contains no data rows (only header or empty).");
                        return result;
                    }

                    if (rowCountTemp > 200_000)
                    {
                        result.Errors.Add("Excel file is too large to process (rows: " + rowCountTemp + "). Please split the file into smaller parts.");
                        return result;
                    }

                    result.TotalRows = rowCountTemp > 1 ? rowCountTemp - 1 : 0; // Exclude header
                }
                catch (OverflowException oe)
                {
                    result.Errors.Add($"Excel file parsing failed (overflow): {oe.Message}");
                    return result;
                }
                catch (Exception exPkg)
                {
                    result.Errors.Add($"Excel file parsing failed: {exPkg.GetType().Name} - {exPkg.Message}");
                    return result;
                }

                // Re-open package for row iteration
                stream.Position = 0;
                using var package2 = new OfficeOpenXml.ExcelPackage(stream);
                var worksheet2 = package2.Workbook.Worksheets.First();
                int rowCount = worksheet2.Dimension.Rows;

                var existingStudents = await _userManager.GetUsersInRoleAsync("Student");
                var existingSsnSet = new HashSet<string>(existingStudents.Select(s => s.SSN));
                var existingIdSet = new HashSet<string>(existingStudents.Select(s => s.Id));

                var newStudents = new List<ApplicationUser>();

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var idVal = worksheet2.Cells[row, 1].Value;
                        var ssnVal = worksheet2.Cells[row, 2].Value;
                        var nameVal = worksheet2.Cells[row, 3].Value;

                        string id = idVal?.ToString()?.Trim();
                        string ssn = ssnVal?.ToString()?.Trim();
                        string name = nameVal?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(ssn) || string.IsNullOrEmpty(name))
                        {
                            result.Skipped++;
                            result.Errors.Add($"Row {row}: Missing required data.");
                            continue;
                        }

                        if (existingSsnSet.Contains(ssn) || existingIdSet.Contains(id))
                        {
                            result.Updated++; // Mark as updated/skipped for now
                            continue;
                        }

                        var newStudent = new ApplicationUser
                        {
                            Id = id,
                            UserName = id,
                            SSN = ssn,
                            Name = name,
                            Email = $"{id}@std.teebaa.edu.eg",
                            EmailConfirmed = true,
                            IsActive = true
                        };

                        newStudents.Add(newStudent);
                    }
                    catch (Exception ex)
                    {
                        result.Skipped++;
                        result.Errors.Add($"Row {row}: ({ex.GetType().Name}) {ex.Message}");
                    }
                }

                if (newStudents.Any())
                {
                    foreach (var st in newStudents)
                    {
                        try
                        {
                            var createResult = await _userManager.CreateAsync(st, "Tb123456*");
                            if (createResult.Succeeded)
                            {
                                await _userManager.AddToRoleAsync(st, "Student");
                                result.Added++;
                            }
                            else
                            {
                                result.Skipped++;
                                result.Errors.Add($"Failed to create user {st.Id}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                            }
                        }
                        catch (Exception innerEx)
                        {
                            result.Skipped++;
                            result.Errors.Add($"Exception on CreateAsync row ({innerEx.GetType().Name}): {innerEx.Message}");
                        }
                    }
                }
            }
            catch (Exception toplevel)
            {
                result.Errors.Add($"Fatal Error parsing the Excel File ({toplevel.GetType().Name}): {toplevel.Message}");
            }

            return result;
        }

        // =====================
        // Helper Method
        // =====================
        private static TicketDto MapToDto(Ticket ticket)
        {
            return new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Body = ticket.Body,
                Level = ticket.Level,
                Term = ticket.Term,
                GroupNumber = ticket.GroupNumber,
                Status = ticket.Status,
                IsHighPriority = ticket.IsHighPriority,
                CreatedAt = ticket.CreatedAt,
                Program = ticket.Program, // ✅ FIX

                StudentId = ticket.StudentId,
                StudentName = ticket.Student?.Name ?? "",

                DoctorId = ticket.DoctorId,
                DoctorName = ticket.Doctor?.Name ?? "",

                SubjectId = ticket.SubjectId,
                SubjectName = ticket.Subject?.Name ?? "",

                Messages = ticket.Messages?.Select(m => new MessageDto
                {
                    Id = m.Id,
                    Body = m.Body,
                    SentAt = m.SentAt,
                    IsHighPriority = m.IsHighPriority,
                    SenderId = m.SenderId,
                    SenderName = m.Sender?.Name ?? ""
                }).ToList() ?? []
            };
        }
    }
}
