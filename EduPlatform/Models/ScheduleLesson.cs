namespace EduPlatform.Models
{
    public class ScheduleLesson
    {
        public int Id { get; set; }

        public string TeacherUserId { get; set; } = "";   

        public ScheduleDay Day { get; set; }
        public int PeriodNumber { get; set; }             
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public bool IsFree { get; set; }
        public int? GradeId { get; set; }                
        public Grade? Grade { get; set; }

        public int? SubjectId { get; set; }               
        public Subject? Subject { get; set; }

        public string? RoomNumber { get; set; }          
        public string? Notes { get; set; }
    }
}
