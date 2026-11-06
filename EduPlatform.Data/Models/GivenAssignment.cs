namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Տրված (ուղարկված) առաջադրանք՝ միայն ուղարկման տվյալները։ Բովանդակությունը վերցվում է կապված
    /// շտեմարանի <see cref="Assignment"/>-ից, ուստի ոչինչ չի կրկնօրինակվում։
    /// Ուղարկվելուց հետո չի խմբագրվում (փոխվում է միայն վերջնաժամկետը, ավելանում են աշակերտներ, արխիվացվում է)։
    /// </summary>
    public class GivenAssignment
    {
        public int Id { get; set; }

        /// <summary>Շտեմարանի առաջադրանքը՝ բովանդակությունը</summary>
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        /// <summary>Ուսուցչի Id-ն AuthServer-ում («sub»)</summary>
        public string TeacherUserId { get; set; } = "";

        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }

        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Ստացող աշակերտները (կարգավիճակ, պատասխան, գնահատական)</summary>
        public List<AssignmentRecipient> Recipients { get; set; } = new();
    }
}
