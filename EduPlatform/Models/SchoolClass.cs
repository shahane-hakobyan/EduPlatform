namespace EduPlatform.Models
{
    public class SchoolClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";   
        public List<ScheduleEntry> ScheduleEntries { get; set; } = new();
    }
}
