using Common.Cosmos;

namespace API.Models
{
    public class RawImage : IEntity
    {
        public string id { get; set; }
        public string _etag { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public SuggestedClassification SuggestedClassification { get; set; }

        //public Point? Location { get; set; }

        public RawImage(string hashedId, Dictionary<string, string> metadata)
        {
            id = hashedId;
            Metadata = metadata;
        }
    }

    public class SuggestedClassification
    {
        public string Label { get; set; }
        public float Score { get; set; }
    }
}