namespace EduPlatform.ViewModels
{
    public class AssignmentDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Type { get; set; } = "";
        public string GradingType { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? StartDateRaw { get; set; }
        public string Description { get; set; } = "";
        public bool IsArchived { get; set; }
        public bool AllowTextAnswer { get; set; }
        public bool AllowFileAnswer { get; set; }
        public List<AssignmentFileViewModel> Files { get; set; } = new();
        public string ArchiveUrl { get; set; } = "#";
        public List<PupilSubmissionViewModel> Submissions { get; set; } = new();
        public List<AssignmentClassGroupViewModel> AvailableGrades { get; set; } = new();
        public List<string> AlreadyAddedPupilUserIds { get; set; } = new();
    }

    public class AssignmentFileViewModel
    {
        public string Name { get; set; } = "";
        public string ViewUrl { get; set; } = "#";
        public string DownloadUrl { get; set; } = "#";
    }

    public class PupilSubmissionViewModel
    {
        public int Id { get; set; }
        public string PupilName { get; set; } = "";
        public string PhotoUrl { get; set; } = "";
        public string GradeName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public DateTime? SubmittedDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending | Submitted | Graded
        public decimal? Score { get; set; }
        public int MaxScore { get; set; } = 10;
        public string ScoreFeedback { get; set; } = "";
        public string AnswerText { get; set; } = "";
        public string GradingType { get; set; } = "CurrentScore"; // CurrentScore | Formative | Diagnostic
        public List<AssignmentFileViewModel> Files { get; set; } = new();
    }
}
