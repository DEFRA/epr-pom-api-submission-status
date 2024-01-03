using System.Linq.Expressions;
using EPR.SubmissionMicroservice.Data.Entities;

namespace EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

public interface IQueryRepository<T>
    where T : EntityWithId
{
    Task<bool> AnyAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);

    IQueryable<T> GetAll(Expression<Func<T, bool>> expression);

    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}