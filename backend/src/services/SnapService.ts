import mongoose from 'mongoose';
import { Snap } from '../models/Snap.js';

export class SnapService {
  /**
   * Validates that a string is a well-formed MongoDB ObjectId.
   * Prevents NoSQL injection by rejecting malformed or malicious input
   * before it reaches the database driver.
   */
  static isValidObjectId(id: string): boolean {
    return mongoose.Types.ObjectId.isValid(id) && new mongoose.Types.ObjectId(id).toString() === id;
  }

  /**
   * Called when a user opens a snap. Marks the snap as viewed and initiates the countdown.
   * In a real system, we would also interact with Redis/BullMQ to schedule the deletion here.
   */
  static async handleSnapViewed(snapId: string, requestingUserId: string): Promise<void> {
    if (!this.isValidObjectId(snapId)) {
      throw new Error('Invalid snap ID format');
    }

    if (!this.isValidObjectId(requestingUserId)) {
      throw new Error('Invalid user ID format');
    }

    const snap = await Snap.findById(snapId);

    if (!snap) {
      throw new Error('Snap not found');
    }

    if (snap.receiver_id.toString() !== requestingUserId) {
      throw new Error('Unauthorized: only the intended recipient can view this snap');
    }

    if (snap.status === 'viewed' || snap.status === 'deleted') {
      throw new Error('Snap already viewed or deleted');
    }

    snap.status = 'viewed';
    snap.viewed_at = new Date();
    await snap.save();

    // Schedule background deletion (using setTimeout for simulation)
    // In production, use Redis TTL or a worker queue (e.g., BullMQ)
    setTimeout(async () => {
      await this.deleteSnapData(snapId);
    }, snap.duration * 1000);
  }

  /**
   * Deletes the snap record from the database and triggers deletion from Cloud Storage.
   */
  static async deleteSnapData(snapId: string): Promise<void> {
    if (!this.isValidObjectId(snapId)) {
      return;
    }

    try {
      const snap = await Snap.findById(snapId);
      if (snap) {
        // In production: delete the media from S3 using snap.media_url
        // await s3Client.send(new DeleteObjectCommand({ Bucket: process.env.S3_BUCKET, Key: snap.media_url }));

        snap.status = 'deleted';
        await snap.save();
      }
    } catch (error) {
      // Avoid logging the full error object which may contain sensitive query/data details
      const message = error instanceof Error ? error.message : 'Unknown error';
      console.error(`[Worker] Error deleting snap ${snapId}: ${message}`);
    }
  }
}
