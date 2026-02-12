using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Repositories;

public interface IReviseRequestRepository
{
    IReadOnlyList<ReviseRequest> GetAll();
    ReviseRequest? GetById(int id);
    ReviseRequest? GetByIdWithImages(int id);
    int Insert(ReviseRequest request);
    void Update(ReviseRequest request);
    void UpdateStatus(int id, ReviseRequestStatus status);
    IReadOnlyList<ReviseRequestImage> GetImages(int reviseRequestId);
    ReviseRequestImage? GetImageById(int imageId);
    void InsertImage(ReviseRequestImage image);
    void DeleteImage(int imageId);
}
