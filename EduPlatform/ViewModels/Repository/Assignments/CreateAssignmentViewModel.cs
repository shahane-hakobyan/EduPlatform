namespace EduPlatform.ViewModels
{
    public class CreateAssignmentViewModel
    {
        public List<SelectOption> Subjects { get; set; } = new();

        /// <summary>Դասարանի մակարդակները՝ 1–12</summary>
        public List<SelectOption> GradeLevels { get; set; } = new();

        /// <summary>Առաջադրանքի տեսակները (Id = AssignmentTypes դրոշակի արժեքը)</summary>
        public List<SelectOption> AssignmentTypes { get; set; } = new();

        /// <summary>Գնահատման տեսակները (Id = AssignmentGradingType արժեքը)</summary>
        public List<SelectOption> GradingTypes { get; set; } = new();

        public List<AssignmentClassGroupViewModel> ClassGroups { get; set; } = new();
        public string SubmitUrl { get; set; } = "#";
        public string CancelUrl { get; set; } = "#";

        /// <summary>Սերվերի կողմից ստուգման սխալի հաղորդագրություն</summary>
        public string? Error { get; set; }

        /// <summary>Շտեմարանից բացելիս՝ ձևի նախալրացումը</summary>
        public CreateAssignmentPrefill? Prefill { get; set; }

        /// <summary>Create | Send (շտեմարանից ուղարկել) | Edit (շտեմարանի գրառման խմբագրում)</summary>
        public string Mode { get; set; } = "Create";
    }

    public class CreateAssignmentPrefill
    {
        public int? SourceTemplateId { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public List<int> SubjectIds { get; set; } = new();
        public List<int> GradeLevels { get; set; } = new();

        /// <summary>Առանձին դրոշակներ (1, 2, 4, ...)</summary>
        public List<int> Types { get; set; } = new();
        public string? OtherTypeText { get; set; }
        public int GradingType { get; set; }
        public bool AllowTextAnswer { get; set; }
        public bool AllowFileAnswer { get; set; }
        public List<PrefillFile> ExistingFiles { get; set; } = new();

        /// <summary>Սխալից հետո ձևի վերադարձ՝ արդեն լրացված ամսաթվերն ու աշակերտները</summary>
        public string? StartDate { get; set; }
        public string? DueDate { get; set; }
        public List<string> PupilIds { get; set; } = new();
    }

    public class PrefillFile
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool Keep { get; set; } = true;
    }

    public class AssignmentClassGroupViewModel
    {
        public int GradeId { get; set; }
        public string GradeName { get; set; } = "";
        public List<RosterPupilViewModel> Pupils { get; set; } = new();
    }

    /// <summary>Ստեղծման ձևից եկող տվյալները</summary>
    public class CreateAssignmentRequest
    {
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public List<int> SubjectIds { get; set; } = new();
        public List<int> GradeLevels { get; set; } = new();

        /// <summary>AssignmentTypes դրոշակների արժեքները</summary>
        public List<int> Types { get; set; } = new();
        public string? OtherTypeText { get; set; }

        /// <summary>AssignmentGradingType-ի արժեքը</summary>
        public int GradingType { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }

        public bool AllowTextAnswer { get; set; }
        public bool AllowFileAnswer { get; set; }

        public List<Microsoft.AspNetCore.Http.IFormFile> Attachments { get; set; } = new();

        /// <summary>Ընտրված աշակերտների Id-ները (PupilUserId)</summary>
        public List<string> PupilIds { get; set; } = new();

        /// <summary>Շտեմարանի ձևանմուշը, որից բացվել է ձևը (եթե կա)</summary>
        public int? SourceTemplateId { get; set; }

        /// <summary>Ձևանմուշի այն ֆայլերի Id-ները, որոնք թողնվում են</summary>
        public List<int> KeepFileIds { get; set; } = new();
    }
}
