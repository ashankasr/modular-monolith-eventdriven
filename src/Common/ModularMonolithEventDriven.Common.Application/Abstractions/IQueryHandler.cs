using MediatR;
using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Common.Application.Abstractions;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
