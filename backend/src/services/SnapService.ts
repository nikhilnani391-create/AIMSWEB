import mongoose from 'mongoose';
import { Snap } from '../models/Snap.js';
import {
  InvalidSnapIdError,
  SnapDeletionError,
  SnapInvalidStateError,
  SnapNotFoundError,
} from '../errors/SnapErrors.js';

const MAX_DELETION_RETRIES = 3;
const RETRY_DELAY_MS = 1000;

export class SnapService {
  /**
   * Called when a user opens a snap. Marks the snap as viewed and initiates the countdown.
   * In a real system, we would also interact with Redis/BullMQ to schedule the deletion here.
   */
  static async handleSnapViewed(snapId: string): Promise<void> {
    if (!mongoose.Types.ObjectId.isValid(snapId)) {
      throw new InvalidSnapIdError(snapId);
    }

    const snap = await Snap.findById(snapId);

    if (!snap) {
      throw new SnapNotFoundError(snapId);
    }

    if (snap.status === 'viewed' || snap.status === 'deleted') {
      throw new SnapInvalidStateError(snapId, snap.status);
    }

    snap.status = 'viewed';
    snap.viewed_at = new Date();
    await snap.save();

    console.log(`Snap ${snapId} viewed. Scheduling deletion in ${snap.duration} seconds.`);

    // Schedule background deletion (using setTimeout for simulation).
    // In production, use Redis TTL or a worker queue (e.g., BullMQ).
    const mediaUrl = snap.media_url;
    setTimeout(() => {
      SnapService.deleteSnapData(snapId, mediaUrl).catch((error: unknown) => {
        console.error(`[Scheduler] Scheduled deletion failed for snap ${snapId}:`, error);
        // In production, push to a dead-letter queue for manual inspection.
      });
    }, snap.duration * 1000);
  }

  /**
   * Deletes the snap's media from storage and marks the record as deleted.
   * Retries transient failures up to MAX_DELETION_RETRIES times before giving up.
   * Throws SnapDeletionError on failure so callers can handle it (e.g., dead-letter queue).
   */
  static async deleteSnapData(snapId: string, mediaUrl: string): Promise<void> {
    let lastError: unknown;

    for (let attempt = 1; attempt <= MAX_DELETION_RETRIES; attempt++) {
      try {
        // 1. Delete from S3 (Mocked)
        console.log(`[Worker] Deleting media from S3: ${mediaUrl} (attempt ${attempt})`);
        // await s3Client.send(new DeleteObjectCommand({ Bucket: process.env.S3_BUCKET, Key: mediaUrl }));

        // 2. Mark as deleted in DB
        const snap = await Snap.findById(snapId);
        if (!snap) {
          console.warn(`[Worker] Snap ${snapId} not found in database during deletion — may have been removed already.`);
          return;
        }

        snap.status = 'deleted';
        await snap.save();
        console.log(`[Worker] Snap ${snapId} marked as deleted in database.`);
        return;
      } catch (error: unknown) {
        lastError = error;
        console.error(`[Worker] Attempt ${attempt}/${MAX_DELETION_RETRIES} failed for snap ${snapId}:`, error);

        if (attempt < MAX_DELETION_RETRIES) {
          await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS * attempt));
        }
      }
    }

    throw new SnapDeletionError(snapId, lastError);
  }
}
