using AutoMapper;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Roles;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RoleService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        var mapped = _mapper.Map<List<RoleDto>>(roles);
        return Result<List<RoleDto>>.Success(mapped);
    }
}
