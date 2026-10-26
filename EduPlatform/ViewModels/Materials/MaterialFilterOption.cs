namespace EduPlatform.ViewModels
{
    public class MaterialFilterOption
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        /// <summary>Ընտրելի արժեքները (կազմվում են էջի նյութերից):</summary>
        public List<string> Options { get; set; } = new();
    }
}
