namespace EduPlatform.ViewModels
{
    public class AssignmentsPageViewModel
    {
        public List<AssignmentCardViewModel> Assignments { get; set; } = new();
        public string CreateUrl { get; set; } = "#";

        public List<AssignmentCardViewModel> Active => Assignments.Where(a => !a.IsArchived).ToList();
        public List<AssignmentCardViewModel> Archived => Assignments.Where(a => a.IsArchived).ToList();
    }

    public class AssignmentCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Type { get; set; } = "";
        public string GradingType { get; set; } = "";
        public DateTime DueDate { get; set; }
        public int ParticipantCount { get; set; }
        public int CompletedCount { get; set; }
        public int GradedCount { get; set; }
        public bool IsArchived { get; set; }
        public string ViewUrl { get; set; } = "#";

        public int ProgressPercent => ParticipantCount == 0 ? 0 : (int)Math.Round(CompletedCount * 100.0 / ParticipantCount);
    }
}