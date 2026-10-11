namespace ShelfOps.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task ExecuteTransactionAsync(Func<Task> action, CancellationToken ct = default);
    }
}