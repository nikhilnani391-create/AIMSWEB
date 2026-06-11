export class SnapNotFoundError extends Error {
  constructor(snapId: string) {
    super(`Snap not found: ${snapId}`);
    this.name = 'SnapNotFoundError';
  }
}

export class SnapInvalidStateError extends Error {
  constructor(snapId: string, currentStatus: string) {
    super(`Snap ${snapId} is in an invalid state for this operation: ${currentStatus}`);
    this.name = 'SnapInvalidStateError';
  }
}

export class SnapDeletionError extends Error {
  public readonly snapId: string;

  constructor(snapId: string, cause: unknown) {
    const message = cause instanceof Error ? cause.message : String(cause);
    super(`Failed to delete snap ${snapId}: ${message}`);
    this.name = 'SnapDeletionError';
    this.snapId = snapId;
    this.cause = cause;
  }
}

export class InvalidSnapIdError extends Error {
  constructor(snapId: string) {
    super(`Invalid snap ID format: ${snapId}`);
    this.name = 'InvalidSnapIdError';
  }
}
