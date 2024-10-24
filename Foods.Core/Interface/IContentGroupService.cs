namespace Foods.Core.Interface
{
    using Foods.Core.Entity.MasterData.Content;
    using System.Linq.Expressions;

    public interface IContentGroupService
    {
        ValueTask<ContentGroup> Save(ContentGroup contentGroup);
        ValueTask<bool> Delete(Expression<Func<ContentGroup, bool>> Filter);
        ValueTask<ContentGroup?> GetByFilter(Expression<Func<ContentGroup, bool>> Filter);
        ValueTask<IEnumerable<ContentGroup>?> GetListByFilter();
        ValueTask<IEnumerable<ContentGroup>?> GetListByFilter(Expression<Func<ContentGroup, bool>> Filter);
    }
}