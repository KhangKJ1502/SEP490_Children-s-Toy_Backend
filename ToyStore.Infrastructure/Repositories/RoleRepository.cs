using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly SEP490ToyStoreContext _context;

    public RoleRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .AsNoTracking()
            .OrderBy(x => x.RoleId)
            .ToListAsync(cancellationToken);
    }
}
