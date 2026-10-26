import { HttpErrorResponse } from '@angular/common/http';

/** RFC 9457 problem details as returned by the Products API. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
  traceId?: string;
}

/** Flattens an HTTP error into user-presentable messages. */
export function toErrorMessages(error: unknown): string[] {
  if (!(error instanceof HttpErrorResponse)) {
    return ['An unexpected error occurred.'];
  }

  if (error.status === 0) {
    return ['Unable to reach the server. Please check your connection.'];
  }

  const problem = error.error as ProblemDetails | null;
  if (problem?.errors) {
    return Object.values(problem.errors).flat();
  }

  if (problem?.detail) {
    return [problem.detail];
  }

  if (problem?.title) {
    return [problem.title];
  }

  switch (error.status) {
    case 401:
      return ['Your session has expired. Please sign in again.'];
    case 403:
      return ['You do not have permission to perform this action.'];
    default:
      return ['An unexpected error occurred.'];
  }
}
