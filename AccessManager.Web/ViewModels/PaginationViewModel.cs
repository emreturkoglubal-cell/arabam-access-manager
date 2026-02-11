using Microsoft.AspNetCore.Routing;

namespace AccessManager.UI.ViewModels;

/// <summary>Paylaşılan sayfalama (_Pagination) partial için model.</summary>
public class PaginationViewModel
{
    public string Controller { get; set; } = "";
    public string Action { get; set; } = "Index";
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    /// <summary>Sayfa linklerinde taşınacak route/query değerleri (filtreler, pageSize). 'page' partial içinde eklenir.</summary>
    public RouteValueDictionary RouteValues { get; set; } = new();
}
