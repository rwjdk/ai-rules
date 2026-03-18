using System.ComponentModel;
using BlazorApp.BusinessLogic;
using BlazorApp.Data;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace BlazorApp.MCP;

[McpServerToolType]
[PublicAPI]
public sealed class TodoMcpTools
{
    [McpServerTool(Name = McpToolNames.GetTasks), Description("Returns tasks from the Todo app. Completed tasks are excluded unless includeCompleted is true.")]
    public static async Task<IReadOnlyList<TodoMcpTask>> GetTasks(
        TodoService todoService,
        [Description("Set to true to include completed tasks in the result.")] bool includeCompleted = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TodoItem> todos = await todoService.GetAllAsync(cancellationToken);

        return todos
            .Where(todo => includeCompleted || !todo.Completed)
            .Select(MapTask)
            .ToArray();
    }

    [McpServerTool(Name = McpToolNames.AddTask), Description("Adds a new task to the Todo app.")]
    public static async Task<TodoMcpCommandResponse> AddTask(
        TodoService todoService,
        [Description("The task title.")] string title,
        [Description("Optional start date for the task.")] DateTime? startDate = null,
        [Description("Optional due date for the task.")] DateTime? dueDate = null,
        [Description("Set to true if the new task should be completed immediately.")] bool completed = false,
        CancellationToken cancellationToken = default)
    {
        TodoCommandResult result = await todoService.CreateAsync(
            new TodoUpsertRequest
            {
                Title = title,
                StartDate = startDate,
                DueDate = dueDate,
                Completed = completed
            },
            cancellationToken);

        return ToCommandResponse(result, successMessage: "Task created.");
    }

    [McpServerTool(Name = McpToolNames.UpdateTask), Description("Updates an existing task. Omitted fields keep their current values.")]
    public static async Task<TodoMcpCommandResponse> UpdateTask(
        TodoService todoService,
        [Description("The id of the task to update.")] int id,
        [Description("Optional replacement title. Leave empty to keep the current title.")] string? title = null,
        [Description("Optional replacement start date.")] DateTime? startDate = null,
        [Description("Set to true to clear the current start date.")] bool clearStartDate = false,
        [Description("Optional replacement due date.")] DateTime? dueDate = null,
        [Description("Set to true to clear the current due date.")] bool clearDueDate = false,
        [Description("Optional completed flag. Leave null to keep the current value.")] bool? completed = null,
        CancellationToken cancellationToken = default)
    {
        TodoItem? existingTodo = await todoService.GetByIdAsync(id, cancellationToken);

        if (existingTodo is null)
        {
            return new TodoMcpCommandResponse(false, $"Task {id} was not found.");
        }

        TodoCommandResult result = await todoService.UpdateAsync(
            id,
            new TodoUpsertRequest
            {
                Title = title ?? existingTodo.Title,
                StartDate = clearStartDate ? null : startDate ?? existingTodo.StartDate,
                DueDate = clearDueDate ? null : dueDate ?? existingTodo.DueDate,
                Completed = completed ?? existingTodo.Completed
            },
            cancellationToken);

        return ToCommandResponse(result, successMessage: "Task updated.");
    }

    [McpServerTool(Name = McpToolNames.RemoveTask), Description("Removes a task from the Todo app.")]
    public static async Task<TodoMcpDeleteResponse> RemoveTask(
        TodoService todoService,
        [Description("The id of the task to remove.")] int id,
        CancellationToken cancellationToken = default)
    {
        bool deleted = await todoService.DeleteAsync(id, cancellationToken);

        return deleted
            ? new TodoMcpDeleteResponse(true, "Task removed.", id)
            : new TodoMcpDeleteResponse(false, $"Task {id} was not found.", id);
    }

    private static TodoMcpCommandResponse ToCommandResponse(TodoCommandResult result, string successMessage)
    {
        if (result is { Succeeded: true, Todo: not null })
        {
            return new TodoMcpCommandResponse(true, successMessage, MapTask(result.Todo));
        }

        if (result.Status == TodoCommandStatus.ValidationFailed)
        {
            return new TodoMcpCommandResponse(false, "Task validation failed.", null, result.Errors ?? []);
        }

        return new TodoMcpCommandResponse(false, "Task was not found.");
    }

    private static TodoMcpTask MapTask(TodoItem todo) =>
        new(
            todo.Id,
            todo.Title,
            todo.StartDate,
            todo.DueDate,
            todo.Completed);
}
