namespace Foods.Application.MasterData.Content.ContentCategory.Commands.Create
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Content;
    using Foods.Application.Abstraction.Command;
   
    public sealed class CreateCommandHandler(IRepository<ContentGroup> _IRepository) : ICommandHander<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {

            try
            {
                var ob = new ContentGroup { Id = new Guid(), Name = request.Name, IsDeleted = false, Status = 0 };
                var res = await _IRepository.CreateAsync(ob);
                if (await _IRepository.Commit())
                {

                    return new Result<CreateCommandResponse>();
                }
            }
            catch (Exception ex)
            {
            }

            return null;
        }
    }
}