namespace Common.Cosmos
{
    public interface IEntity
    {
        public string id { get; set; }
        public string _etag { get; set; }
    }
}