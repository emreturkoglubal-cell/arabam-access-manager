using AccessManager.Domain.Entities;
using AccessManager.UI.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AccessManager.UI.Services;

/// <summary>
/// Zimmet belgesi PDF: donanım, Zimmet Onay, Zimmet İade (aktifte boş / iade sonrası dolu), imza alanları.
/// </summary>
public class ZimmetPdfService
{
    static ZimmetPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(Asset asset, AssetAssignment assignment, string? personName, string? managerName)
    {
        var isArchive = assignment.ReturnedAt.HasValue;
        var assetTypeLabel = StatusLabels.AssetTypeLabel(asset.AssetType);
        var assignedAtStr = assignment.AssignedAt.ToString("dd.MM.yyyy");
        var assignedBy = assignment.AssignedByUserName ?? "—";
        var notes = string.IsNullOrWhiteSpace(assignment.Notes) ? "—" : assignment.Notes;
        var zimmetteKisi = string.IsNullOrWhiteSpace(personName) ? "—" : personName;
        var returnedAtStr = assignment.ReturnedAt?.ToString("dd.MM.yyyy") ?? "—";
        var returnReceiver = assignment.ReturnedByUserName ?? "—";
        var returnCondition = string.IsNullOrWhiteSpace(assignment.ReturnCondition) ? "—" : assignment.ReturnCondition!;

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
                    .Column(h =>
                    {
                        h.Item().Text("ZİMMET BELGESİ").Bold().FontSize(18);
                        if (isArchive)
                            h.Item().PaddingTop(4).Text("(Arşiv — iade tamamlandı)").FontSize(9).Italic();
                    });

                page.Content().Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Text("Donanım bilgileri").Bold().FontSize(12);
                    column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                    {
                        c.Item().Row(r => { r.RelativeItem().Text("Barkod / ad:"); r.RelativeItem(2).Text(asset.Name ?? "—"); });
                        c.Item().Row(r => { r.RelativeItem().Text("Tür:"); r.RelativeItem(2).Text(assetTypeLabel); });
                        c.Item().Row(r => { r.RelativeItem().Text("Seri no:"); r.RelativeItem(2).Text(asset.SerialNumber ?? "—"); });
                        c.Item().Row(r => { r.RelativeItem().Text("Marka / model:"); r.RelativeItem(2).Text(asset.BrandModel ?? "—"); });
                    });

                    column.Item().Text("Zimmet Onay").Bold().FontSize(12);
                    column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                    {
                        c.Item().Row(r => { r.RelativeItem().Text("Teslim eden (zimmeti yapan):"); r.RelativeItem(2).Text(assignedBy); });
                        c.Item().Row(r => { r.RelativeItem().Text("Teslim alan (personel):"); r.RelativeItem(2).Text(zimmetteKisi); });
                        c.Item().Row(r => { r.RelativeItem().Text("Zimmet tarihi:"); r.RelativeItem(2).Text(assignedAtStr); });
                        c.Item().Row(r => { r.RelativeItem().Text("Tür / marka / seri:"); r.RelativeItem(2).Text($"{assetTypeLabel} · {asset.BrandModel ?? "—"} · {asset.SerialNumber ?? "—"}"); });
                        c.Item().Row(r => { r.RelativeItem().Text("Not:"); r.RelativeItem(2).Text(notes); });
                    });

                    column.Item().PaddingTop(8).Text("Zimmet Onay — İmza").Bold().FontSize(11);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Yönetici").FontSize(9).Bold();
                            col.Item().Text(managerName ?? "—").FontSize(10);
                            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(2).Text("İmza").FontSize(8).Italic();
                        });
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Teslim alan (personel)").FontSize(9).Bold();
                            col.Item().Text(zimmetteKisi).FontSize(10);
                            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(2).Text("İmza").FontSize(8).Italic();
                        });
                    });

                    column.Item().PaddingTop(16).Text("Zimmet İade").Bold().FontSize(12);
                    if (isArchive)
                    {
                        column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Row(r => { r.RelativeItem().Text("İade tarihi:"); r.RelativeItem(2).Text(returnedAtStr); });
                            c.Item().Row(r => { r.RelativeItem().Text("Teslim eden (personel — cihazı iade eden):"); r.RelativeItem(2).Text(zimmetteKisi); });
                            c.Item().Row(r => { r.RelativeItem().Text("Teslim alan (iadeyi alan):"); r.RelativeItem(2).Text(returnReceiver); });
                            c.Item().Row(r => { r.RelativeItem().Text("İade durumu / koşul:"); r.RelativeItem(2).Text(returnCondition); });
                        });
                        column.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Teslim eden (personel)").FontSize(9).Bold();
                                col.Item().Text(zimmetteKisi).FontSize(10);
                                col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(2).Text("İmza").FontSize(8).Italic();
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Teslim alan (iadeyi alan)").FontSize(9).Bold();
                                col.Item().Text(returnReceiver).FontSize(10);
                                col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(2).Text("İmza").FontSize(8).Italic();
                            });
                        });
                    }
                    else
                    {
                        column.Item().Text("İade işlemi sırasında doldurulacaktır.").FontSize(9).Italic();
                        column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Row(r => { r.RelativeItem().Text("İade tarihi:"); r.RelativeItem(2).Text("………………"); });
                            c.Item().Row(r => { r.RelativeItem().Text("Teslim eden (personel):"); r.RelativeItem(2).Text("………………"); });
                            c.Item().Row(r => { r.RelativeItem().Text("Teslim alan (iadeyi alan):"); r.RelativeItem(2).Text("………………"); });
                        });
                        column.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Teslim eden (personel)").FontSize(9).Bold();
                                col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(2).Text("İmza").FontSize(8).Italic();
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Teslim alan (iadeyi alan)").FontSize(9).Bold();
                                col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(2).Text("İmza").FontSize(8).Italic();
                            });
                        });
                    }

                    column.Item().PaddingTop(24).AlignCenter()
                        .Text($"Belge oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(9)
                        .Italic();
                });
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }

    /// <summary>İşten çıkış özet belgesi: personel, açık yetki uyarısı, zimmet özeti.</summary>
    public byte[] GenerateOffboardingExitSummaryPdf(Personnel personnel, bool hasOpenAccess, IReadOnlyList<(Asset Asset, AssetAssignment Assignment)> zimmetRows)
    {
        var fullName = $"{personnel.FirstName} {personnel.LastName}".Trim();
        using var stream = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                page.Header().AlignCenter().Text("İŞTEN ÇIKIŞ — ÖZET").Bold().FontSize(16);
                page.Content().Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Text("Personel").Bold().FontSize(12);
                    column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                    {
                        c.Item().Text($"Ad Soyad: {fullName}");
                        c.Item().Text($"E-posta: {personnel.Email}");
                        c.Item().Text($"İşe giriş: {personnel.StartDate:dd.MM.yyyy}");
                        c.Item().Text($"İşten çıkış: {(personnel.EndDate?.ToString("dd.MM.yyyy") ?? "—")}");
                    });
                    column.Item().Text("Uygulama yetkileri").Bold().FontSize(12);
                    if (hasOpenAccess)
                        column.Item().Background("#fef2f2").Padding(8).Text("UYARI: Bu personelde hâlâ aktif uygulama yetkisi kayıtları bulunmaktadır. İnceleyin.").FontColor("#b91c1c");
                    else
                        column.Item().Background(Colors.Grey.Lighten3).Padding(8).Text("Aktif uygulama yetkisi kaydı yok (özet anına göre).");

                    column.Item().Text("Zimmet özeti").Bold().FontSize(12);
                    if (zimmetRows.Count == 0)
                    {
                        column.Item().Text("Zimmet kaydı yok.").Italic();
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("Donanım").Bold();
                                h.Cell().Element(CellStyle).Text("Durum").Bold();
                                h.Cell().Element(CellStyle).Text("Tarihler").Bold();
                            });
                            foreach (var row in zimmetRows.OrderByDescending(x => x.Assignment.AssignedAt))
                            {
                                var a = row.Asset;
                                var z = row.Assignment;
                                var active = !z.ReturnedAt.HasValue;
                                var statusText = active ? "Aktif zimmet" : "İade edildi";
                                var dates = $"{z.AssignedAt:dd.MM.yyyy}" + (z.ReturnedAt.HasValue ? $" → {z.ReturnedAt:dd.MM.yyyy}" : "");
                                table.Cell().Element(CellStyle).Text(a.Name ?? "—");
                                table.Cell().Element(CellStyle).Text(statusText);
                                table.Cell().Element(CellStyle).Text(dates);
                            }
                        });
                    }
                    column.Item().PaddingTop(16).AlignCenter().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9).Italic();
                });
            });
        }).GeneratePdf(stream);
        return stream.ToArray();

        static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4);
    }
}
