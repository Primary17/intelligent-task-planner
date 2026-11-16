import { request } from './http';
import type {
  AuthResponse, AvailabilityException, AvailabilityExceptionInput, AvailabilityWindow,
  AvailabilityWindowInput, CreateSessionInput, CreateTaskInput, Discipline, DisciplineInput, EffectiveDay,
  CapacityReport, ChangePasswordInput, GenerateScheduleResponse, ReplanScheduleResponse, ScheduleRequest, StudySession, StudyTask, TaskFilters, UpdateProfileInput,
  UpdateSessionInput, UpdateTaskInput, UserProfile,
} from './types';

const post = <T>(path: string, body: unknown) => request<T>(path, { method: 'POST', body });
const put = <T>(path: string, body: unknown) => request<T>(path, { method: 'PUT', body });
const del = (path: string) => request<void>(path, { method: 'DELETE' });

export function buildQuery(params: Record<string, string | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value) search.set(key, value);
  }
  const text = search.toString();
  return text ? `?${text}` : '';
}

export const authApi = {
  login: (email: string, password: string) => post<AuthResponse>('/auth/login', { email, password }),
  register: (email: string, displayName: string, password: string) =>
    post<AuthResponse>('/auth/register', { email, displayName, password }),
  me: () => request<UserProfile>('/auth/me'),
  updateProfile: (input: UpdateProfileInput) => put<UserProfile>('/auth/me', input),
  changePassword: (input: ChangePasswordInput) => post<void>('/auth/change-password', input),
};

export const disciplinesApi = {
  list: () => request<Discipline[]>('/disciplines'),
  create: (input: DisciplineInput) => post<Discipline>('/disciplines', input),
  update: (id: string, input: DisciplineInput) => put<Discipline>(`/disciplines/${id}`, input),
  remove: (id: string) => del(`/disciplines/${id}`),
};

export const tasksApi = {
  list: (filters: TaskFilters = {}) => request<StudyTask[]>(`/tasks${buildQuery({ ...filters })}`),
  create: (input: CreateTaskInput) => post<StudyTask>('/tasks', input),
  update: (id: string, input: UpdateTaskInput) => put<StudyTask>(`/tasks/${id}`, input),
  remove: (id: string) => del(`/tasks/${id}`),
};

export const availabilityApi = {
  windows: () => request<AvailabilityWindow[]>('/availability/windows'),
  createWindow: (input: AvailabilityWindowInput) => post<AvailabilityWindow>('/availability/windows', input),
  updateWindow: (id: string, input: AvailabilityWindowInput) =>
    put<AvailabilityWindow>(`/availability/windows/${id}`, input),
  removeWindow: (id: string) => del(`/availability/windows/${id}`),

  exceptions: (from?: string, to?: string) =>
    request<AvailabilityException[]>(`/availability/exceptions${buildQuery({ from, to })}`),
  createException: (input: AvailabilityExceptionInput) =>
    post<AvailabilityException>('/availability/exceptions', input),
  updateException: (id: string, input: AvailabilityExceptionInput) =>
    put<AvailabilityException>(`/availability/exceptions/${id}`, input),
  removeException: (id: string) => del(`/availability/exceptions/${id}`),

  effective: (from: string, to: string) =>
    request<EffectiveDay[]>(`/availability/effective${buildQuery({ from, to })}`),
};

export const sessionsApi = {
  list: (from?: string, to?: string) => request<StudySession[]>(`/sessions${buildQuery({ from, to })}`),
  create: (input: CreateSessionInput) => post<StudySession>('/sessions', input),
  update: (id: string, input: UpdateSessionInput) => put<StudySession>(`/sessions/${id}`, input),
  remove: (id: string) => del(`/sessions/${id}`),
};

export const planningApi = {
  capacity: (utcOffsetMinutes: number) =>
    request<CapacityReport>(`/planning/capacity${buildQuery({ utcOffsetMinutes: String(utcOffsetMinutes) })}`),
};

export const scheduleApi = {
  generate: (input: ScheduleRequest) => post<GenerateScheduleResponse>('/schedule/generate', input),
  replan: (input: ScheduleRequest) => post<ReplanScheduleResponse>('/schedule/replan', input),
};
