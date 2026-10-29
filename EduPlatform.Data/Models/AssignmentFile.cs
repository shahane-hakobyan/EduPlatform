namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Առաջադրանքի համար ուսուցչի կցած ֆայլ (մեկ առաջադրանքին՝ մի քանի ֆայլ)
    /// </summary>
    /// <remarks>
    /// Ֆայլի բովանդակությունը պահվում է սերվերի ֆայլային պահոցում, իսկ բազայում՝
    /// միայն տվյալները: <see cref="StoredFileName"/>-ը սերվերի վրա եզակի անունն է
    /// (ոչ թե օգտատիրոջ տված անունը՝ անվտանգության համար):
    /// </remarks>
    public class AssignmentFile
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        /// <summary>Ֆայլի սկզբնական անունը (ցուցադրելու համար)</summary>
        public string FileName { get; set; } = "";

        /// <summary>Սերվերի վրա պահված եզակի անունը</summary>
        public string StoredFileName { get; set; } = "";

        public string ContentType { get; set; } = "";

        public long SizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
