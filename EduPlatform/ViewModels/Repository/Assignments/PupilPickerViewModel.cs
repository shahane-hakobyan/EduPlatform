namespace EduPlatform.ViewModels
{
    /// <summary>
    /// Սովորողների ընտրության ընդհանուր բլոկը («Իմ դասարանները» / «Իմ խմբերը»)՝
    /// օգտագործվում է Ստեղծելու, Շտեմարանից ուղարկելու էջերում և «Ավելացնել սովորողներ» պատուհանում։
    /// </summary>
    public class PupilPickerViewModel
    {
        public List<AssignmentClassGroupViewModel> ClassGroups { get; set; } = new();
        public List<AssignmentGroupOptionViewModel> Groups { get; set; } = new();

        /// <summary>Արդեն ընտրված սովորողները (սխալից հետո ձևի վերադարձ / նախալրացում)</summary>
        public ICollection<string> SelectedIds { get; set; } = new List<string>();

        /// <summary>Առաջադրանքն արդեն ստացած սովորողները՝ երևում են, բայց չեն ընտրվում</summary>
        public ICollection<string> AlreadyAddedIds { get; set; } = new List<string>();

        /// <summary>Checkbox-ների name՝ ձևի հետ ուղարկելու համար («PupilIds»)։ Պատուհանում դատարկ է (JS-ն է ուղարկում)</summary>
        public string? FieldName { get; set; }
    }
}
