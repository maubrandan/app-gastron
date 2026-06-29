using Resto.Application.Common.Interfaces;
using Resto.Domain.Payments;

namespace Resto.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly RestoDbContext _context;

    public PaymentRepository(RestoDbContext context) => _context = context;

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        await _context.Payments.AddAsync(payment, cancellationToken);
}
