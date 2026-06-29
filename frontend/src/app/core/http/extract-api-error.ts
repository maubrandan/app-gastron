import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetailsBody {
  detail?: string;
  error?: string;
  title?: string;
}

export function extractApiError(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error;
    if (body && typeof body === 'object') {
      const problem = body as ProblemDetailsBody;
      if (typeof problem.detail === 'string' && problem.detail.length > 0) {
        return problem.detail;
      }
      if (typeof problem.error === 'string' && problem.error.length > 0) {
        return problem.error;
      }
      if (typeof problem.title === 'string' && problem.title.length > 0) {
        return problem.title;
      }
    }
    return fallback;
  }

  if (error instanceof Error && error.message.length > 0) {
    return error.message;
  }

  return fallback;
}

export function isConcurrencyConflict(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 409;
}
