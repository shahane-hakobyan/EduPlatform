
namespace EduPlatform.Models
{
    public enum ScheduleDay
    {
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
        Sunday = 7
    }

    public class ScheduleEntry
    {
        public int Id { get; set; }

        public string TeacherName { get; set; } = "";  

        public ScheduleDay Day { get; set; }
        public int PeriodNumber { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public int? SchoolClassId { get; set; }  
        public SchoolClass? SchoolClass { get; set; }

        public int? SubjectId { get; set; }       
        public Subject? Subject { get; set; }

        public string? RoomNumber { get; set; }

        public string? Notes { get; set; }         
    }
}