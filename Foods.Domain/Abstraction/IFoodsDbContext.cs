using Foods.Domain.Entity.MasterData.Content;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Foods.Domain.Abstraction
{
    public interface IFoodsDbContext<TContext> : IDisposable , IDbContextFactory<TContext> where TContext : DbContext
    {
       
    }
}