namespace EduPlatform.ViewModels
{
    /// <summary>«Ուղարկել» էջ՝ շտեմարանի առաջադրանքի ամփոփում (միայն դիտում) + ժամկետներ + սովորողների ընտրություն</summary>
    public class SendAssignmentViewModel
    {
        public AssignmentDetailViewModel Assignment { get; set; } = new();
        public List<AssignmentClassGroupViewModel> ClassGroups { get; set; } = new();
        public List<AssignmentGroupOptionViewModel> Groups { get; set; } = new();

        public string SubmitUrl { get; set; } = "#";
        public string CancelUrl { get; set; } = "#";
        public string? Error { get; set; }

        /// <summary>datetime-local ձևաչափով (yyyy-MM-ddTHH:mm)</summary>
        public string StartDate { get; set; } = "";
        public string DueDate { get; set; } = "";
        public List<string> PupilIds { get; set; } = new();
    }

    /// <summary>«Ուղարկել» էջից եկող տվյալները</summary>
    public class SendAssignmentRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public List<string> PupilIds { get; set; } = new();
    }
}
