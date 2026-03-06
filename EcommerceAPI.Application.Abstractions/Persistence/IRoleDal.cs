using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface IRoleDal : IEntityRepository<Role>
{
}
