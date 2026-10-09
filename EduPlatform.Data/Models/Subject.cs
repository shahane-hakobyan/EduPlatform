namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Դասավանդվող առարկա՝ «Մաթեմատիկա», «Ֆիզիկա»:
    /// </summary>
    /// <remarks>
    /// Առարկաները ընդհանուր են բոլոր հաստատությունների համար,
    /// ուստի անվանումը եզակի է ամբողջ համակարգում:
    /// </remarks>
    public class Subject
    {
        /// <summary>Առարկայի Id-ն բազայում</summary>
        public int Id { get; set; }

        /// <summary>Առարկայի անվանումը</summary>
        public string Name { get; set; } = "";
    }
}