using AccessManager.Application.Dtos;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Donanım (Asset) ve zimmet (AssetAssignment) yönetimi: CRUD, durum/tür ile listeleme ve sayfalama, zimmet atama/iade ve atama notları.
/// </summary>
public interface IAssetService
{
    /// <summary>Tüm donanım kayıtlarını döner.</summary>
    IReadOnlyList<Asset> GetAll();
    /// <summary>Durum ve tür filtresi ile sayfalı donanım listesi.</summary>
    PagedResult<Asset> GetPaged(AssetStatus? status, AssetType? type, int page, int pageSize);
    /// <summary>Belirtilen duruma (Available, Assigned, InRepair, Retired) göre donanımlar.</summary>
    IReadOnlyList<Asset> GetByStatus(AssetStatus status);
    /// <summary>Belirtilen türe (Laptop, Desktop, vb.) göre donanımlar.</summary>
    IReadOnlyList<Asset> GetByType(AssetType type);
    /// <summary>ID ile tek donanım; yoksa null.</summary>
    Asset? GetById(int id);
    /// <summary>Personelin zimmetinde olan (iade edilmemiş) atamalar.</summary>
    IReadOnlyList<AssetAssignment> GetActiveAssignmentsByPersonnel(int personnelId);
    /// <summary>Donanımın geçmiş zimmet kayıtları (iade edilenler dahil).</summary>
    IReadOnlyList<AssetAssignment> GetAssignmentHistoryByAsset(int assetId);
    /// <summary>Donanımın şu anki aktif zimmeti; yoksa null.</summary>
    AssetAssignment? GetActiveAssignmentForAsset(int assetId);
    /// <summary>Birden fazla donanım için aktif zimmetleri tek sorguda döner (liste sayfaları için).</summary>
    IReadOnlyList<AssetAssignment> GetActiveAssignmentsForAssets(IReadOnlyList<int> assetIds);
    /// <summary>Zimmet kaydı ID ile atama; yoksa null.</summary>
    AssetAssignment? GetAssignmentById(int assignmentId);
    /// <summary>Zimmet kaydına ait notları (AssetAssignmentNote) döner.</summary>
    IReadOnlyList<AssetAssignmentNote> GetNotesForAssignment(int assignmentId);
    /// <summary>Zimmete yeni not ekler (kim eklediği kaydedilir).</summary>
    void AddNoteToAssignment(int assignmentId, string content, int? createdByUserId, string? createdByUserName);

    /// <summary>Yeni donanım kaydı oluşturur.</summary>
    Asset Create(Asset asset);
    /// <summary>Donanım bilgilerini günceller.</summary>
    void Update(Asset asset);
    /// <summary>Donanımı siler; zimmette veya geçmiş atamada kullanılıyorsa InvalidOperationException atar.</summary>
    void Delete(int assetId);

    /// <summary>Donanımı personele zimmetler; Asset durumu Assigned olur, AssetAssignment oluşturulur.</summary>
    AssetAssignment Assign(int assetId, int personnelId, string? notes, int? assignedByUserId, string? assignedByUserName);
    /// <summary>Zimmet iadesini kaydeder; ReturnedAt ve isteğe bağlı iade koşulu/not.</summary>
    void Return(int assignmentId, string? returnCondition, string? notes);
}
