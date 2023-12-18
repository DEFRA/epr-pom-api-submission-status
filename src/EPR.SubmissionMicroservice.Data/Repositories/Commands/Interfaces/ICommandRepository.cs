namespace EPR.SubmissionMicroservice.Data.Repositories.Commands.Interfaces;

public interface ICommandRepository<T>
    where T : class
{
    Task AddAsync(T entity);

    void Update(T entity);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}