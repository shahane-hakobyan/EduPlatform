using EduPlatform.ViewModels;
using Microsoft.Extensions.Localization;

namespace EduPlatform.Helpers
{
    public static class MaterialFilters
    {
        public static List<MaterialFilterOption> Standard(IStringLocalizer l) => new() {
            new() { Key = "subject", Label = l["Subject"] },
            new() { Key = "grade", Label = l["Grade"] },
            new() { Key = "author", Label = l["Author"] },
            new() { Key = "year", Label = l["PublicationYear"] },
            new() { Key = "publisher", Label = l["Publisher"] },
        };

        public static List<MaterialFilterOption> Other(IStringLocalizer l) =>
            Standard(l).Append(new() { Key = "type", Label = l["MaterialType"] }).ToList();

        public static List<MaterialFilterOption> LibraryTextbook(IStringLocalizer l) =>
            Standard(l).Append(new() { Key = "language", Label = l["Language"] }).ToList();

        public static List<MaterialFilterOption> LibraryFiction(IStringLocalizer l) => new() {
            new() { Key = "author", Label = l["Author"] },
            new() { Key = "year", Label = l["PublicationYear"] },
            new() { Key = "publisher", Label = l["Publisher"] },
            new() { Key = "language", Label = l["Language"] },
        };
    }
}