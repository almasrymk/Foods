namespace Foods.Domain
{
    public abstract class BaseEntity
    { 
        //protected BaseEntity(Guid id, bool isDeleted, int status)
        //{
        //    Id = id;
        //    IsDeleted = isDeleted;
        //    Status = status;
        //}

        public override bool Equals(object? obj)
        {
            if(obj == null) return false;   
            if(obj.GetType() != GetType()) return false;  
            if(obj is not BaseEntity entity) return false;
            return Id == entity.Id;
        }

        public virtual Guid Id { get; private init; }
        public bool IsDeleted { get; set; }
        public int Status { get; set; }
    }
}