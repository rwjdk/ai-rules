using BlazorApp.Data;

namespace BlazorApp.BusinessLogic;

public sealed record TodoUpsertRequest(
    string Title,
    DateTime StartDate,
    DateTime DueDate,
    bool Completed);

public enum TodoCommandStatus
{
    Success,
    ValidationFailed,
    NotFound
}

public sealed record TodoCommandResult(
    TodoCommandStatus Status,
    TodoItem? Todo = null,
    IReadOnlyList<string>? Errors = null)
{
    public bool Succeeded => Status == TodoCommandStatus.Success;

    public static TodoCommandResult Success(TodoItem todo) =>
        new(TodoCommandStatus.Success, todo, []);

    public static TodoCommandResult Validation(IReadOnlyList<string> errors) =>
        new(TodoCommandStatus.ValidationFailed, null, errors);

    public static TodoCommandResult NotFound() =>
        new(TodoCommandStatus.NotFound, null, []);
}
