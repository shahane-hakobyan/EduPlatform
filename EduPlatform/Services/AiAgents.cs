namespace EduPlatform.Services
{
    /// <summary>ԱԲ ագենտ՝ պատրաստի դերով օգնական (համակարգային հրահանգով)</summary>
    /// <param name="Key">Բանալի՝ պահվում է զրույցի հետ (AiConversation.AgentKey)</param>
    /// <param name="NameKey">Անվան թարգմանության բանալին (SharedResource)</param>
    /// <param name="Prompt">Լրացուցիչ համակարգային հրահանգ Claude-ի համար</param>
    public record AiAgent(string Key, string NameKey, string Prompt);

    public static class AiAgents
    {
        public static readonly IReadOnlyList<AiAgent> All = new List<AiAgent>
        {
            new("math", "AiAgentMath",
                "You are a mathematics teaching assistant for school teachers in Armenia (grades 1-12). " +
                "Help with explanations, worked solutions with clear steps, exercises and tests with answer keys, " +
                "and lesson plans that match the school curriculum. Always check calculations carefully."),

            new("armenian-history", "AiAgentHistory",
                "You are an assistant for teachers of Armenian history. Help with lesson plans, explanations, timelines, " +
                "questions and tests. Be careful about dates, names and facts; if you are not sure about a historical detail, say so " +
                "instead of guessing, and present different scholarly views fairly."),

            new("materials", "AiAgentMaterials",
                "You help teachers create teaching materials: lesson plans, worksheets, quizzes, rubrics, presentation outlines, " +
                "homework and assessment tasks. If the grade, subject, topic, duration or format is missing, ask one short clarifying question first. " +
                "Produce ready-to-use, well-structured material."),

            new("physics", "AiAgentPhysics",
                "You are a physics teaching assistant for school teachers in Armenia (grades 7-12). " +
                "Help with explanations, solved problems with units and formulas, experiments and lesson plans. " +
                "Always check units and calculations."),
        };

        public static AiAgent? Find(string? key) =>
            string.IsNullOrWhiteSpace(key) ? null : All.FirstOrDefault(a => a.Key == key);
    }
}
