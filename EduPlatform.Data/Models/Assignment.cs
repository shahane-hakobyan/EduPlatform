namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Առաջադրանք: նույն աղյուսակում պահվում են երկու տեսակի տողեր՝
    /// շտեմարանի ձևանմուշը և աշակերտներին ուղարկված առաջադրանքը:
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ձևանմուշ</b> (<see cref="IsTemplate"/> = <c>true</c>)՝ «Առաջադրանքների շտեմարան»-ի
    /// առաջադրանք: Կարող է խմբագրվել և կրկին ուղարկվել: Ամսաթվեր և ստացողներ չունի:
    /// </para>
    /// <para>
    /// <b>Ուղարկված առաջադրանք</b> (<see cref="IsTemplate"/> = <c>false</c>)՝ «Տրված առաջադրանքներ»-ում
    /// երևացողը: Բովանդակությունը ձևանմուշից <b>պատճենվում է</b> ուղարկելու պահին,
    /// և հետո այլևս չի փոխվում (որպեսզի արդեն կատարած աշակերտների աշխատանքը չկորչի):
    /// Փոխվել կարող են միայն վերջնաժամկետը և ստացողների ցանկը:
    /// </para>
    /// </remarks>
    public class Assignment
    {
        /// <summary>Առաջադրանքի Id-ն բազայում</summary>
        public int Id { get; set; }

        /// <summary>Ուսուցչի Id-ն AuthServer-ում («sub»)</summary>
        public string TeacherUserId { get; set; } = "";

        /// <summary><c>true</c>՝ շտեմարանի ձևանմուշ; <c>false</c>՝ ուղարկված առաջադրանք</summary>
        public bool IsTemplate { get; set; }

        /// <summary>Ուղարկված առաջադրանքի ձևանմուշը (եթե գիտենք), որից այն ստեղծվել է</summary>
        public int? TemplateId { get; set; }

        /// <summary>Նավիգացիա դեպի ձևանմուշը</summary>
        public Assignment? Template { get; set; }

        /// <summary>Անվանում</summary>
        public string Title { get; set; } = "";

        /// <summary>Նկարագրություն (ոչ պարտադիր)</summary>
        public string? Description { get; set; }

        /// <summary>Առաջադրանքի տեսակները (դրոշակներ)</summary>
        public AssignmentTypes Types { get; set; }

        /// <summary>«Այլ» տեսակի տեքստը (երբ <see cref="Types"/>-ում կա <see cref="AssignmentTypes.Other"/>)</summary>
        public string? OtherTypeText { get; set; }

        /// <summary>Գնահատման տեսակը</summary>
        public AssignmentGradingType GradingType { get; set; }

        /// <summary>Պատասխանի ներկայացման ձևերը (դրոշակներ)</summary>
        public AssignmentResponseForms ResponseForms { get; set; }

        /// <summary>Երբ է առաջադրանքը ուղարկվում սովորողներին (ձևանմուշի դեպքում՝ դատարկ)</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>Մինչև երբ կարող են սովորողները կատարել (ձևանմուշի դեպքում՝ դատարկ)</summary>
        public DateTime? DueDate { get; set; }

        /// <summary><c>true</c>՝ արխիվացված (ուսուցիչն այլևս չի աշխատում դրա վրա)</summary>
        public bool IsArchived { get; set; }

        /// <summary>Արխիվացման պահը</summary>
        public DateTime? ArchivedAt { get; set; }

        /// <summary>
        /// Նյութի հղումը («Իմ ստեղծած նյութերը»-ից կամ Երաշխավորված նյութերից «Տալ առաջադրանք»-ով ստեղծելիս)
        /// </summary>
        public string? MaterialUrl { get; set; }

        /// <summary>Ստեղծման ամսաթիվը (շտեմարանի ֆիլտրի համար)</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Վերջին փոփոխության ամսաթիվը</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Առարկաները (մեկից ավելի)</summary>
        public List<AssignmentSubject> Subjects { get; set; } = new();

        /// <summary>Դասարանների մակարդակները 1–12 (մեկից ավելի)</summary>
        public List<AssignmentGradeLevel> GradeLevels { get; set; } = new();

        /// <summary>Առաջադրանքի համար կցված ֆայլերը</summary>
        public List<AssignmentFile> Files { get; set; } = new();

        /// <summary>Ստացողները (միայն ուղարկված առաջադրանքի համար)</summary>
        public List<AssignmentRecipient> Recipients { get; set; } = new();
    }
}
