namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Աշակերտների խումբ՝ «9Ա», «10Բ»:
    /// </summary>
    /// <remarks>
    /// Ուշադրություն տերմինաբանությանը՝ <see cref="Grade"/>-ը աշակերտների խումբ է,
    /// իսկ <see cref="ScheduleLesson"/>-ը դասացուցակի մեկ դաս է:
    /// Անվանումը եզակի է միայն մեկ հաստատության շրջանակում. երկու տարբեր
    /// դպրոց կարող են երկուսն էլ ունենալ «9Ա»:
    /// </remarks>
    public class Grade
    {
        /// <summary>Դասարանի Id-ն բազայում</summary>
        public int Id { get; set; }

        /// <summary>Հաստատությունը, որին պատկանում է դասարանը</summary>
        public int InstitutionId { get; set; }

        /// <summary>Նավիգացիա դեպի <see cref="Institution"/>:</summary>
        public Institution Institution { get; set; } = null!;

        /// <summary>Դասարանի անվանումը՝ «9Ա», «10Բ»</summary>
        public string GradeName { get; set; } = "";
    }
}