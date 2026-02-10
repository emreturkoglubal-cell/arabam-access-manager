using AccessManager.Domain.Constants;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Data;

/// <summary>
/// Mock veri deposu. DI ile Singleton olarak kaydedilir; DB bağlandığında gerçek repository'ler kullanılacak.
/// </summary>
public class MockDataStore
{
    public List<Department> Departments { get; } = new();
    public List<Personnel> Personnel { get; } = new();
    public List<ResourceSystem> ResourceSystems { get; } = new();
    public List<Role> Roles { get; } = new();
    public List<RolePermission> RolePermissions { get; } = new();
    public List<PersonnelAccess> PersonnelAccesses { get; } = new();
    public List<AccessRequest> AccessRequests { get; } = new();
    public List<ApprovalStep> ApprovalSteps { get; } = new();
    public List<AuditLog> AuditLogs { get; } = new();
    public List<AppUser> AppUsers { get; } = new();
    public List<Asset> Assets { get; } = new();
    public List<AssetAssignment> AssetAssignments { get; } = new();
    public List<AssetAssignmentNote> AssetAssignmentNotes { get; } = new();
    public List<PersonnelNote> PersonnelNotes { get; } = new();

    public MockDataStore()
    {
        Seed();
    }

    /// <summary>Mock ortamda tüm kullanıcılar için geçerli parola.</summary>
    public const string MockPassword = "Password1";

    private void Seed()
    {
        SeedAppUsers();
        var depBilgiIslem = new Department { Id = Guid.NewGuid(), Name = "Bilgi İşlem", Code = "IT" };
        var depInsanKaynaklari = new Department { Id = Guid.NewGuid(), Name = "İnsan Kaynakları", Code = "HR" };
        var depYazilim = new Department { Id = Guid.NewGuid(), Name = "Yazılım Geliştirme", Code = "DEV" };
        var depFinans = new Department { Id = Guid.NewGuid(), Name = "Finans", Code = "FIN" };
        Departments.AddRange(new[] { depBilgiIslem, depInsanKaynaklari, depYazilim, depFinans });

        var rolBackend = new Role { Id = Guid.NewGuid(), Name = "Backend Developer", Code = "BE" };
        var rolFrontend = new Role { Id = Guid.NewGuid(), Name = "Frontend Developer", Code = "FE" };
        var rolHR = new Role { Id = Guid.NewGuid(), Name = "HR Specialist", Code = "HR" };
        var rolIT = new Role { Id = Guid.NewGuid(), Name = "IT Admin", Code = "IT" };
        var rolFinance = new Role { Id = Guid.NewGuid(), Name = "Finance User", Code = "FIN" };
        Roles.AddRange(new[] { rolBackend, rolFrontend, rolHR, rolIT, rolFinance });

        var sysBitbucket = new ResourceSystem { Id = Guid.NewGuid(), Name = "Bitbucket", Code = "BIT", SystemType = SystemType.Application, CriticalLevel = CriticalLevel.Medium };
        var sysMail = new ResourceSystem { Id = Guid.NewGuid(), Name = "Kurumsal Mail", Code = "MAIL", SystemType = SystemType.Application, CriticalLevel = CriticalLevel.High };
        var sysVpn = new ResourceSystem { Id = Guid.NewGuid(), Name = "VPN", Code = "VPN", SystemType = SystemType.Infrastructure, CriticalLevel = CriticalLevel.High };
        var sysOffice = new ResourceSystem { Id = Guid.NewGuid(), Name = "Office 365", Code = "O365", SystemType = SystemType.License, CriticalLevel = CriticalLevel.Low };
        var sysErp = new ResourceSystem { Id = Guid.NewGuid(), Name = "ERP", Code = "ERP", SystemType = SystemType.Application, CriticalLevel = CriticalLevel.High };
        var sysTestEnv = new ResourceSystem { Id = Guid.NewGuid(), Name = "Test Ortamları", Code = "TEST", SystemType = SystemType.Infrastructure, CriticalLevel = CriticalLevel.Medium };
        var sysEskiProje = new ResourceSystem { Id = Guid.NewGuid(), Name = "Eski Proje (Kapatıldı)", Code = "LEGACY", SystemType = SystemType.Application, CriticalLevel = CriticalLevel.Low };
        ResourceSystems.AddRange(new[] { sysBitbucket, sysMail, sysVpn, sysOffice, sysErp, sysTestEnv, sysEskiProje });

        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolBackend.Id, ResourceSystemId = sysBitbucket.Id, PermissionType = PermissionType.Read, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolBackend.Id, ResourceSystemId = sysBitbucket.Id, PermissionType = PermissionType.Write, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolBackend.Id, ResourceSystemId = sysVpn.Id, PermissionType = PermissionType.Read, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolBackend.Id, ResourceSystemId = sysTestEnv.Id, PermissionType = PermissionType.Read, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolBackend.Id, ResourceSystemId = sysMail.Id, PermissionType = PermissionType.Read, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolBackend.Id, ResourceSystemId = sysOffice.Id, PermissionType = PermissionType.Read, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolIT.Id, ResourceSystemId = sysBitbucket.Id, PermissionType = PermissionType.Admin, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolIT.Id, ResourceSystemId = sysMail.Id, PermissionType = PermissionType.Admin, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolIT.Id, ResourceSystemId = sysVpn.Id, PermissionType = PermissionType.Admin, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolHR.Id, ResourceSystemId = sysMail.Id, PermissionType = PermissionType.Read, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolHR.Id, ResourceSystemId = sysOffice.Id, PermissionType = PermissionType.Read, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolFinance.Id, ResourceSystemId = sysErp.Id, PermissionType = PermissionType.Read, IsDefault = true });
        RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = rolFinance.Id, ResourceSystemId = sysOffice.Id, PermissionType = PermissionType.Read, IsDefault = true });

        var manager = new Personnel
        {
            Id = Guid.NewGuid(),
            SicilNo = "1001",
            FirstName = "Enes Emre",
            LastName = "Arıkan",
            Email = "ahmet.yilmaz@arabam.com",
            DepartmentId = depBilgiIslem.Id,
            Position = "Bilgi İşlem Müdürü",
            ManagerId = null,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = null,
            Status = PersonnelStatus.Active,
            RoleId = rolIT.Id,
            // Demo: yüz doğrulama için referans foto (CC0 / public domain örnek)
            ImageUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400"
        };
        Personnel.Add(manager);
        var appUserManager = AppUsers.Find(u => string.Equals(u.UserName, "manager", StringComparison.OrdinalIgnoreCase));
        if (appUserManager != null) appUserManager.PersonnelId = manager.Id;

        var dev1 = new Personnel
        {
            Id = Guid.NewGuid(),
            SicilNo = "1002",
            FirstName = "Mehmet",
            LastName = "Kaya",
            Email = "mehmet.kaya@arabam.com",
            DepartmentId = depYazilim.Id,
            Position = "Backend Geliştirici",
            ManagerId = manager.Id,
            StartDate = new DateTime(2022, 3, 15),
            EndDate = null,
            Status = PersonnelStatus.Active,
            RoleId = rolBackend.Id,
            Rating = 8.0m,
            ManagerComment = "Teknik bilgisi güçlü, projelere zamanında teslim. Takım içi iletişimi iyi."
        };
        Personnel.Add(dev1);

        var dev2 = new Personnel
        {
            Id = Guid.NewGuid(),
            SicilNo = "1003",
            FirstName = "Ayşe",
            LastName = "Demir",
            Email = "ayse.demir@arabam.com",
            DepartmentId = depYazilim.Id,
            Position = "Frontend Geliştirici",
            ManagerId = manager.Id,
            StartDate = new DateTime(2023, 6, 1),
            EndDate = null,
            Status = PersonnelStatus.Active,
            RoleId = rolFrontend.Id,
            Rating = 7.5m,
            ManagerComment = "Takım çalışması çok iyi. UI/UX konusunda gelişmeye açık."
        };
        Personnel.Add(dev2);

        var hr1 = new Personnel
        {
            Id = Guid.NewGuid(),
            SicilNo = "1004",
            FirstName = "Fatma",
            LastName = "Şahin",
            Email = "fatma.sahin@arabam.com",
            DepartmentId = depInsanKaynaklari.Id,
            Position = "İK Uzmanı",
            ManagerId = manager.Id,
            StartDate = new DateTime(2021, 9, 1),
            EndDate = null,
            Status = PersonnelStatus.Active,
            RoleId = rolHR.Id,
            Rating = 9.0m,
            ManagerComment = "Düzenli ve sorumlu. İşe alım süreçlerinde başarılı."
        };
        Personnel.Add(hr1);

        var exEmployee = new Personnel
        {
            Id = Guid.NewGuid(),
            SicilNo = "1000",
            FirstName = "Eski",
            LastName = "Personel",
            Email = "eski@arabam.com",
            DepartmentId = depYazilim.Id,
            Position = "Eski Geliştirici",
            ManagerId = manager.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2024, 11, 30),
            Status = PersonnelStatus.Offboarded,
            RoleId = rolBackend.Id
        };
        Personnel.Add(exEmployee);

        // Mock: İşten çıkmış ama açık yetkisi olan – Eski Personel (1000) işten ayrıldı ama yetkileri kapatılmamış (test için).
        PersonnelAccesses.Add(new PersonnelAccess
        {
            Id = Guid.NewGuid(),
            PersonnelId = exEmployee.Id,
            ResourceSystemId = sysBitbucket.Id,
            PermissionType = PermissionType.Open,
            IsException = false,
            GrantedAt = exEmployee.StartDate,
            ExpiresAt = null,
            IsActive = true
        });
        PersonnelAccesses.Add(new PersonnelAccess
        {
            Id = Guid.NewGuid(),
            PersonnelId = exEmployee.Id,
            ResourceSystemId = sysMail.Id,
            PermissionType = PermissionType.Open,
            IsException = false,
            GrantedAt = exEmployee.StartDate,
            ExpiresAt = null,
            IsActive = true
        });
        PersonnelAccesses.Add(new PersonnelAccess
        {
            Id = Guid.NewGuid(),
            PersonnelId = exEmployee.Id,
            ResourceSystemId = sysVpn.Id,
            PermissionType = PermissionType.Open,
            IsException = false,
            GrantedAt = exEmployee.StartDate,
            ExpiresAt = null,
            IsActive = true
        });

        sysBitbucket.OwnerId = manager.Id;
        sysMail.OwnerId = manager.Id;
        sysVpn.OwnerId = manager.Id;
        sysOffice.OwnerId = manager.Id;
        sysErp.OwnerId = manager.Id;
        sysTestEnv.OwnerId = manager.Id;
        sysEskiProje.OwnerId = manager.Id;

        var now = DateTime.UtcNow;
        foreach (var rp in RolePermissions.Where(r => r.RoleId == rolBackend.Id))
        {
            PersonnelAccesses.Add(new PersonnelAccess
            {
                Id = Guid.NewGuid(),
                PersonnelId = dev1.Id,
                ResourceSystemId = rp.ResourceSystemId,
                PermissionType = rp.PermissionType,
                IsException = false,
                GrantedAt = dev1.StartDate,
                ExpiresAt = null,
                IsActive = true
            });
        }
        PersonnelAccesses.Add(new PersonnelAccess
        {
            Id = Guid.NewGuid(),
            PersonnelId = dev1.Id,
            ResourceSystemId = sysErp.Id,
            PermissionType = PermissionType.Read,
            IsException = true,
            GrantedAt = now.AddDays(-30),
            ExpiresAt = now.AddDays(30),
            IsActive = true
        });

        // Mock: Daha önce açılıp kapatılan yetkiler – test için.
        // Mehmet Kaya (dev1): "Eski Proje (Kapatıldı)" sistemi için sadece kapalı kayıt (eskiden vardı, alındı).
        // Mehmet Kaya (dev1): Office 365 için ek kapalı kayıt (liste ilk bulduğu = rol ile açık olanı gösterir).
        PersonnelAccesses.Add(new PersonnelAccess
        {
            Id = Guid.NewGuid(),
            PersonnelId = dev1.Id,
            ResourceSystemId = sysEskiProje.Id,
            PermissionType = PermissionType.Open,
            IsException = false,
            GrantedAt = now.AddMonths(-12),
            ExpiresAt = null,
            IsActive = false
        });
        PersonnelAccesses.Add(new PersonnelAccess
        {
            Id = Guid.NewGuid(),
            PersonnelId = dev1.Id,
            ResourceSystemId = sysOffice.Id,
            PermissionType = PermissionType.Open,
            IsException = false,
            GrantedAt = dev1.StartDate,
            ExpiresAt = null,
            IsActive = false
        });
        // Mock: Ayşe Demir (dev2) – eskiden VPN yetkisi vardı, kapatıldı.
        PersonnelAccesses.Add(new PersonnelAccess
        {
            Id = Guid.NewGuid(),
            PersonnelId = dev2.Id,
            ResourceSystemId = sysVpn.Id,
            PermissionType = PermissionType.Open,
            IsException = false,
            GrantedAt = dev2.StartDate,
            ExpiresAt = null,
            IsActive = false
        });

        var req1 = new AccessRequest
        {
            Id = Guid.NewGuid(),
            PersonnelId = dev2.Id,
            ResourceSystemId = sysBitbucket.Id,
            RequestedPermission = PermissionType.Write,
            Reason = "Proje geliştirmesi",
            Status = AccessRequestStatus.Applied,
            CreatedAt = now.AddDays(-5),
            CreatedBy = dev2.Id
        };
        AccessRequests.Add(req1);
        ApprovalSteps.Add(new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = req1.Id, StepName = ApprovalStepNames.Manager, ApprovedBy = manager.Id, ApprovedAt = now.AddDays(-4), Approved = true, Order = 1 });
        ApprovalSteps.Add(new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = req1.Id, StepName = ApprovalStepNames.IT, ApprovedBy = manager.Id, ApprovedAt = now.AddDays(-3), Approved = true, Order = 2 });

        var req2 = new AccessRequest
        {
            Id = Guid.NewGuid(),
            PersonnelId = dev1.Id,
            ResourceSystemId = sysErp.Id,
            RequestedPermission = PermissionType.Read,
            Reason = "Raporlama",
            StartDate = now.AddDays(-30),
            EndDate = now.AddDays(30),
            Status = AccessRequestStatus.Applied,
            CreatedAt = now.AddDays(-35),
            CreatedBy = dev1.Id
        };
        AccessRequests.Add(req2);

        var reqPending = new AccessRequest
        {
            Id = Guid.NewGuid(),
            PersonnelId = dev2.Id,
            ResourceSystemId = sysTestEnv.Id,
            RequestedPermission = PermissionType.Write,
            Reason = "Test deployment",
            Status = AccessRequestStatus.PendingManager,
            CreatedAt = now,
            CreatedBy = dev2.Id
        };
        AccessRequests.Add(reqPending);
        ApprovalSteps.Add(new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = reqPending.Id, StepName = ApprovalStepNames.Manager, ApprovedBy = null, ApprovedAt = null, Approved = null, Order = 1 });

        AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorId = manager.Id, ActorName = "Enes Emre Arıkan", Action = AuditAction.PersonnelCreated, TargetType = "Personnel", TargetId = dev2.Id.ToString(), Details = "İşe giriş", Timestamp = dev2.StartDate });
        AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorId = manager.Id, ActorName = "Enes Emre Arıkan", Action = AuditAction.AccessGranted, TargetType = "PersonnelAccess", TargetId = req1.Id.ToString(), Details = "Bitbucket Write", Timestamp = now.AddDays(-3) });
        AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorId = manager.Id, ActorName = "Enes Emre Arıkan", Action = AuditAction.PersonnelOffboarded, TargetType = "Personnel", TargetId = exEmployee.Id.ToString(), Details = "İşten çıkış", Timestamp = exEmployee.EndDate!.Value });
        AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorId = dev2.Id, ActorName = "Ayşe Demir", Action = AuditAction.RequestCreated, TargetType = "AccessRequest", TargetId = reqPending.Id.ToString(), Details = "Test Ortamları Write talebi", Timestamp = now });
        for (int i = 0; i < 15; i++)
        {
            AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorId = manager.Id,
                ActorName = "Enes Emre Arıkan",
                Action = (AuditAction)(i % 5),
                TargetType = i % 2 == 0 ? "Personnel" : "Access",
                TargetId = Guid.NewGuid().ToString(),
                Details = $"Örnek işlem {i}",
                Timestamp = now.AddDays(-i)
            });
        }

        SeedAssets(manager, dev1, dev2, hr1);
    }

    private void SeedAssets(Personnel manager, Personnel dev1, Personnel dev2, Personnel hr1)
    {
        var now = DateTime.UtcNow;
        var lap1 = new Asset { Id = Guid.NewGuid(), AssetType = AssetType.Laptop, Name = "Dizüstü-001", SerialNumber = "SN-LAP-001", BrandModel = "Dell Latitude 5520", Status = AssetStatus.Assigned, PurchaseDate = new DateTime(2022, 1, 15), CreatedAt = now.AddMonths(-24) };
        var lap2 = new Asset { Id = Guid.NewGuid(), AssetType = AssetType.Laptop, Name = "Dizüstü-002", SerialNumber = "SN-LAP-002", BrandModel = "HP EliteBook 840", Status = AssetStatus.Assigned, PurchaseDate = new DateTime(2023, 3, 1), CreatedAt = now.AddMonths(-18) };
        var lap3 = new Asset { Id = Guid.NewGuid(), AssetType = AssetType.Laptop, Name = "Dizüstü-003", SerialNumber = "SN-LAP-003", BrandModel = "Lenovo ThinkPad X1", Status = AssetStatus.Available, PurchaseDate = new DateTime(2024, 1, 10), CreatedAt = now.AddMonths(-6) };
        var phone1 = new Asset { Id = Guid.NewGuid(), AssetType = AssetType.Phone, Name = "Telefon-001", SerialNumber = "SN-PH-001", BrandModel = "iPhone 14", Status = AssetStatus.Assigned, PurchaseDate = new DateTime(2023, 6, 1), CreatedAt = now.AddMonths(-12) };
        var phone2 = new Asset { Id = Guid.NewGuid(), AssetType = AssetType.Phone, Name = "Telefon-002", SerialNumber = "SN-PH-002", BrandModel = "Samsung Galaxy S23", Status = AssetStatus.Available, PurchaseDate = new DateTime(2024, 2, 1), CreatedAt = now.AddMonths(-4) };
        var mon1 = new Asset { Id = Guid.NewGuid(), AssetType = AssetType.Monitor, Name = "Monitör-001", SerialNumber = "SN-MON-001", BrandModel = "Dell P2422H", Status = AssetStatus.Assigned, PurchaseDate = new DateTime(2021, 8, 1), CreatedAt = now.AddMonths(-36) };
        Assets.AddRange(new[] { lap1, lap2, lap3, phone1, phone2, mon1 });

        var assignLap1 = new AssetAssignment { Id = Guid.NewGuid(), AssetId = lap1.Id, PersonnelId = dev1.Id, AssignedAt = dev1.StartDate.AddDays(1), AssignedByUserId = manager.Id, AssignedByUserName = "Enes Emre Arıkan", Notes = "İşe giriş donanımı" };
        var assignLap2 = new AssetAssignment { Id = Guid.NewGuid(), AssetId = lap2.Id, PersonnelId = dev2.Id, AssignedAt = dev2.StartDate.AddDays(1), AssignedByUserId = manager.Id, AssignedByUserName = "Enes Emre Arıkan", Notes = "İşe giriş donanımı" };
        var assignPhone1 = new AssetAssignment { Id = Guid.NewGuid(), AssetId = phone1.Id, PersonnelId = dev1.Id, AssignedAt = now.AddMonths(-6), AssignedByUserId = manager.Id, AssignedByUserName = "Enes Emre Arıkan", Notes = "Kurumsal telefon" };
        var assignMon1 = new AssetAssignment { Id = Guid.NewGuid(), AssetId = mon1.Id, PersonnelId = hr1.Id, AssignedAt = hr1.StartDate.AddDays(2), AssignedByUserId = manager.Id, AssignedByUserName = "Enes Emre Arıkan" };
        AssetAssignments.AddRange(new[] { assignLap1, assignLap2, assignPhone1, assignMon1 });
    }

    private void SeedAppUsers()
    {
        AppUsers.Add(new AppUser { Id = Guid.NewGuid(), UserName = "admin", DisplayName = "Sistem Yöneticisi", PasswordHash = MockPassword, Role = AppRole.Admin });
        AppUsers.Add(new AppUser { Id = Guid.NewGuid(), UserName = "manager", DisplayName = "Yetki Yöneticisi", PasswordHash = MockPassword, Role = AppRole.Manager });
        AppUsers.Add(new AppUser { Id = Guid.NewGuid(), UserName = "user", DisplayName = "Standart Kullanıcı", PasswordHash = MockPassword, Role = AppRole.User });
        AppUsers.Add(new AppUser { Id = Guid.NewGuid(), UserName = "auditor", DisplayName = "Denetçi", PasswordHash = MockPassword, Role = AppRole.Auditor });
        AppUsers.Add(new AppUser { Id = Guid.NewGuid(), UserName = "viewer", DisplayName = "İzleyici", PasswordHash = MockPassword, Role = AppRole.Viewer });
    }
}
