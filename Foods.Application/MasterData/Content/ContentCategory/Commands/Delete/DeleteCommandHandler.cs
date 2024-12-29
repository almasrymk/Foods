namespace Foods.Application.MasterData.Content.ContentCategory.Commands.Delete
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Content;

    public sealed class DeleteCommandHandler(IUnitOfWork<ContentGroup> _IRepository) : ICommandHander<DeleteCommand, bool>
    {
        public async Task<Result<bool>> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ob = await _IRepository._Repository.GetByFilterAsync(e => e.Id == request.Id);
                if (ob == null)
                    return null;
                 

                var res = await _IRepository._Repository.DeleteAsync(e => e.Id == request.Id);
                if (await _IRepository.SaveChangeAsync() > 0)
                {
                    return new Result<bool>();
                }
            }
            catch (Exception ex)
            {
            }

            return null;
        }
    }
}