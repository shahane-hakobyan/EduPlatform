namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Ուսուցչի կցած ֆայլ։ Մեկ ֆայլ՝ մեկ գրառում, որը կապվում է մի քանի առաջադրանքի հետ
    /// (շտեմարանի օրինակ + ուղարկված առաջադրանք)՝ <c>AssignmentFileLinks</c> աղյուսակի միջոցով
    /// </summary>
    /// <remarks>
    /// Ֆայլի բովանդակությունը պահվում է սերվերի ֆայլային պահոցում, իսկ բազայում՝
    /// միայն տվյալները: <see cref="StoredFileName"/>-ը սերվերի վրա եզակի անունն է
    /// (ոչ թե օգտատիրոջ տված անունը՝ անվտանգության համար):
    /// </remarks>
    public class AssignmentFile
    {
        public int Id { get; set; }


        /// <summary>Այն առաջադրանքները, որոնց կցված է այս ֆայլը</summary>
        public List<Assignment> Assignments { get; set; } = new();


        /// <summary>Ֆայլի սկզբնական անունը (ցուցադրելու համար)</summary>
        public string FileName { get; set; } = "";

        /// <summary>Սերվերի վրա պահված եզակի անունը</summary>
        public string StoredFileName { get; set; } = "";

        public string ContentType { get; set; } = "";

        public long SizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
