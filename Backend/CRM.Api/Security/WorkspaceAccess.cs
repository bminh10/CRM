using CRM.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Security;

public static class WorkspaceAccess
{
    public static Task<bool> IsMemberAsync(CrmDbContext db, Guid workspaceId, Guid userId, CancellationToken cancellationToken = default) =>
        db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId
            && !x.Workspace.IsArchived, cancellationToken);

    public static Task<bool> CanEditAsync(CrmDbContext db, Guid workspaceId, Guid userId, CancellationToken cancellationToken = default) =>
        db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId
            && !x.Workspace.IsArchived
            && (x.Role == "owner" || x.Role == "editor" || x.Workspace.CreatedBy == userId), cancellationToken);

    public static Task<bool> IsOwnerAsync(
        CrmDbContext db, Guid workspaceId, Guid userId, bool includeArchived = false, CancellationToken cancellationToken = default) =>
        db.WorkspaceMembers.AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId
            && (includeArchived || !x.Workspace.IsArchived)
            && (x.Role == "owner" || x.Workspace.CreatedBy == userId), cancellationToken);
}
