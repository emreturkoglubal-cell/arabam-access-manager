using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Revizyon talepleri (ReviseRequest): liste, oluşturma (görsel yükleme), güncelleme, durum (Pending/Resolved) ve görsel (ReviseRequestImage) yönetimi.
/// </summary>
public interface IReviseRequestService
{
    /// <summary>Tüm revizyon taleplerini döner.</summary>
    IReadOnlyList<ReviseRequest> GetAll();
    /// <summary>ID ile tek talep; yoksa null.</summary>
    ReviseRequest? GetById(int id);
    /// <summary>Yeni revizyon talebi oluşturur; isteğe bağlı görseller webRootPath altına kaydedilir.</summary>
    Task<ReviseRequest> CreateAsync(string title, string description, int? createdByUserId, string? createdByUserName, List<IFormFile>? images, string webRootPath);
    /// <summary>Talep başlık/açıklama ve görselleri günceller; silinecek görsel ID'leri imagesToDelete ile verilir.</summary>
    Task UpdateAsync(int id, string title, string description, List<IFormFile>? newImages, List<int>? imagesToDelete, string webRootPath);
    /// <summary>Talep durumunu Pending veya Resolved yapar.</summary>
    void UpdateStatus(int id, ReviseRequestStatus status);
    /// <summary>Tek talebin ekran görüntülerini (DisplayOrder'a göre) döner.</summary>
    IReadOnlyList<ReviseRequestImage> GetImages(int reviseRequestId);
    /// <summary>Birden fazla talep ID'si için tüm görselleri tek seferde döner (liste sayfaları için).</summary>
    IReadOnlyList<ReviseRequestImage> GetImagesByReviseRequestIds(IReadOnlyList<int> reviseRequestIds);
    /// <summary>Görsel dosyayı siler ve veritabanı kaydını kaldırır.</summary>
    void DeleteImage(int imageId, string webRootPath);
}
