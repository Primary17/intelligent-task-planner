const TOKEN_KEY = 'taskplanner.token';

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
};

export class ApiError extends Error {
  constructor(message: string, public status: number) {
    super(message);
  }
}

let onUnauthorized: () => void = () => {};
export const setUnauthorizedHandler = (handler: () => void) => {
  onUnauthorized = handler;
};

interface ProblemBody {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function problemToMessage(body: ProblemBody | null, fallback: string): string {
  if (!body) return fallback;
  if (body.errors) {
    const messages = Object.values(body.errors).flat();
    if (messages.length > 0) return messages.join(' ');
  }
  return body.detail ?? body.title ?? fallback;
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Сталася невідома помилка.';
}

export async function request<T>(
  path: string,
  options: { method?: string; body?: unknown } = {},
): Promise<T> {
  const headers: Record<string, string> = {};
  const token = tokenStore.get();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (options.body !== undefined) headers['Content-Type'] = 'application/json';

  let response: Response;
  try {
    response = await fetch(`/api${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    });
  } catch {
    throw new ApiError('Не вдалося зʼєднатися з сервером. Перевірте, чи запущений API.', 0);
  }

  if (response.status === 401 && token) {
    onUnauthorized();
  }

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as ProblemBody | null;
    const fallback: Record<number, string> = {
      401: 'Потрібна авторизація.',
      403: 'Недостатньо прав для цієї дії.',
      404: 'Запис не знайдено.',
    };
    throw new ApiError(
      problemToMessage(body, fallback[response.status] ?? `Помилка сервера (${response.status}).`),
      response.status,
    );
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}
