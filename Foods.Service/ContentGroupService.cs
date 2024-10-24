namespace Foods.Service
{
    using Foods.Core.Interface;
    using System.Threading.Tasks;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Foods.Core.Entity.MasterData.Content;

    public class ContentGroupService(IRepository<ContentGroup> repository) : IContentGroupService
    {
        private readonly IRepository<ContentGroup> _repository = repository;

        public ValueTask<ContentGroup> Save(ContentGroup contentGroup)
        {
            return Save(contentGroup, _repository);
        }

        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "<Pending>")]
        public ValueTask<ContentGroup> Save(ContentGroup contentGroup, IRepository<ContentGroup> _repository)
        {
            ValueTask<ContentGroup> ob = new(contentGroup);
            if (string.IsNullOrEmpty("" + contentGroup.Id))
                ob = _repository.CreateAsync(contentGroup);
            else
                _repository.UpdateAsync(contentGroup);
            return ob;
        }

        public ValueTask<bool> Delete(Expression<Func<ContentGroup, bool>> Filter)
        {
            return _repository.DeleteAsync(Filter);
        }

        public ValueTask<ContentGroup?> GetByFilter(Expression<Func<ContentGroup, bool>> Filter)
        {
            return _repository.GetByFilterAsync(Filter);
        }

        public ValueTask<IEnumerable<ContentGroup>?> GetListByFilter()
        {
            return _repository.GetListByFilterAsync();
        }

        public ValueTask<IEnumerable<ContentGroup>?> GetListByFilter(Expression<Func<ContentGroup, bool>>? Filter = null)
        {
            return _repository.GetListByFilterAsync(Filter);
        }
    }
}