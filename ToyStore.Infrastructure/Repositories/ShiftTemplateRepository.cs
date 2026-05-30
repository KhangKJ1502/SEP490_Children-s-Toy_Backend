using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class ShiftTemplateRepository : IShiftTemplateRepository
{
    private readonly SEP490ToyStoreContext _context;

    public ShiftTemplateRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<List<ShiftTemplate>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return _context.ShiftTemplates
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.StartTime)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ShiftTemplate>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return _context.ShiftTemplates
            .AsNoTracking()
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.ShiftTemplateId)
            .ToListAsync(cancellationToken);
    }

    public Task<ShiftTemplate?> GetByIdAsync(byte shiftTemplateId, CancellationToken cancellationToken = default)
    {
        return _context.ShiftTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShiftTemplateId == shiftTemplateId, cancellationToken);
    }

    public Task<ShiftTemplate?> GetByIdForUpdateAsync(byte shiftTemplateId, CancellationToken cancellationToken = default)
    {
        return _context.ShiftTemplates
            .FirstOrDefaultAsync(x => x.ShiftTemplateId == shiftTemplateId, cancellationToken);
    }

    public Task<bool> HasOverlappingActiveTemplateAsync(
        TimeSpan startTime,
        TimeSpan endTime,
        byte? excludeShiftTemplateId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShiftTemplates
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (excludeShiftTemplateId.HasValue)
        {
            query = query.Where(x => x.ShiftTemplateId != excludeShiftTemplateId.Value);
        }

        return query.AnyAsync(
            x => x.StartTime < endTime && startTime < x.EndTime,
            cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string shiftName, CancellationToken cancellationToken = default)
    {
        var normalized = shiftName.Trim().ToLowerInvariant();
        return _context.ShiftTemplates
            .AsNoTracking()
            .AnyAsync(x => x.ShiftName.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> ExistsByNameExceptIdAsync(string shiftName, byte shiftTemplateId, CancellationToken cancellationToken = default)
    {
        var normalized = shiftName.Trim().ToLowerInvariant();
        return _context.ShiftTemplates
            .AsNoTracking()
            .AnyAsync(x => x.ShiftTemplateId != shiftTemplateId && x.ShiftName.ToLower() == normalized, cancellationToken);
    }

    public async Task<ShiftTemplate> CreateAsync(ShiftTemplate template, CancellationToken cancellationToken = default)
    {
        template.CreatedAt = DateTime.UtcNow;
        await _context.ShiftTemplates.AddAsync(template, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<ShiftTemplate> UpdateAsync(ShiftTemplate template, CancellationToken cancellationToken = default)
    {
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }
}
