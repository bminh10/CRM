export type Value = string | number | boolean | null
export type RecordItem = { id: string; [key: string]: Value | string[] }
export interface Workspace { id: string; name: string }
export interface Invitation { id: string; workspaceId: string; email: string; status: string; expiresAt: string; token?: string | null }
export interface IncomingInvitation {
  id: string
  workspaceId: string
  workspaceName: string
  email: string
  invitedByName: string
  expiresAt: string
  createdAt: string | null
}
export type Statistics = Record<'contacts' | 'companies' | 'opportunities' | 'tasks' | 'events' | 'members' | 'pipelines', number>
export interface Option { id: string; name: string }
export interface OpportunityOption { id: string; title: string }
export interface PipelineOpportunity {
  id: string
  pipelineId: string
  stageId: string
  title: string
  expectedValue: number
  probability: number
  status: string
}
