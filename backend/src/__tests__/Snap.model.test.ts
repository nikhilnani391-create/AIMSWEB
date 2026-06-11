import mongoose from 'mongoose';
import { MongoMemoryServer } from 'mongodb-memory-server';
import { Snap, ISnap } from '../models/Snap';

let mongoServer: MongoMemoryServer;

describe('Snap Model', () => {
  beforeAll(async () => {
    mongoServer = await MongoMemoryServer.create();
    await mongoose.connect(mongoServer.getUri());
  });

  afterAll(async () => {
    await mongoose.connection.dropDatabase();
    await mongoose.connection.close();
    await mongoServer.stop();
  });

  afterEach(async () => {
    await Snap.deleteMany({});
  });

  const validSnapData = {
    sender_id: new mongoose.Types.ObjectId(),
    receiver_id: new mongoose.Types.ObjectId(),
    media_url: 'https://s3.amazonaws.com/bucket/snap123.jpg',
    media_type: 'image' as const,
    duration: 5,
  };

  describe('Schema Validation', () => {
    it('should create a snap with valid data', async () => {
      const snap = new Snap(validSnapData);
      const savedSnap = await snap.save();

      expect(savedSnap._id).toBeDefined();
      expect(savedSnap.sender_id.toString()).toBe(validSnapData.sender_id.toString());
      expect(savedSnap.receiver_id.toString()).toBe(validSnapData.receiver_id.toString());
      expect(savedSnap.media_url).toBe(validSnapData.media_url);
      expect(savedSnap.media_type).toBe('image');
      expect(savedSnap.duration).toBe(5);
    });

    it('should set default status to "sent"', async () => {
      const snap = new Snap(validSnapData);
      const savedSnap = await snap.save();
      expect(savedSnap.status).toBe('sent');
    });

    it('should set default viewed_at to null', async () => {
      const snap = new Snap(validSnapData);
      const savedSnap = await snap.save();
      expect(savedSnap.viewed_at).toBeNull();
    });

    it('should set created_at automatically', async () => {
      const snap = new Snap(validSnapData);
      const savedSnap = await snap.save();
      expect(savedSnap.created_at).toBeInstanceOf(Date);
    });

    it('should reject snap without sender_id', async () => {
      const { sender_id, ...dataWithoutSender } = validSnapData;
      const snap = new Snap(dataWithoutSender);
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should reject snap without receiver_id', async () => {
      const { receiver_id, ...dataWithoutReceiver } = validSnapData;
      const snap = new Snap(dataWithoutReceiver);
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should reject snap without media_url', async () => {
      const { media_url, ...dataWithoutUrl } = validSnapData;
      const snap = new Snap(dataWithoutUrl);
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should reject snap without media_type', async () => {
      const { media_type, ...dataWithoutType } = validSnapData;
      const snap = new Snap(dataWithoutType);
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should reject snap without duration', async () => {
      const { duration, ...dataWithoutDuration } = validSnapData;
      const snap = new Snap(dataWithoutDuration);
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should reject invalid media_type', async () => {
      const snap = new Snap({ ...validSnapData, media_type: 'audio' });
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should reject invalid status', async () => {
      const snap = new Snap({ ...validSnapData, status: 'expired' });
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should reject duration less than 1', async () => {
      const snap = new Snap({ ...validSnapData, duration: 0 });
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should reject duration greater than 10', async () => {
      const snap = new Snap({ ...validSnapData, duration: 11 });
      await expect(snap.validate()).rejects.toThrow();
    });

    it('should accept duration of 1 (minimum)', async () => {
      const snap = new Snap({ ...validSnapData, duration: 1 });
      const savedSnap = await snap.save();
      expect(savedSnap.duration).toBe(1);
    });

    it('should accept duration of 10 (maximum)', async () => {
      const snap = new Snap({ ...validSnapData, duration: 10 });
      const savedSnap = await snap.save();
      expect(savedSnap.duration).toBe(10);
    });

    it('should accept "video" media_type', async () => {
      const snap = new Snap({ ...validSnapData, media_type: 'video' });
      const savedSnap = await snap.save();
      expect(savedSnap.media_type).toBe('video');
    });

    it('should accept all valid status values', async () => {
      const statuses = ['sent', 'delivered', 'viewed', 'deleted'] as const;
      for (const status of statuses) {
        const snap = new Snap({ ...validSnapData, status });
        const savedSnap = await snap.save();
        expect(savedSnap.status).toBe(status);
      }
    });
  });

  describe('CRUD Operations', () => {
    it('should find a snap by id', async () => {
      const snap = await new Snap(validSnapData).save();
      const found = await Snap.findById(snap._id);
      expect(found).not.toBeNull();
      expect(found!.media_url).toBe(validSnapData.media_url);
    });

    it('should update snap status', async () => {
      const snap = await new Snap(validSnapData).save();
      snap.status = 'viewed';
      snap.viewed_at = new Date();
      await snap.save();

      const updated = await Snap.findById(snap._id);
      expect(updated!.status).toBe('viewed');
      expect(updated!.viewed_at).toBeInstanceOf(Date);
    });

    it('should delete a snap', async () => {
      const snap = await new Snap(validSnapData).save();
      await Snap.findByIdAndDelete(snap._id);
      const found = await Snap.findById(snap._id);
      expect(found).toBeNull();
    });

    it('should find snaps by sender_id', async () => {
      const senderId = new mongoose.Types.ObjectId();
      await new Snap({ ...validSnapData, sender_id: senderId }).save();
      await new Snap({ ...validSnapData, sender_id: senderId }).save();

      const snaps = await Snap.find({ sender_id: senderId });
      expect(snaps).toHaveLength(2);
    });

    it('should find snaps by receiver_id', async () => {
      const receiverId = new mongoose.Types.ObjectId();
      await new Snap({ ...validSnapData, receiver_id: receiverId }).save();

      const snaps = await Snap.find({ receiver_id: receiverId });
      expect(snaps).toHaveLength(1);
    });
  });
});
