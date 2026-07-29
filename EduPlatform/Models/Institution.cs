namespace EduPlatform.Models
{
    /// <summary>
    /// Ուսումնական հաստատություն՝ դպրոց, ՆՄՈՒՀ կամ ՄՄՈՒՀ:
    /// </summary>
    /// <remarks>
    /// Պահում ենք միայն այն տվյալները, որոնք պետք են EduPlatform-ին:
    /// Մնացածը մնում է արտաքին համակարգերում և կապվում է
    /// արտաքին Id-ի միջոցով:
    /// </remarks>
    public class Institution
    {
        /// <summary>Հաստատության Id-ն բազայում</summary>
        public int Id { get; set; }

        /// <summary>Հաստատության Id-ն ԿՏԱԿ արտաքին համակարգում</summary>
        public string KtakInstitutionId { get; set; } = "";

        /// <summary>Հաստատության Id-ն  ԴՇՀ (AuthServer) համակարգում</summary>
        /// <remarks>Դատարկ է մինչև AuthServer-ի հետ ինտեգրումը</remarks>
        public string? DshhInstitutionId { get; set; }

        /// <summary>Հաստատության անվանումը</summary>
        public string Name { get; set; } = "";

        /// <summary>Հաստատության տեսակը՝ դպրոց, ՆՄՈՒՀ կամ ՄՄՈՒՀ</summary>
        public Place Place { get; set; }

        /// <summary>Այս հաստատությանը պատկանող բոլոր դասարանները</summary>
        /// <remarks>
        /// Հաստատությունը ջնջելիս ջնջվում են նաև իր դասարանները (Cascade)
        /// </remarks>
        public List<Grade> Grades { get; set; } = new();
    }
}