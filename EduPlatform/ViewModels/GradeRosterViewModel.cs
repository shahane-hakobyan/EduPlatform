namespace EduPlatform.ViewModels
{
    public class GradeRosterViewModel
    {
        public string GradeName { get; set; } = "";
        public List<RosterPupilViewModel> Pupils { get; set; } = new();
    }

    public class RosterPupilViewModel
    {
        public string Name { get; set; } = "";
        public string PhotoUrl { get; set; } = "";
    }
}