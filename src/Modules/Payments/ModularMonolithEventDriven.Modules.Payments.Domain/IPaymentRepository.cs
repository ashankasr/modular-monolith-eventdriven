using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Payments.Domain;

public interface IPaymentRepository : IRepository<Payment, Guid>
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
