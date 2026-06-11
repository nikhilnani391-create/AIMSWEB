import { Snap } from '../models/Snap.js';

export class SnapService {
  /**
   * Called when a user opens a snap. Marks the snap as viewed and initiates the countdown.
   * In a real system, we would also interact with Redis/BullMQ to schedule the deletion here.
   */
  static async handleSnapViewed(snapId: string): Promise<void> {
    try {
      const snap = await Snap.findById(snapId);

      if (!snap) {
        throw new Error('Snap not found');
      }

      if (snap.status === 'viewed' || snap.status === 'deleted') {
        throw new Error('Snap already viewed or deleted');
      }

      // Mark as viewed
      snap.status = 'viewed';
      snap.viewed_at = new Date();
      await snap.save();

      console.log(`Snap ${snapId} viewed. Scheduling deletion in ${snap.duration} seconds.`);

      // Schedule background deletion (using setTimeout for simulation)
      // In production, use Redis TTL or a worker queue (e.g., BullMQ)
      setTimeout(async () => {
        await this.deleteSnapData(snapId, snap.media_url);
      }, snap.duration * 1000);

    } catch (error) {
      console.error('Error handling snap view:', error);
      throw error;
    }
  }

  /**
   * Deletes the snap record from the database and triggers deletion from Cloud Storage.
   */
  static async deleteSnapData(snapId: string, mediaUrl: string): Promise<void> {
    try {
      // 1. Delete from S3 (Mocked)
      console.log(`[Worker] Deleting media from S3: ${mediaUrl}`);
      // await s3Client.send(new DeleteObjectCommand({ Bucket: process.env.S3_BUCKET, Key: mediaUrl }));

      // 2. Mark as deleted in DB or hard delete
      const snap = await Snap.findById(snapId);
      if (snap) {
         snap.status = 'deleted';
         await snap.save();
         console.log(`[Worker] Snap ${snapId} marked as deleted in database.`);
      }
    } catch (error) {
      console.error(`[Worker] Error deleting snap ${snapId}:`, error);
    }
  }
}
