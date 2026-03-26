using MediatR;
using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Common.Application.Abstractions;

public interface ICommand : IRequest<Result> { }

public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
