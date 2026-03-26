using MediatR;
using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Common.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
