namespace IjazahService.Models
{
    public class Ijazah
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
