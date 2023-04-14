using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly FoodsContext _foodContext;
        private IUserRepository _userRepository;
        public IUserRepository UserRepository
        {
            get
            {
                if(_userRepository == null)
                {
                    _userRepository = new UserRepository(_foodContext);
                }
                return _userRepository;
            }
        }

        //public IUserRepository UserRepository => throw new NotImplementedException();

        public RepositoryWrapper(FoodsContext repositoryContext)
        {
            _foodContext = repositoryContext;
        }

        public void Save()
        {
            _foodContext.SaveChanges();       
        }
    }
}
