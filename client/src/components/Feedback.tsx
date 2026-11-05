import type { ReactNode } from 'react';

export function ErrorBanner({ message }: { message: string | null }) {
  return message ? <div className="alert alert-error" role="alert">{message}</div> : null;
}

export function Loading({ text = 'Завантаження…' }: { text?: string }) {
  return <p className="muted">{text}</p>;
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <div className="empty">{children}</div>;
}
