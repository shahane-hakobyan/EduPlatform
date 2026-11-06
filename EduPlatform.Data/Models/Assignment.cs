namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Շտեմարանի առաջադրանք՝ առաջադրանքի բովանդակությունը (վերնագիր, տեքստ, տեսակներ, առարկաներ, դասարաններ, ֆայլեր)։
    /// Աշակերտին տրված առաջադրանքը <see cref="GivenAssignment"/>-ն է, որը միայն կապվում է այս գրառմանը։
    /// </summary>
    public class Assignment
    {
        public int Id { get; set; }

        /// <summary>Ուսուցչի Id-ն AuthServer-ում («sub»)</summary>
        public string TeacherUserId { get; set; } = "";

        /// <summary>
        /// true՝ երևում է շտեմարանի ցուցակում։ false՝ հին տարբերակ, որը թաքցված է շտեմարանից,
        /// բայց պահվում է, քանի որ արդեն տրված առաջադրանքները դեռ օգտագործում են այն։
        /// </summary>
        public bool IsInBank { get; set; } = true;

        /// <summary>Անվանում</summary>
        public string Title { get; set; } = "";

        /// <summary>Նկարագրություն (ոչ պարտադիր)</summary>
        public string? Description { get; set; }

        /// <summary>Առաջադրանքի տեքստը՝ ուսուցչի գրած առաջադրանքը (ոչ պարտադիր, կարելի է ուղարկել միայն կցված ֆայլով)</summary>
        public string? TaskText { get; set; }

        /// <summary>Առաջադրանքի տեսակները (դրոշակներ)</summary>
        public AssignmentTypes Types { get; set; }

        /// <summary>«Այլ» տեսակի ազատ տեքստը</summary>
        public string? OtherTypeText { get; set; }

        /// <summary>Գնահատման տեսակը</summary>
        public AssignmentGradingType GradingType { get; set; }

        /// <summary>Պատասխանի ձևերը՝ տեքստ և/կամ ֆայլ (դրոշակներ)</summary>
        public AssignmentResponseForms ResponseForms { get; set; }

        public string? MaterialUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<AssignmentSubject> Subjects { get; set; } = new();
        public List<AssignmentGradeLevel> GradeLevels { get; set; } = new();
        public List<AssignmentFile> Files { get; set; } = new();

        /// <summary>Այս բովանդակությամբ տրված առաջադրանքները</summary>
        public List<GivenAssignment> GivenAssignments { get; set; } = new();
    }
}
