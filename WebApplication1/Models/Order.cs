namespace WebApplication1.Models
{
    public class Order
    {
        public int vNo { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string grade { get; set; }
        public string director { get; set; }

        public System.Collections.Generic.List<VideoCopyDto> VideoCopies { get; set; } = new System.Collections.Generic.List<VideoCopyDto>();
    }
}