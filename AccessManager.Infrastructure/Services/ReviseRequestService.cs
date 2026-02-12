using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;

namespace AccessManager.Infrastructure.Services;

public class ReviseRequestService : IReviseRequestService
{
    private readonly IReviseRequestRepository _repo;

    public ReviseRequestService(IReviseRequestRepository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<ReviseRequest> GetAll() => _repo.GetAll();

    public ReviseRequest? GetById(int id) => _repo.GetByIdWithImages(id);

    public async Task<ReviseRequest> CreateAsync(string title, string description, int? createdByUserId, string? createdByUserName, List<IFormFile>? images, string webRootPath)
    {
        var request = new ReviseRequest
        {
            Title = title.Trim(),
            Description = description.Trim(),
            Status = ReviseRequestStatus.Pending,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = createdByUserName ?? "Sistem"
        };

        request.Id = _repo.Insert(request);

        if (images != null && images.Any())
        {
            await SaveImagesAsync(request.Id, images, webRootPath);
        }

        return request;
    }

    public async Task UpdateAsync(int id, string title, string description, List<IFormFile>? newImages, List<int>? imagesToDelete, string webRootPath)
    {
        var request = _repo.GetById(id);
        if (request == null)
        {
            throw new InvalidOperationException($"ReviseRequest with id {id} not found.");
        }

        request.Title = title.Trim();
        request.Description = description.Trim();
        _repo.Update(request);

        // Silinecek resimleri kaldır
        if (imagesToDelete != null && imagesToDelete.Any())
        {
            foreach (var imageId in imagesToDelete)
            {
                DeleteImage(imageId, webRootPath);
            }
        }

        // Yeni resimleri ekle
        if (newImages != null && newImages.Any())
        {
            await SaveImagesAsync(id, newImages, webRootPath);
        }
    }

    public void UpdateStatus(int id, ReviseRequestStatus status) => _repo.UpdateStatus(id, status);

    public IReadOnlyList<ReviseRequestImage> GetImages(int reviseRequestId) => _repo.GetImages(reviseRequestId);

    public void DeleteImage(int imageId, string webRootPath)
    {
        var image = _repo.GetImageById(imageId);
        if (image != null)
        {
            var filePath = Path.Combine(webRootPath, image.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            _repo.DeleteImage(imageId);
        }
    }

    private async Task SaveImagesAsync(int reviseRequestId, List<IFormFile> imageFiles, string webRootPath)
    {
        var imageFolder = Path.Combine(webRootPath, "ReviseRequestImage");
        if (!Directory.Exists(imageFolder))
        {
            Directory.CreateDirectory(imageFolder);
        }

        var existingImages = _repo.GetImages(reviseRequestId);
        var maxDisplayOrder = existingImages.Any() ? existingImages.Max(i => i.DisplayOrder) : -1;
        var displayOrder = maxDisplayOrder + 1;

        foreach (var imageFile in imageFiles)
        {
            if (imageFile.Length > 0)
            {
                var fileName = $"{reviseRequestId}_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(imageFolder, fileName);
                var relativePath = $"/ReviseRequestImage/{fileName}";

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                var image = new ReviseRequestImage
                {
                    ReviseRequestId = reviseRequestId,
                    FileName = imageFile.FileName,
                    FilePath = relativePath,
                    FileSize = imageFile.Length,
                    MimeType = imageFile.ContentType,
                    DisplayOrder = displayOrder++
                };

                _repo.InsertImage(image);
            }
        }
    }
}
