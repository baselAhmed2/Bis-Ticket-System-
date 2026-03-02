namespace TicketsServies
{
    public static class CacheKeys
    {
        public static string DoctorSubjectsDetail(string doctorId) => $"doctor_subjects_detail_{doctorId}";
        public static string DoctorSubjects(string doctorId) => $"doctor_subjects_{doctorId}";
        public static string DoctorStats(string doctorId) => $"doctor_stats_{doctorId}";
        public static string AllSubjects(string? program) => string.IsNullOrEmpty(program) ? "all_subjects" : $"subjects_{program}";
        public static string AdminAnalytics(int? days) => days.HasValue ? $"admin_analytics_days_{days.Value}" : "admin_analytics_all";
    }
}
