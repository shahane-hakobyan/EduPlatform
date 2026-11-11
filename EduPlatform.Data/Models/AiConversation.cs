using System.Net.Mail;

namespace EduPlatform.Data.Models
{
    /// <summary>
    /// «ԱԲ զրուցարան»՝ օգտատիրոջ մեկ զրույց (նրա հաղորդագրությունների շարքը)։
    /// </summary>
    public class AiConversation
    {
        public int Id { get; set; }

        /// <summary>Զրույցի տերը (AuthServer-ի «sub»)</summary>
        public string UserId { get; set; } = "";

        public string Title { get; set; } = "";

        /// <summary>ԱԲ ագենտի բանալին (օր․ «math»), եթե զրույցը սկսվել է ագենտով</summary>
        public string? AgentKey { get; set; }

        /// <summary>Ամրացված զրույցները երևում են առանձին բաժնում՝ ցուցակի վերևում</summary>
        public bool IsPinned { get; set; }
        public DateTime? PinnedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<AiMessage> Messages { get; set; } = new();
    }
}
