import type { ApiResponse } from './deviceMonitoring'

export interface CurrentUser {
  username: string
  displayName: string
  role: 'Administrator' | 'Operator'
}

export interface UserAccount {
  id: string
  username: string
  displayName: string
  role: 'Administrator' | 'Operator'
  isEnabled: boolean
  createdAt: string
  lastLoginAt: string | null
}

export interface CreateUserRequest {
  username: string
  displayName: string
  password: string
  role: 'Administrator' | 'Operator'
}

async function request<T>(url: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(url, {
    ...options,
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
  })
  if (response.status === 401) throw new Error('UNAUTHORIZED')
  if (response.status === 403) throw new Error('FORBIDDEN')
  const body = (await response.json()) as ApiResponse<T>
  if (!response.ok || !body.success) throw new Error(body.message || body.code)
  return body.data
}

export const identityApi = {
  getSession: () => request<CurrentUser>('/api/v1/auth/session'),
  login: (username: string, password: string) => request<CurrentUser>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  }),
  logout: () => request<object>('/api/v1/auth/logout', { method: 'POST' }),
  getUsers: () => request<UserAccount[]>('/api/v1/admin/users'),
  createUser: (input: CreateUserRequest) => request<UserAccount>('/api/v1/admin/users', {
    method: 'POST',
    body: JSON.stringify(input),
  }),
  deleteUser: (userId: string) => request<object>(`/api/v1/admin/users/${encodeURIComponent(userId)}`, {
    method: 'DELETE',
  }),
}
