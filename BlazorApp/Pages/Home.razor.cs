using System.Linq;
using System.ComponentModel.DataAnnotations;
using BlazorApp.Repositories;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using BlazorApp.Data;

namespace BlazorApp.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    private TodoQuery TodoQuery { get; set; } = default!;

    [Inject]
    private TodoCommand TodoCommand { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private MudDataGrid<TodoItem>? TodoGrid { get; set; }

    private List<TodoItem> Todos { get; set; } = [];

    private TodoItem? PendingNewTodo { get; set; }

    private bool IsLoading { get; set; }

    private bool IsSaving { get; set; }

    private DialogOptions TodoDialogOptions { get; } =
        new()
        {
            CloseButton = true,
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

    private int TotalCount => Todos.Count;

    private int OpenCount => Todos.Count(todo => !todo.Completed);

    private int CompletedCount => Todos.Count(todo => todo.Completed);

    protected override async Task OnInitializedAsync() => await LoadTodosAsync();

    private async Task LoadTodosAsync()
    {
        IsLoading = true;

        try
        {
            Todos = (await TodoQuery.GetAllAsync()).ToList();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenCreateDialogAsync()
    {
        if (TodoGrid is null)
        {
            return;
        }

        if (PendingNewTodo is not null)
        {
            await TodoGrid.SetEditingItemAsync(PendingNewTodo);
            return;
        }

        PendingNewTodo =
            new TodoItem
            {
                StartDate = DateTime.Today,
                DueDate = DateTime.Today
            };

        Todos.Insert(0, PendingNewTodo);
        await InvokeAsync(StateHasChanged);
        await TodoGrid.SetEditingItemAsync(PendingNewTodo);
    }

    private async Task OpenEditDialogAsync(TodoItem todo)
    {
        if (TodoGrid is null)
        {
            return;
        }

        await TodoGrid.SetEditingItemAsync(todo);
    }

    private async Task<DataGridEditFormAction> CommitItemChangesAsync(TodoItem item)
    {
        if (!TryValidateTodo(item, out string validationMessage))
        {
            Snackbar.Add(validationMessage, Severity.Error);
            return DataGridEditFormAction.KeepOpen;
        }

        IsSaving = true;

        try
        {
            item.Title = item.Title.Trim();

            if (item.Id == 0)
            {
                await TodoCommand.AddAsync(item);
                Snackbar.Add("Task created.", Severity.Success);
            }
            else
            {
                bool updated = await TodoCommand.UpdateAsync(item);

                if (!updated)
                {
                    Snackbar.Add("That task no longer exists.", Severity.Warning);
                    PendingNewTodo = null;
                    await LoadTodosAsync();
                    return DataGridEditFormAction.Close;
                }

                Snackbar.Add("Task updated.", Severity.Success);
            }

            PendingNewTodo = null;
            await LoadTodosAsync();
            return DataGridEditFormAction.Close;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task ToggleCompletedAsync(TodoItem todo)
    {
        TodoItem updatedTodo =
            new()
            {
                Id = todo.Id,
                Title = todo.Title,
                StartDate = todo.StartDate,
                DueDate = todo.DueDate,
                Completed = !todo.Completed
            };

        bool updated = await TodoCommand.UpdateAsync(updatedTodo);

        if (!updated)
        {
            Snackbar.Add("That task could not be updated.", Severity.Warning);
            await LoadTodosAsync();
            return;
        }

        await LoadTodosAsync();
        Snackbar.Add(updatedTodo.Completed ? "Task marked completed." : "Task reopened.", Severity.Normal);
    }

    private async Task DeleteTodoAsync(int id, string title)
    {
        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete task",
            $"Delete '{title}' from the TodoApp database?",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        bool deleted = await TodoCommand.DeleteAsync(id);

        if (!deleted)
        {
            Snackbar.Add("That task was already removed.", Severity.Info);
            await LoadTodosAsync();
            return;
        }

        if (PendingNewTodo is not null && PendingNewTodo.Id == id)
        {
            PendingNewTodo = null;
        }

        await LoadTodosAsync();
        Snackbar.Add("Task deleted.", Severity.Warning);
    }

    private Task HandleCanceledEditingItem(TodoItem item)
    {
        if (PendingNewTodo is not null && ReferenceEquals(item, PendingNewTodo))
        {
            Todos.Remove(PendingNewTodo);
            PendingNewTodo = null;
            return InvokeAsync(StateHasChanged);
        }

        return Task.CompletedTask;
    }

    private static bool TryValidateTodo(TodoItem item, out string validationMessage)
    {
        List<ValidationResult> validationResults = [];
        ValidationContext validationContext = new(item);
        bool isValid = Validator.TryValidateObject(item, validationContext, validationResults, validateAllProperties: true);

        if (isValid)
        {
            validationMessage = string.Empty;
            return true;
        }

        validationMessage = string.Join(" ", validationResults
            .Select(result => result.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message)));

        return false;
    }
}
