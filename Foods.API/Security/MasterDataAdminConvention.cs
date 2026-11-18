namespace Foods.API.Security
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Foods.Domain.Entity.Security;

    public sealed class MasterDataAdminConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.Namespace?.Contains(".MasterData") == true)
                controller.Filters.Add(new AuthorizeFilter(new[] { new AuthorizeAttribute { Roles = RoleNames.Admin } }));
        }
    }
}
