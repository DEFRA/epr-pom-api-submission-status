namespace EPR.SubmissionMicroservice.Data.Repositories.Queries;

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Entities;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

[ExcludeFromCodeCoverage]
public class QueryRepository<TEntity> : IQueryRepository<TEntity>
    where TEntity : EntityWithId
{
    private readonly SubmissionContext _context;

    public QueryRepository(SubmissionContext context)
    {
        _context = context;
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