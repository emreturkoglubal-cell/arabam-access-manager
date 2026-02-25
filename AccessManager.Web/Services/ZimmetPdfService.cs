using AccessManager.Domain.Entities;
using AccessManager.UI.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AccessManager.UI.Services;

/// <summary>
/// Zimmet belgesi PDF oluşturur: donanım ve zimmet bilgileri, yönetici ve zimmete alan kişi adları ile imza alanları.
/// </summary>
public class ZimmetPdfService
{
    static ZimmetPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Zimmet bilgilerini içeren PDF belgesi oluşturur; yönetici ve zimmete alan kişi için imza alanları eklenir.
    /// </summary>
    public byte[] GeneratePdf(Asset asset, AssetAssignment assignment, string? personName, string? managerName)
    {
        var assetTypeLabel = StatusLabels.AssetTypeLabel(asset.AssetType);
        var assignedAtStr = assignment.AssignedAt.ToString("dd.MM.yyyy");
        var assignedBy = assignment.AssignedByUserName ?? "—";
        var notes = string.IsNullOrWhiteSpace(assignment.Notes) ? "—" : assignment.Notes;
        var zimmetteKisi = string.IsNullOrWhiteSpace(personName) ? "—" : personName;

        using var stream = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .AlignCenter()
                    .Text("ZİMMET BELGESİ")
                    .Bold()
                    .FontSize(18);

                page.Content().Column(column =>
                {
                    column.Spacing(16);

                    column.Item().Text("Donanım bilgileri").Bold().FontSize(12);
                    column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Ad:");
                            r.RelativeItem(2).Text(asset.Name ?? "—");
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Tür:");
                            r.RelativeItem(2).Text(assetTypeLabel);
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Seri no:");
                            r.RelativeItem(2).Text(asset.SerialNumber ?? "—");
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Marka / model:");
                            r.RelativeItem(2).Text(asset.BrandModel ?? "—");
                        });
                    });

                    column.Item().Text("Zimmet bilgileri").Bold().FontSize(12);
                    column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Zimmette (kişi):");
                            r.RelativeItem(2).Text(zimmetteKisi);
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Zimmet tarihi:");
                            r.RelativeItem(2).Text(assignedAtStr);
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Zimmetleyen:");
                            r.RelativeItem(2).Text(assignedBy);
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Not:");
                            r.RelativeItem(2).Text(notes);
                        });
                    });

                    column.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Yönetici").Bold().FontSize(11);
                            col.Item().Text(managerName ?? "—").FontSize(10);
                            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(4).Text("İmza").FontSize(8).Italic();
                        });
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Zimmete alan kişi").Bold().FontSize(11);
                            col.Item().Text(zimmetteKisi).FontSize(10);
                            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(4).Text("İmza").FontSize(8).Italic();
                        });
                    });

                    column.Item().PaddingTop(32).AlignCenter()
                        .Text($"Belge tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(9)
                        .Italic();
                });
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }
}
