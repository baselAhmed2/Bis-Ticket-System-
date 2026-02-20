using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsShared.DTO.Doctor;

namespace TicketsServiesAbstraction.IServices
{
    public interface IDoctorService
    {
        Task<DoctorStatsDto> GetDoctorStatsAsync(string doctorId);
        Task<IEnumerable<DoctorSubjectDto>> GetDoctorSubjectsAsync(string doctorId);
    }
}
