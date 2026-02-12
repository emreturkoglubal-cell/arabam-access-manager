using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AccessManager.Application.Interfaces;

public interface IReviseRequestService
{
    IReadOnlyList<ReviseRequest> GetAll();
    ReviseRequest? GetById(int id);
    Task<ReviseRequest> CreateAsync(string title, string description, int? createdByUserId, string? createdByUserName, List<IFormFile>? images, string webRootPath);
    Task UpdateAsync(int id, string title, string description, List<IFormFile>? newImages, List<int>? imagesToDelete, string webRootPath);
    void UpdateStatus(int id, ReviseRequestStatus status);
    IReadOnlyList<ReviseRequestImage> GetImages(int reviseRequestId);
    IReadOnlyList<ReviseRequestImage> GetImagesByReviseRequestIds(IReadOnlyList<int> reviseRequestIds);
    void DeleteImage(int imageId, string webRootPath);
}
