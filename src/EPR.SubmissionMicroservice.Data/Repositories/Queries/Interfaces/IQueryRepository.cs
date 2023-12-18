namespace EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

using System.Linq.Expressions;
using Entities;

public interface IQueryRepository<T>
    where T : EntityWithId
{
    IQueryable<T> GetAll(Expression<Func<T, bool>> expression);

    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}