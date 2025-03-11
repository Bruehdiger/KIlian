using Microsoft.FluentUI.AspNetCore.Components;

namespace KIlian.Dashboard.Features.Components.Extensions;

public static class FluentDataGridExtensions
{
    public static async Task RefreshAsync<T>(this FluentDataGrid<T>? grid)
    {
        if (grid is not null)
        {
            await grid.RefreshDataAsync();
        }
    }
}