using Common.Cosmos;
using Microsoft.Azure.Cosmos.Spatial;

namespace API.Models
{
    public class RawImage: IEntity
    {
        public string id { get; set; }
        public string _etag { get; set; }        
        public Point? Location { get; set; }
    }
}