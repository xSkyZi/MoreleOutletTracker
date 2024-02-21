namespace MoreleOutletTracker.MoreleTracker.PostGetJsonTemplates
{
    public class MorelePostRequestData
    {
        public int limit { get; set; }
        public bool isOutlet { get; set; }
        public string[]? categories { get; set; }
    }
}
