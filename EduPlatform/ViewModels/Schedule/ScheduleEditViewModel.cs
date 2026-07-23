namespace EduPlatform.ViewModels
{
    public class ScheduleEditViewModel
    {
        public string Day { get; set; } = "";
        public string DayLabel { get; set; } = "";
        public List<ScheduleEditRowViewModel> Rows { get; set; } = new();
        public List<SelectOption> Subjects { get; set; } = new();
        public List<SelectOption> Classes { get; set; } = new();
    }

    public class SelectOption
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
    }
}