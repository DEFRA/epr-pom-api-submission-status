namespace EPR.SubmissionMicroservice.Data.Repositories.Commands;

using System.Diagnostics.CodeAnalysis;
using Interfaces;

[ExcludeFromCodeCoverage]
public class CommandRepository<T> : ICommandRepository<T>
    where T : class
{
    private readonly SubmissionContext _context;

    public CommandRepository(SubmissionContext context)
    {
        _context = context;
    }

    public async Task AddAsync(T entity) => await _context.Set<T>().AddAsync(entity);

    public void Update(T entity) => _context.Set<T>().Update(entity);

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }
}