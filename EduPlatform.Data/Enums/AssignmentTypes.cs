namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Առաջադրանքի տեսակը (կարելի է ընտրել մեկից ավելի՝ դրոշակներով)
    /// </summary>
    /// <remarks>
    /// Պահվում է մեկ թվով (օր․՝ Test | Homework = 17):
    /// «Այլ» ընտրելիս տեքստը պահվում է <see cref="Assignment.OtherTypeText"/>-ում:
    /// </remarks>
    [Flags]
    public enum AssignmentTypes
    {
        None = 0,

        /// <summary>Թեստային</summary>
        Test = 1,

        /// <summary>Գործնական</summary>
        Practical = 2,

        /// <summary>Լաբորատոր</summary>
        Laboratory = 4,

        /// <summary>Նախագծային</summary>
        Project = 8,

        /// <summary>Տնային աշխատանք</summary>
        Homework = 16,

        /// <summary>Այլ (տեքստը՝ <see cref="Assignment.OtherTypeText"/>)</summary>
        Other = 32
    }
}
