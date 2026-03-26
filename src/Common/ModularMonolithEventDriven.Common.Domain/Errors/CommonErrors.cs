using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Common.Domain.Errors;

public static class CommonErrors
{
    public static Error NotFound(string entity, Guid id) =>
        Error.NotFound($"{entity}.NotFound", $"{entity} with id '{id}' was not found.");

    public static Error Conflict(string entity) =>
        Error.Conflict($"{entity}.Conflict", $"{entity} already exists.");
}
