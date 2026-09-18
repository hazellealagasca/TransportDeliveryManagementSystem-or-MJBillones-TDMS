namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models
{
    public class GlobalSearchResult
    {
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string TargetModule { get; set; } = "";
        public int RecordId { get; set; }
        public int Tag { get => RecordId; set => RecordId = value; }

        public override string ToString()
        {
            return $"[{Category}] {Title} - {Subtitle}";
        }
    }
}
