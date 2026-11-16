export type DayName =
  | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday';
export type Priority = 'Low' | 'Medium' | 'High' | 'Critical';
export type TaskStatus = 'NotStarted' | 'InProgress' | 'Completed';
export type ExceptionKind = 'Unavailable' | 'CustomHours';
export type SessionStatus = 'Planned' | 'Completed' | 'Missed';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  role: 'User' | 'Admin';
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: UserProfile;
}

export interface Discipline {
  id: string;
  name: string;
  description: string | null;
  color: string;
  taskCount: number;
}

export interface DisciplineInput {
  name: string;
  description: string | null;
  color: string;
}

export interface StudyTask {
  id: string;
  disciplineId: string;
  disciplineName: string;
  title: string;
  deadlineUtc: string;
  complexity: number;
  priority: Priority;
  estimatedMinutes: number;
  actualMinutes: number;
  status: TaskStatus;
}

export interface CreateTaskInput {
  disciplineId: string;
  title: string;
  deadlineUtc: string;
  complexity: number;
  priority: Priority;
  estimatedMinutes: number;
  status: TaskStatus;
}

export interface UpdateTaskInput {
  title: string;
  deadlineUtc: string;
  complexity: number;
  priority: Priority;
  estimatedMinutes: number;
  actualMinutes: number;
  status: TaskStatus;
}

export interface TaskFilters {
  disciplineId?: string;
  status?: TaskStatus;
  minPriority?: Priority;
  search?: string;
  sort?: 'deadline' | 'priority' | 'title';
}

export interface AvailabilityWindow {
  id: string;
  dayOfWeek: DayName;
  startTime: string;
  endTime: string;
}

export interface AvailabilityWindowInput {
  dayOfWeek: DayName;
  startTime: string;
  endTime: string;
}

export interface AvailabilityException {
  id: string;
  date: string;
  kind: ExceptionKind;
  startTime: string | null;
  endTime: string | null;
  reason: string | null;
}

export interface AvailabilityExceptionInput {
  date: string;
  kind: ExceptionKind;
  startTime: string | null;
  endTime: string | null;
  reason: string | null;
}

export interface TimeInterval {
  startTime: string;
  endTime: string;
}

export interface EffectiveDay {
  date: string;
  source: 'Weekly' | ExceptionKind;
  intervals: TimeInterval[];
}

export interface StudySession {
  id: string;
  studyTaskId: string;
  taskTitle: string;
  disciplineId: string;
  disciplineName: string;
  startAtUtc: string;
  endAtUtc: string;
  plannedMinutes: number;
  actualMinutes: number | null;
  status: SessionStatus;
  isLocked: boolean;
}

export interface CreateSessionInput {
  studyTaskId: string;
  startAtUtc: string;
  endAtUtc: string;
  isLocked: boolean;
}

export interface UpdateSessionInput {
  startAtUtc: string;
  endAtUtc: string;
  status: SessionStatus;
  actualMinutes: number | null;
  isLocked: boolean;
}

export type CapacityStatus = 'Ok' | 'Shortfall' | 'Overdue';

export interface TaskCapacity {
  taskId: string;
  title: string;
  disciplineId: string;
  disciplineName: string;
  deadlineUtc: string;
  remainingMinutes: number;
  plannedMinutes: number;
  unplannedMinutes: number;
  availableMinutes: number;
  requiredMinutes: number;
  shortfallMinutes: number;
  status: CapacityStatus;
}

export interface CapacityReport {
  generatedAtUtc: string;
  totalShortfallMinutes: number;
  tasks: TaskCapacity[];
}

export interface UpdateProfileInput {
  displayName: string;
}

export interface ChangePasswordInput {
  currentPassword: string;
  newPassword: string;
}

export interface ScheduleRequest {
  from: string;
  to: string;
  timeZoneId?: string;
}

export interface GeneratedSession {
  id: string;
  studyTaskId: string;
  startAtUtc: string;
  endAtUtc: string;
  plannedMinutes: number;
  status: SessionStatus;
  isLocked: boolean;
}

export interface UnscheduledWork {
  studyTaskId: string;
  remainingMinutes: number;
  scheduledMinutes: number;
}

export interface ScheduleConflict {
  firstSessionId: string;
  secondSessionId: string;
  overlapStartUtc: string;
  overlapEndUtc: string;
}

export interface GenerateScheduleResponse {
  from: string;
  to: string;
  sessions: GeneratedSession[];
  unscheduled: UnscheduledWork[];
  conflicts: ScheduleConflict[];
}

export interface ReplanScheduleResponse {
  from: string;
  to: string;
  stayed: GeneratedSession[];
  created: GeneratedSession[];
  removed: GeneratedSession[];
  unscheduled: UnscheduledWork[];
  conflicts: ScheduleConflict[];
}
