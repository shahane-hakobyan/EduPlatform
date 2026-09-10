namespace EduPlatform.ViewModels
{
    public class CreateAssignmentViewModel
    {
        public List<SelectOption> Subjects { get; set; } = new();
        public List<SelectOption> Grades { get; set; } = new();
        public List<string> AssignmentTypes { get; set; } = new();
        public List<string> GradingTypes { get; set; } = new();
        public List<AssignmentClassGroupViewModel> ClassGroups { get; set; } = new();
        public string SubmitUrl { get; set; } = "#";
        public string CancelUrl { get; set; } = "#";
    }

    public class AssignmentClassGroupViewModel
    {
        public int GradeId { get; set; }
        public string GradeName { get; set; } = "";
        public List<RosterPupilViewModel> Pupils { get; set; } = new();
    }
}