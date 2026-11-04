namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Օգտատիրոջ դերը խմբում
    /// </summary>
    public enum GroupMemberRole
    {
        /// <summary>Խմբի ստեղծող (ադմին)՝ կարող է խմբագրել և ջնջել</summary>
        Admin = 1,

        /// <summary>Խմբի անդամ</summary>
        Member = 2
    }
}
