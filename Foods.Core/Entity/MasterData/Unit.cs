using System.Numerics;

namespace Foods.Core.Entity.MasterData
{
    public class Unit : BaseMasterDataEntity
    {
        public string? NickName { get; set; }
        public decimal BasicQuantity { get; set; }
        public bool BeDivided { get; set; }
    }
}