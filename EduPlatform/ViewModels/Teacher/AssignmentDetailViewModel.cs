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
        public string Description { get; set; } = "";
        public bool IsArchived { get; set; }
        public string ArchiveUrl { get; set; } = "#";
        public List<PupilSubmissionViewModel> Submissions { get; set; } = new();
        public List<AssignmentClassGroupViewModel> AvailableGrades { get; set; } = new();
        public List<int> AlreadyAddedPupilIds { get; set; } = new(); 
    }

    public class PupilSubmissionViewModel
    {
        public int Id { get; set; }
        public string PupilName { get; set; } = "";
        public string PhotoUrl { get; set; } = "";
        public string GradeName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public DateTime? SubmittedDate { get; set; }
        public string Status { get; set; } = "Pending";
        public int? Score { get; set; }
        public int MaxScore { get; set; } = 10;
        public string ScoreFeedback { get; set; } = "";
        public string AttachmentFileName { get; set; } = "";
        public string AttachmentUrl { get; set; } = "#";
        public string GradingMode { get; set; } = "Point"; // Point | Formative
        public string ProtocolText { get; set; } = "";
    }
}