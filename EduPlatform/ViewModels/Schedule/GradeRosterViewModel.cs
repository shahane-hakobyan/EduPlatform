namespace EduPlatform.ViewModels
{
    public class GradeRosterViewModel
    {
        public string GradeName { get; set; } = "";
        public List<RosterPupilViewModel> Pupils { get; set; } = new();
    }

    public class RosterPupilViewModel
    {
        /// <summary>Աշակերտի Id-ն AuthServer-ում («sub»)</summary>
        public string UserId { get; set; } = "";
        public string Name { get; set; } = "";
        public string PhotoUrl { get; set; } = "";

        /// <summary>Աշակերտի էջի հղումը։ Դատարկ է՝ քանի դեռ աշակերտի էջը (AuthServer) չկա, այդ դեպքում անունը հղում չէ</summary>
        public string? ProfileUrl { get; set; }
    }
}