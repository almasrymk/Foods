namespace Entity
{
    public class Occasion : BaseDataModel
    {
        public long JobId { get; set; }
        public long CountryId { get; set; }
        public long GenderId { get; set; }
        public long ReligionId { get; set; }
        public int FromMonth { get; set; }
        public int ToMonth { get; set; }
        public int FromDay { get; set; }
        public int DayDay { get; set; }
    }
}