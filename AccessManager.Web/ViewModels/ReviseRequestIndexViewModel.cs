using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.UI.ViewModels;

public class ReviseRequestIndexViewModel
{
    public IReadOnlyList<ReviseRequest> Requests { get; set; } = new List<ReviseRequest>();
    public ReviseRequestStatus? FilterStatus { get; set; }
}
