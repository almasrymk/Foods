namespace Entity
{
    public class Location : BaseDataModel
    {
        public long DistrictId { get; set; }
        public long UserId { get; set; }
        public string? Longitude { get; set; }
        public string? Latitude { get; set; }
    }
}