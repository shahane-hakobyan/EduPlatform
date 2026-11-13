using EduPlatform.Helpers;
using EduPlatform.Resources;
using EduPlatform.ViewModels;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace EduPlatform.Services
{
    public interface ISidebarMenuBuilder
    {
        List<SidebarMenuItem> ForTeacher();
        List<SidebarMenuItem> FooterForTeacher();
        List<SidebarMenuItem> ForPupil();
        List<SidebarMenuItem> FooterForPupil();

        /// <summary>Մենյուն՝ ըստ մուտք գործած օգտատիրոջ դերի (Pupil → սովորողի, մնացածը՝ ուսուցչի)</summary>
        List<SidebarMenuItem> ForUser(ClaimsPrincipal user);
        List<SidebarMenuItem> FooterForUser(ClaimsPrincipal user);
    }

    public class SidebarMenuBuilder : ISidebarMenuBuilder
    {
        private readonly IStringLocalizer<SharedResource> _l;

        public SidebarMenuBuilder(IStringLocalizer<SharedResource> localizer)
        {
            _l = localizer;
        }

        private SidebarMenuItem PersonalPage(string url) =>
            new() { Label = _l["PersonalPage"], IconSvg = SidebarIcons.PersonalPage, Url = url };

        private SidebarMenuItem Groups(string url) =>
            new() { Label = _l["Groups"], IconSvg = SidebarIcons.Groups, Url = url };

        private SidebarMenuItem Logout() =>
            new() { Label = _l["Logout"], IconSvg = SidebarIcons.Logout, Url = "/Account/Logout" };
        private SidebarMenuItem Settings(string url) =>
            new() { Label = _l["Settings"], IconSvg = SidebarIcons.Settings, Url = url };
        private SidebarMenuItem MyLibrary(string baseUrl) => new()
        {
            Label = _l["MyLibrary"],
            IconSvg = SidebarIcons.MyLibrary,
            Children =
            {
                new() { Label = _l["Textbooks"],                  IconSvg = SidebarIcons.Textbooks,      Url = $"{baseUrl}/Materials/Textbooks" },
                new() { Label = _l["MethodologicalGuidelines"],    IconSvg = SidebarIcons.MethodologicalGuidelines,          Url = $"{baseUrl}/Materials/MethodologicalGuidelines" },
                new() { Label = _l["OtherEducationalMaterials"],   IconSvg = SidebarIcons.OtherMaterials,  Url = $"{baseUrl}/Materials/OtherEducationalMaterials" },
                new() { Label = _l["OtherBooks"],                     IconSvg = SidebarIcons.OtherBooks,         Url = $"{baseUrl}/OtherBooks" },
            }
        };

        public List<SidebarMenuItem> ForTeacher() => new() {
            PersonalPage("/Profile"),
            MyLibrary("/Library"),
            new() { Label = _l["MyBookshelf"], IconSvg = SidebarIcons.Repository, Children = {
                new() { Label = _l["MyTools"], IconSvg = SidebarIcons.Tools, Url = "/Repository/MyTools" },
                new() { Label = _l["MyCreatedMaterials"], IconSvg = SidebarIcons.CreatedMaterials, Url = "/Repository/MyCreatedMaterials" },
                new() { Label = _l["MyExpertise"], IconSvg = SidebarIcons.Expertise, Url = "/Soon/MyExpertise" },
                new() { Label = _l["MyAssignments"], IconSvg = SidebarIcons.Assignments, Children = {
                    new() { Label = _l["GivenAssignments"], IconSvg = SidebarIcons.Assignments, Url = "/Repository/Assignments" },
                    new() { Label = _l["AssignmentBank"], IconSvg = SidebarIcons.CreatedMaterials, Url = "/Repository/Assignments/Bank" },
                }},
            }},
            new() { Label = _l["StudentAssessment"], IconSvg = SidebarIcons.Assessment, Children = {
                new() { Label = _l["MyLogbook"], IconSvg = SidebarIcons.Journal, Url = "/Soon/MyLogbook" },
                new() { Label = _l["AIAssessment"], IconSvg = SidebarIcons.AIScore, Url = "/Soon/AIAssessment" },
            }},
            Groups("/Groups"),
            new() { Label = _l["Messages"], IconSvg = SidebarIcons.Messages, Url = "/Soon/Messages" },
            new() { Label = _l["AIChat"], IconSvg = SidebarIcons.AIChat, Url = "/AIChat" },
        };

        public List<SidebarMenuItem> FooterForTeacher() => new() {
            new() { Label = _l["MyStatistics"], IconSvg = SidebarIcons.Statistics, Url = "/Statistics" },
            Settings("/Soon/Settings"),
            Logout(),
        };

        // ── Սովորողի մենյու ───────────────────────────────────────────────
        public List<SidebarMenuItem> ForPupil() => new() {
            PersonalPage("/Profile"),
            MyLibrary("/Library"),
            new() { Label = _l["MyBookshelf"], IconSvg = SidebarIcons.Repository, Children = {
                new() { Label = _l["MyTools"], IconSvg = SidebarIcons.Tools, Url = "/Repository/MyTools" },
                new() { Label = _l["MyCreatedMaterials"], IconSvg = SidebarIcons.CreatedMaterials, Url = "/Repository/MyCreatedMaterials" },
            }},
            new() { Label = _l["Assignments"], IconSvg = SidebarIcons.Assignments, Url = "/Soon/Assignments" },
            new() { Label = _l["Assessment"], IconSvg = SidebarIcons.Assessment, Children = {
                new() { Label = _l["MyDiary"], IconSvg = SidebarIcons.Journal, Url = "/Soon/MyDiary" },
                new() { Label = _l["AIAssessment"], IconSvg = SidebarIcons.AIScore, Url = "/Soon/AIAssessment" },
            }},
            Groups("/Soon/Groups"),
            new() { Label = _l["Messages"], IconSvg = SidebarIcons.Messages, Url = "/Soon/Messages" },
            new() { Label = _l["AIChat"], IconSvg = SidebarIcons.AIChat, Url = "/Soon/AIChat" },
        };

        public List<SidebarMenuItem> FooterForPupil() => new() {
            new() { Label = _l["MyStatistics"], IconSvg = SidebarIcons.Statistics, Url = "/Statistics" },
            Settings("/Soon/Settings"),
            Logout(),
        };

        // ── Ըստ դերի ─────────────────────────────────────────────────────
        public List<SidebarMenuItem> ForUser(ClaimsPrincipal user) =>
            user.IsInRole("Pupil") ? ForPupil() : ForTeacher();

        public List<SidebarMenuItem> FooterForUser(ClaimsPrincipal user) =>
            user.IsInRole("Pupil") ? FooterForPupil() : FooterForTeacher();
    }
}

