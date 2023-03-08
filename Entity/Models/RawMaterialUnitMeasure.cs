namespace Entity
{
    public class RawMaterialUnitMeasure : BaseDataModel
    {
        public long RawMaterialId { get; set; }
        public long UnitMeasureId { get; set; }
        public decimal DefaultQuentiy { get; set; }
        public bool CanDivided { get; set; }
        public decimal Rate { get; set; }
    }
}