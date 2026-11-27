import type { IncomingInvitation, Invitation, OpportunityOption, Option, PipelineOpportunity, RecordItem, Statistics, Workspace } from '@/types/crm'
import { api } from './api'

const endpoints: Record<string, { path: string; paginated?: boolean; queryOnItem?: boolean }> = {
  contacts: { path: '/contacts', paginated: true, queryOnItem: true },
  companies: { path: '/companies', queryOnItem: true },
  pipelines: { path: '/pipelines' },
  opportunities: { path: '/opportunities' },
  tasks: { path: '/tasks', paginated: true },
  calendar: { path: '/calendar-events', paginated: true },
  members: { path: '/workspaces/{workspaceId}/members' },
}

function endpoint(section: string) {
  const config = endpoints[section]
  if (!config) throw new Error('Không tìm thấy mục dữ liệu.')
  return config
}

export function listWorkspaces() { return api<Workspace[]>('/workspaces') }
export function listArchivedWorkspaces() { return api<Workspace[]>('/workspaces/archived') }
export function createWorkspace(name: string, description: string) {
  return api<Workspace>('/workspaces', { method: 'POST', body: JSON.stringify({ name, description: description || null }) })
}
export function updateWorkspace(id: string, name: string, description: string) {
  return api<Workspace>(`/workspaces/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ name, description: description || null }),
  })
}
export function archiveWorkspace(id: string) {
  return api<void>(`/workspaces/${id}`, { method: 'DELETE' })
}
export function restoreWorkspace(id: string) {
  return api<Workspace>(`/workspaces/${id}/restore`, { method: 'POST' })
}
export function ensurePersonalWorkspace() {
  return api<Workspace>('/workspaces/personal', { method: 'POST' })
}
export function createWorkspaceInvitation(workspaceId: string, email: string) {
  return api<Invitation>(`/workspaces/${workspaceId}/invitations`, { method: 'POST', body: JSON.stringify({ email }) })
}
export function acceptWorkspaceInvitation(token: string) {
  return api<{ invitationId: string; status: string }>(`/invitations/${encodeURIComponent(token)}/accept`, { method: 'POST' })
}
export function listIncomingInvitations() {
  return api<IncomingInvitation[]>('/invitations')
}
export function respondToInvitation(id: string, response: 'accept' | 'reject') {
  return api<{ invitationId: string; status: string }>(`/invitations/${id}/${response}`, { method: 'POST' })
}
export function getStatistics(workspaceId: string) {
  return api<Statistics>(`/workspaces/${workspaceId}/statistics`)
}
export function listRecords(section: string, workspaceId: string, page: number) {
  const config = endpoint(section)
  const path = config.path.replace('{workspaceId}', workspaceId)
  const query = path.includes('/workspaces/') ? '' : `?workspaceId=${encodeURIComponent(workspaceId)}${config.paginated ? `&page=${page}&pageSize=50` : ''}`
  return api<RecordItem[] | { items: RecordItem[]; total: number }>(path + query)
}
export function saveRecord(section: string, workspaceId: string, id: string | undefined, body: object) {
  const config = endpoint(section)
  const path = config.path + (id ? `/${id}` : '') + (id && config.queryOnItem ? `?workspaceId=${encodeURIComponent(workspaceId)}` : '')
  return api<void>(path, { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) })
}
export function deleteRecord(section: string, workspaceId: string, id: string) {
  const config = endpoint(section)
  const path = config.path.replace('{workspaceId}', workspaceId)
  return api<void>(`${path}/${id}${config.queryOnItem ? `?workspaceId=${encodeURIComponent(workspaceId)}` : ''}`, { method: 'DELETE' })
}
export function updateWorkspaceMemberRole(workspaceId: string, userId: string, role: 'editor' | 'viewer') {
  return api<void>(`/workspaces/${workspaceId}/members/${userId}/role`, {
    method: 'PUT',
    body: JSON.stringify({ role }),
  })
}
export function removeWorkspaceMember(workspaceId: string, userId: string) {
  return api<void>(`/workspaces/${workspaceId}/members/${userId}`, { method: 'DELETE' })
}
export function listPipelines(workspaceId: string) {
  return api<Option[]>(`/pipelines?workspaceId=${encodeURIComponent(workspaceId)}`)
}
export function listStages<T extends Option | RecordItem>(pipelineId: string) {
  return api<T[]>(`/pipelines/${pipelineId}/stages`)
}
export function addStage(pipelineId: string, name: string, position: number) {
  return api<void>(`/pipelines/${pipelineId}/stages`, { method: 'POST', body: JSON.stringify({ name, position, stageType: 'open' }) })
}
export function deleteStage(stageId: string) {
  return api<void>(`/pipeline-stages/${stageId}`, { method: 'DELETE' })
}
export function renameStage(stage: RecordItem, name: string) {
  return api<void>(`/pipeline-stages/${stage.id}`, { method: 'PUT', body: JSON.stringify({ name, position: stage.position, stageType: stage.stageType }) })
}
export function reorderStages(pipelineId: string, stageIds: string[]) {
  return api<void>(`/pipelines/${pipelineId}/stages/reorder`, { method: 'PATCH', body: JSON.stringify({ stageIds }) })
}
export function listOpportunities(workspaceId: string) {
  return api<OpportunityOption[]>(`/opportunities?workspaceId=${encodeURIComponent(workspaceId)}`)
}
export function listPipelineOpportunities(workspaceId: string) {
  return api<PipelineOpportunity[]>(`/opportunities?workspaceId=${encodeURIComponent(workspaceId)}`)
}
export function moveOpportunity(id: string, stageId: string) {
  return api<PipelineOpportunity>(`/opportunities/${id}/stage`, { method: 'PATCH', body: JSON.stringify({ stageId }) })
}
export function saveActivity(kind: 'tasks' | 'calendar', id: string | undefined, body: object) {
  const path = kind === 'tasks' ? '/tasks' : '/calendar-events'
  return api<void>(`${path}${id ? `/${id}` : ''}`, { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) })
}
