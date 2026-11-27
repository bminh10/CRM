using System;
using System.Collections.Generic;
using CRM.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Data;

public partial class CrmDbContext : DbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CalendarEvent> CalendarEvents { get; set; }

    public virtual DbSet<CalendarEventAttendee> CalendarEventAttendees { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<Opportunity> Opportunities { get; set; }

    public virtual DbSet<OpportunityMember> OpportunityMembers { get; set; }

    public virtual DbSet<Pipeline> Pipelines { get; set; }

    public virtual DbSet<PipelineStage> PipelineStages { get; set; }

    public virtual DbSet<CRM.Api.Models.Task> Tasks { get; set; }

    public virtual DbSet<TaskAssignee> TaskAssignees { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Workspace> Workspaces { get; set; }

    public virtual DbSet<WorkspaceInvitation> WorkspaceInvitations { get; set; }

    public virtual DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => new { e.WorkspaceId, e.CreatedAt }, "idx_audit_logs_created_at").IsDescending(false, true);

            entity.HasIndex(e => e.UserId, "idx_audit_logs_user_id");

            entity.HasIndex(e => e.WorkspaceId, "idx_audit_logs_workspace_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .HasColumnName("entity_type");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.NewData)
                .HasColumnType("jsonb")
                .HasColumnName("new_data");
            entity.Property(e => e.OldData)
                .HasColumnType("jsonb")
                .HasColumnName("old_data");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_audit_logs_user");

            entity.HasOne(d => d.Workspace).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_audit_logs_workspace");
        });

        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("calendar_events_pkey");

            entity.ToTable("calendar_events");

            entity.HasIndex(e => e.CompanyId, "idx_calendar_events_company_id");

            entity.HasIndex(e => new { e.WorkspaceId, e.StartTime }, "idx_calendar_events_start_time");

            entity.HasIndex(e => e.WorkspaceId, "idx_calendar_events_workspace_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.EventType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'offline'::character varying")
                .HasColumnName("event_type");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.MeetingUrl).HasColumnName("meeting_url");
            entity.Property(e => e.OpportunityId).HasColumnName("opportunity_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Company).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_calendar_events_company");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_calendar_events_created_by");

            entity.HasOne(d => d.Opportunity).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.OpportunityId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_calendar_events_opportunity");

            entity.HasOne(d => d.Workspace).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_calendar_events_workspace");
        });

        modelBuilder.Entity<CalendarEventAttendee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("calendar_event_attendees_pkey");

            entity.ToTable("calendar_event_attendees");

            entity.HasIndex(e => e.UserId, "idx_event_attendees_user_id");

            entity.HasIndex(e => new { e.EventId, e.UserId }, "uq_event_attendees").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AttendanceStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("attendance_status");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.InvitedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("invited_at");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Event).WithMany(p => p.CalendarEventAttendees)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("fk_event_attendees_event");

            entity.HasOne(d => d.User).WithMany(p => p.CalendarEventAttendees)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_event_attendees_user");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("companies_pkey");

            entity.ToTable("companies");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CompanyType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'customer'::character varying")
                .HasColumnName("company_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasColumnName("tax_code");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.Website).HasColumnName("website");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Companies)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_companies_workspace");
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("contacts_pkey");

            entity.ToTable("contacts");

            entity.HasIndex(e => e.CompanyId, "idx_contacts_company_id");

            entity.HasIndex(e => e.WorkspaceId, "idx_contacts_workspace_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.JobTitle)
                .HasMaxLength(100)
                .HasColumnName("job_title");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Company).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("fk_contacts_company");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contacts_workspace");
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("opportunities_pkey");

            entity.ToTable("opportunities");

            entity.HasIndex(e => new { e.WorkspaceId, e.ExpectedCloseDate }, "idx_opportunities_close_date");

            entity.HasIndex(e => e.CompanyId, "idx_opportunities_company_id");

            entity.HasIndex(e => e.PipelineId, "idx_opportunities_pipeline_id");

            entity.HasIndex(e => e.StageId, "idx_opportunities_stage_id");

            entity.HasIndex(e => e.WorkspaceId, "idx_opportunities_workspace_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ExpectedCloseDate).HasColumnName("expected_close_date");
            entity.Property(e => e.ExpectedValue)
                .HasPrecision(18, 2)
                .HasColumnName("expected_value");
            entity.Property(e => e.PipelineId).HasColumnName("pipeline_id");
            entity.Property(e => e.Probability).HasColumnName("probability");
            entity.Property(e => e.StageId).HasColumnName("stage_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'open'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Company).WithMany(p => p.Opportunities)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("fk_opportunities_company");

            entity.HasOne(d => d.Contact).WithMany(p => p.Opportunities)
                .HasForeignKey(d => d.ContactId)
                .HasConstraintName("fk_opportunities_contact");

            entity.HasOne(d => d.Pipeline).WithMany(p => p.Opportunities)
                .HasForeignKey(d => d.PipelineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_opportunities_pipeline");

            entity.HasOne(d => d.Stage).WithMany(p => p.Opportunities)
                .HasForeignKey(d => d.StageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_opportunities_stage");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Opportunities)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_opportunities_workspace");
        });

        modelBuilder.Entity<OpportunityMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("opportunity_members_pkey");

            entity.ToTable("opportunity_members");

            entity.HasIndex(e => e.UserId, "idx_opportunity_members_user_id");

            entity.HasIndex(e => new { e.OpportunityId, e.UserId }, "uq_opportunity_members").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("assigned_at");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.OpportunityId).HasColumnName("opportunity_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Opportunity).WithMany(p => p.OpportunityMembers)
                .HasForeignKey(d => d.OpportunityId)
                .HasConstraintName("fk_opportunity_members_opportunity");

            entity.HasOne(d => d.User).WithMany(p => p.OpportunityMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_opportunity_members_user");
        });

        modelBuilder.Entity<Pipeline>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pipelines_pkey");

            entity.ToTable("pipelines");

            entity.HasIndex(e => e.WorkspaceId, "idx_pipelines_workspace_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Pipelines)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pipelines_workspace");
        });

        modelBuilder.Entity<PipelineStage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pipeline_stages_pkey");

            entity.ToTable("pipeline_stages");

            entity.HasIndex(e => e.PipelineId, "idx_pipeline_stages_pipeline_id");

            entity.HasIndex(e => new { e.PipelineId, e.Position }, "uq_pipeline_stages_position").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PipelineId).HasColumnName("pipeline_id");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.StageType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'open'::character varying")
                .HasColumnName("stage_type");

            entity.HasOne(d => d.Pipeline).WithMany(p => p.PipelineStages)
                .HasForeignKey(d => d.PipelineId)
                .HasConstraintName("fk_pipeline_stages_pipeline");
        });

        modelBuilder.Entity<CRM.Api.Models.Task>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tasks_pkey");

            entity.ToTable("tasks");

            entity.HasIndex(e => new { e.WorkspaceId, e.DueDate }, "idx_tasks_due_date");

            entity.HasIndex(e => e.OpportunityId, "idx_tasks_opportunity_id");

            entity.HasIndex(e => e.WorkspaceId, "idx_tasks_workspace_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.OpportunityId).HasColumnName("opportunity_id");
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasDefaultValueSql("'medium'::character varying")
                .HasColumnName("priority");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'todo'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tasks_created_by");

            entity.HasOne(d => d.Opportunity).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.OpportunityId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_tasks_opportunity");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tasks_workspace");
        });

        modelBuilder.Entity<TaskAssignee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_assignees_pkey");

            entity.ToTable("task_assignees");

            entity.HasIndex(e => e.UserId, "idx_task_assignees_user_id");

            entity.HasIndex(e => new { e.TaskId, e.UserId }, "uq_task_assignees").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("assigned_at");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskAssignees)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("fk_task_assignees_task");

            entity.HasOne(d => d.User).WithMany(p => p.TaskAssignees)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_task_assignees_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("create_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.IsVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_verified");
            entity.Property(e => e.OtpLastUsedStep).HasColumnName("otp_last_used_step");
            entity.Property(e => e.OtpPendingExpiresAt).HasColumnName("otp_pending_expires_at");
            entity.Property(e => e.OtpPendingSecret).HasColumnName("otp_pending_secret");
            entity.Property(e => e.OtpSecret).HasColumnName("otp_secret");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("update_at");
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("workspaces_pkey");

            entity.ToTable("workspaces");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Workspaces)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_workspaces_created_by");
        });

        modelBuilder.Entity<WorkspaceInvitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("workspace_invitations_pkey");

            entity.ToTable("workspace_invitations");

            entity.HasIndex(e => e.Token, "workspace_invitations_token_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.InvitedBy).HasColumnName("invited_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.InvitedByNavigation).WithMany(p => p.WorkspaceInvitations)
                .HasForeignKey(d => d.InvitedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_invitations_invited_by");

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceInvitations)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_invitations_workspace");
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("workspace_members_pkey");

            entity.ToTable("workspace_members");

            entity.HasIndex(e => new { e.WorkspaceId, e.UserId }, "uq_workspace_members").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("joined_at");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValueSql("'viewer'::character varying")
                .HasColumnName("role");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.User).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_workspace_members_user");

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_workspace_members_workspace");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
