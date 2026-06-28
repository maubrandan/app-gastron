using Resto.Domain.Tables;

namespace Resto.Application.Common.Interfaces;

public interface ITableRepository
{
    Task<Table?> GetByNumberAsync(int number, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Table>> GetAllAsync(CancellationToken cancellationToken = default);
}
