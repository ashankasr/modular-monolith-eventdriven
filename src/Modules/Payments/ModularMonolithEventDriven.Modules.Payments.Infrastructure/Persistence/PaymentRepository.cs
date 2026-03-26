using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Payments.Domain;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentRepository(PaymentsDbContext context)
    : BaseRepository<Payment, Guid, PaymentsDbContext>(context), IPaymentRepository
{
    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await Context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
}
