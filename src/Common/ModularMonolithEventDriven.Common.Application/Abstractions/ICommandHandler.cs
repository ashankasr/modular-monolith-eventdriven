using MediatR;
using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Common.Application.Abstractions;

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
