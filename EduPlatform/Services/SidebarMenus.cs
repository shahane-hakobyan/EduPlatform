using EduPlatform.Models;
using EduPlatform.Resources;
using Microsoft.Extensions.Localization;

namespace EduPlatform.Services
{
    public interface ISidebarMenuBuilder
    {
        List<SidebarMenuItem> ForTeacher();
    }

    public class SidebarMenuBuilder : ISidebarMenuBuilder
    {
        private readonly IStringLocalizer<SharedResource> _l;

        public SidebarMenuBuilder(IStringLocalizer<SharedResource> localizer)
        {
            _l = localizer;
        }

        // ── Shared building blocks ──────────────────────────────────────────
        private SidebarMenuItem PersonalPage(string url) =>
            new() { Label = _l["PersonalPage"], IconSvg = SidebarIcons.PersonalPage, Url = url };

        private SidebarMenuItem Groups(string url) =>
            new() { Label = _l["Groups"], IconSvg = SidebarIcons.Groups, Url = url };

        private SidebarMenuItem Logout() =>
            new() { Label = _l["Logout"], IconSvg = SidebarIcons.Logout, Url = "/Account/Logout" };

        private SidebarMenuItem AvailableMaterials(string baseUrl) => new()
        {
            Label = _l["AvailableMaterials"],
            IconSvg = SidebarIcons.AvailableMaterials,
            Children =
            {
                new() { Label = _l["Textbooks"],                  IconSvg = SidebarIcons.Textbooks,      Url = $"{baseUrl}/Materials/Textbooks" },
                new() { Label = _l["MethodologicalGuidelines"],    IconSvg = SidebarIcons.Guides,          Url = $"{baseUrl}/Materials/Guides" },
                new() { Label = _l["OtherEducationalMaterials"],   IconSvg = SidebarIcons.OtherMaterials,  Url = $"{baseUrl}/Materials/Other" },
                new() { Label = _l["Library"],                     IconSvg = SidebarIcons.Library,         Url = $"{baseUrl}/Library" },
            }
        };

        // ── Teacher composition ──────────────────────────────────────────
        public List<SidebarMenuItem> ForTeacher() => new()
        {
            PersonalPage("/Teacher"),
            AvailableMaterials("/Teacher"),
            new()
            {
                Label = _l["MyBookshelf"], IconSvg = SidebarIcons.Repository,
                Children =
                {
                    new() { Label = _l["MyTools"],             IconSvg = SidebarIcons.Tools,            Url = "/Teacher/Repository/Tools" },
                    new() { Label = _l["MyCreatedMaterials"],  IconSvg = SidebarIcons.CreatedMaterials, Url = "/Teacher/Repository/CreatedMaterials" },
                    new() { Label = _l["MyExpertise"],         IconSvg = SidebarIcons.Expertise,        Url = "/Teacher/Repository/Expertise" },
                    new() { Label = _l["Assignments"],         IconSvg = SidebarIcons.Assignments,      Url = "/Teacher/Repository/Assignments" },
                }
            },
            new()
            {
                Label = _l["StudentAssessment"], IconSvg = SidebarIcons.Assessment,
                Children =
                {
                    new() { Label = _l["MyLogbook"],   IconSvg = SidebarIcons.Journal, Url = "/Teacher/Assessment/Journal" },
                    new() { Label = _l["AIAssessment"], IconSvg = SidebarIcons.AbScore, Url = "/Teacher/Assessment/AbScore" },
                }
            },
            Groups("/Teacher/Groups"),
            new() { Label = _l["AIChat"],        IconSvg = SidebarIcons.AbChat,     Url = "/Teacher/AbChat" },
            new() { Label = _l["MyStatistics"],  IconSvg = SidebarIcons.Statistics, Url = "/Teacher/Statistics" },
            Logout(),
        };
    }
}