using System.Numerics;

namespace Foods.Domain.Entity.MasterData
{
    public class Unit : BaseMasterDataEntity
    {
        public string? NickName { get; set; }
        public decimal BasicQuantity { get; set; }
        public bool BeDivided { get; set; }
        public string? IconPath { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
    }
}