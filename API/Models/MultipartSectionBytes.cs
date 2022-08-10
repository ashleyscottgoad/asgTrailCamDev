namespace API.Models
{
    public class MultipartSectionBytes
    {
        public MultipartSectionType SectionType { get; set; }
        public string Name { get; set; }
        public byte[] Bytes { get; set; }
    }


    public enum MultipartSectionType
    {
        File,
        Text
    }
}
