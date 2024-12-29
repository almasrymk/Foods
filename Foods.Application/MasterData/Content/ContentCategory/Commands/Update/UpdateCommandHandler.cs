namespace Foods.Application.MasterData.Content.ContentCategory.Commands.Update
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Content;
    using Foods.Application.MasterData.Content.ContentCategory.Commands.Create;

    public sealed class UpdateCommandHandler(IUnitOfWork<ContentGroup> _IRepository) : ICommandHander<UpdateCommand, UpdateCommandResponse>
    {
        public async Task<Result<UpdateCommandResponse>> Handle(UpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ob = await _IRepository._Repository.GetByFilterAsync(e => e.Id == request.Id);
                if (ob == null)
                    return null;

                ob.Name = request.Name;
                
                var res = await _IRepository._Repository.UpdateAsync(ob);
                if (await _IRepository.SaveChangeAsync() > 0)
                {
                    return new Result<UpdateCommandResponse>();
                }
            }
            catch (Exception ex)
            {
            }

            return null;
        }
    }
}