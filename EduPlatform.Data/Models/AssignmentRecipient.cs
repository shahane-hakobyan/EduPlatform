namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Աշակերտը, որին ուղարկվել է առաջադրանքը՝ իր կարգավիճակով, պատասխանով և գնահատականով
    /// </summary>
    /// <remarks>
    /// Մեկ աշակերտը մեկ առաջադրանքի համար ունի ուղիղ մեկ տող՝ ապահովվում է
    /// եզակի ինդեքսով <c>(AssignmentId, PupilUserId)</c>:
    /// «Իմ խմբերը» ընտրելիս աշակերտները նույնպես ավելանում են այստեղ՝ առանձին:
    /// </remarks>
    public class AssignmentRecipient
    {
        public int Id { get; set; }

        public int GivenAssignmentId { get; set; }
        public GivenAssignment GivenAssignment { get; set; } = null!;

        /// <summary>Աշակերտի Id-ն AuthServer-ում («sub»)</summary>
        public string PupilUserId { get; set; } = "";

        /// <summary>Դասարանը (օր․ «9Ա»), որի միջոցով ուղարկվել է (ֆիլտրի համար)</summary>
        public int? GradeId { get; set; }
        public Grade? Grade { get; set; }

        /// <summary>Կարգավիճակ՝ Ուղարկված / Կատարված / Գնահատված</summary>
        public AssignmentRecipientStatus Status { get; set; } = AssignmentRecipientStatus.Sent;

        /// <summary>Երբ է ուղարկվել աշակերտին</summary>
        public DateTime SentAt { get; set; } = DateTime.Now;

        /// <summary>Երբ է աշակերտը ներկայացրել պատասխանը</summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>Աշակերտի տեքստային պատասխանը</summary>
        public string? AnswerText { get; set; }

        /// <summary>
        /// Միավորը՝ ընթացիկ միավորային՝ 1–10, հայտորոշիչ՝ «Նախնական միավոր» (ազատ թիվ), ձևավորող՝ դատարկ
        /// </summary>
        public decimal? Score { get; set; }

        /// <summary>Հայտորոշիչ գնահատման «Առավելագույն միավոր» (ազատ թիվ)։ Մնացած տեսակների համար՝ դատարկ</summary>
        public decimal? MaxScore { get; set; }

        /// <summary>Հետադարձ կապի տեքստ</summary>
        public string? Feedback { get; set; }

        /// <summary>Երբ է գնահատվել</summary>
        public DateTime? GradedAt { get; set; }

        /// <summary>Աշակերտի կցած ֆայլերը</summary>
        public List<AssignmentSubmissionFile> Files { get; set; } = new();
    }
}
