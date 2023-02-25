namespace Entity
{
    using Utility;
    public class BaseModel
    {
        public virtual long Id  { get; set; }                
        public Status Status { get; set; }
    }
}