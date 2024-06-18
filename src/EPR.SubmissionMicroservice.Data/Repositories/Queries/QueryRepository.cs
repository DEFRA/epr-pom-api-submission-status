using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using EPR.SubmissionMicroservice.Data.Entities;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Data.Repositories.Queries;

[ExcludeFromCodeCoverage]
public class QueryRepository<TEntity> : IQueryRepository<TEntity>
    where TEntity : EntityWithId
{
    private readonly SubmissionContext _context;

    public QueryRepository(SubmissionContext context)
    {
        _context = context;
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await GetAll(expression).AnyAsync(cancellationToken);
    }

    public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> expression)
    {
        return _context.Set<TEntity>().AsNoTracking().Where(expression);
    }

    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context
            .Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}