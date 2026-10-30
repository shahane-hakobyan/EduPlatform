namespace EduPlatform.ViewModels
{
    public class AssignmentBankViewModel
    {
        public List<AssignmentBankCardViewModel> Items { get; set; } = new();
        public List<MaterialFilterOption> Filters { get; set; } = new();
        public string CreateUrl { get; set; } = "#";
    }

    public class AssignmentBankCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Type { get; set; } = "";
        public string GradingType { get; set; } = "";

        // առանձին արժեքներ՝ ֆիլտրերի համար
        public List<string> SubjectList { get; set; } = new();
        public List<string> GradeList { get; set; } = new();
        public List<string> TypeList { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public int FilesCount { get; set; }
        public int ParticipantCount { get; set; }
    }
}
