using CivicFlow.Application.Abstractions;
using CivicFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Infrastructure;

public class CivicFlowDbContext(DbContextOptions<CivicFlowDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RequestStatus> RequestStatuses => Set<RequestStatus>();
    public DbSet<ServiceRequestType> ServiceRequestTypes => Set<ServiceRequestType>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();
    public DbSet<CaseNote> CaseNotes => Set<CaseNote>();
    public DbSet<RequestDocument> RequestDocuments => Set<RequestDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PolicyArticle> PolicyArticles => Set<PolicyArticle>();
    public DbSet<AssignmentHistory> AssignmentHistories => Set<AssignmentHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("ServiceRequestNumberSeq", "dbo")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.RoleId);
            entity.Property(x => x.RoleId).ValueGeneratedNever();
            entity.Property(x => x.RoleName).HasConversion<int>();
        });

        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.HasKey(x => x.StatusId);
            entity.Property(x => x.StatusId).ValueGeneratedNever();
            entity.Property(x => x.StatusName).HasConversion<int>();
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(x => x.DepartmentId);
            entity.Property(x => x.DepartmentName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.EntraObjectId).HasMaxLength(64);
            entity.HasIndex(x => x.EntraObjectId)
                .IsUnique()
                .HasFilter("[EntraObjectId] IS NOT NULL");
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceRequestType>(entity =>
        {
            entity.HasKey(x => x.ServiceRequestTypeId);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasOne(x => x.Department)
                .WithMany(x => x.RequestTypes)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(x => x.RequestId);
            entity.Property(x => x.RequestNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.RequestNumber).IsUnique();
            entity.HasIndex(x => x.StatusId);
            entity.HasIndex(x => x.AssignedEmployeeId);
            entity.HasIndex(x => x.CitizenId);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.Priority).HasConversion<int>();
            entity.HasOne(x => x.Citizen)
                .WithMany()
                .HasForeignKey(x => x.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedEmployee)
                .WithMany()
                .HasForeignKey(x => x.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequestType)
                .WithMany()
                .HasForeignKey(x => x.RequestTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Status)
                .WithMany()
                .HasForeignKey(x => x.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.SlaDueAt);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.NotificationId);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.LinkPath).HasMaxLength(256);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyArticle>(entity =>
        {
            entity.HasKey(x => x.PolicyArticleId);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Body).IsRequired();
            entity.Property(x => x.Keywords).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<RequestStatusHistory>(entity =>
        {
            entity.HasKey(x => x.HistoryId);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.HasIndex(x => new { x.RequestId, x.ChangedAt });
            entity.HasOne(x => x.Request)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.OldStatus)
                .WithMany()
                .HasForeignKey(x => x.OldStatusId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.NewStatus)
                .WithMany()
                .HasForeignKey(x => x.NewStatusId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ChangedByUser)
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CaseNote>(entity =>
        {
            entity.HasKey(x => x.NoteId);
            entity.Property(x => x.NoteText).IsRequired();
            entity.HasOne(x => x.Request)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequestDocument>(entity =>
        {
            entity.HasKey(x => x.DocumentId);
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.StorageKey).IsUnique();
            entity.HasIndex(x => x.RequestId);
            entity.HasOne(x => x.Request)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssignmentHistory>(entity =>
        {
            entity.HasKey(x => x.AssignmentId);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.HasOne(x => x.Request)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AssignedFromUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedFromUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedToUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedByUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.AuditLogId);
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.HasIndex(x => x.Timestamp);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
