using Common.Cosmos;
using Microsoft.Azure.Cosmos.Spatial;

namespace API.Models
{
    public class RawImage: IEntity
    {
        public string id { get; set; }
        public string _etag { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public string SuggestedClassification { get; set; }
        //public Point? Location { get; set; }

        public RawImage(string hashedId, Dictionary<string, string> metadata)
        {
            id = hashedId;
            Metadata = metadata;
        }

    }
}