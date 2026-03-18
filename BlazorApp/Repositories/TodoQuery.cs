using BlazorApp.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp.Repositories;

public sealed class TodoQuery(IDbContextFactory<TodoDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using TodoDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.TodoItems
            .AsNoTracking()
            .OrderBy(todo => todo.Completed)
            .ThenBy(todo => todo.DueDate)
            .ThenBy(todo => todo.StartDate)
            .ThenBy(todo => todo.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using TodoDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.TodoItems
            .AsNoTracking()
            .SingleOrDefaultAsync(todo => todo.Id == id, cancellationToken);
    }
}
