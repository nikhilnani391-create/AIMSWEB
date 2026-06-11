import { SnapService } from '../services/SnapService';
import { Snap } from '../models/Snap';

jest.mock('../models/Snap');

const MockedSnap = Snap as jest.Mocked<typeof Snap>;

describe('SnapService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
    jest.spyOn(console, 'log').mockImplementation(() => {});
    jest.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  describe('handleSnapViewed', () => {
    it('should throw "Snap not found" when snap does not exist', async () => {
      (MockedSnap.findById as jest.Mock).mockResolvedValue(null);

      await expect(SnapService.handleSnapViewed('nonexistent-id'))
        .rejects.toThrow('Snap not found');
    });

    it('should throw when snap status is already "viewed"', async () => {
      const mockSnap = { status: 'viewed', save: jest.fn() };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await expect(SnapService.handleSnapViewed('snap-id'))
        .rejects.toThrow('Snap already viewed or deleted');
    });

    it('should throw when snap status is already "deleted"', async () => {
      const mockSnap = { status: 'deleted', save: jest.fn() };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await expect(SnapService.handleSnapViewed('snap-id'))
        .rejects.toThrow('Snap already viewed or deleted');
    });

    it('should mark snap as viewed and set viewed_at timestamp', async () => {
      const mockSnap = {
        status: 'sent',
        viewed_at: null,
        duration: 5,
        media_url: 'https://s3.example.com/snap.jpg',
        save: jest.fn().mockResolvedValue(undefined),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await SnapService.handleSnapViewed('snap-id');

      expect(mockSnap.status).toBe('viewed');
      expect(mockSnap.viewed_at).toBeInstanceOf(Date);
      expect(mockSnap.save).toHaveBeenCalledTimes(1);
    });

    it('should handle snap with "delivered" status', async () => {
      const mockSnap = {
        status: 'delivered',
        viewed_at: null,
        duration: 3,
        media_url: 'https://s3.example.com/snap.mp4',
        save: jest.fn().mockResolvedValue(undefined),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await SnapService.handleSnapViewed('snap-id');

      expect(mockSnap.status).toBe('viewed');
      expect(mockSnap.save).toHaveBeenCalled();
    });

    it('should schedule deletion after snap duration', async () => {
      const mockSnap = {
        status: 'sent',
        viewed_at: null,
        duration: 5,
        media_url: 'https://s3.example.com/snap.jpg',
        save: jest.fn().mockResolvedValue(undefined),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      const deleteSpySnap = {
        status: 'viewed',
        save: jest.fn().mockResolvedValue(undefined),
      };

      await SnapService.handleSnapViewed('snap-id');

      // After duration seconds, deletion should be triggered
      (MockedSnap.findById as jest.Mock).mockResolvedValue(deleteSpySnap);
      jest.advanceTimersByTime(5000);

      // Allow async operations to resolve
      await Promise.resolve();
      await Promise.resolve();
    });

    it('should log snap viewed message', async () => {
      const mockSnap = {
        status: 'sent',
        viewed_at: null,
        duration: 7,
        media_url: 'https://s3.example.com/snap.jpg',
        save: jest.fn().mockResolvedValue(undefined),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await SnapService.handleSnapViewed('test-snap-123');

      expect(console.log).toHaveBeenCalledWith(
        'Snap test-snap-123 viewed. Scheduling deletion in 7 seconds.'
      );
    });

    it('should re-throw errors from findById', async () => {
      (MockedSnap.findById as jest.Mock).mockRejectedValue(new Error('DB connection failed'));

      await expect(SnapService.handleSnapViewed('snap-id'))
        .rejects.toThrow('DB connection failed');
    });

    it('should re-throw errors from save', async () => {
      const mockSnap = {
        status: 'sent',
        viewed_at: null,
        duration: 5,
        media_url: 'https://s3.example.com/snap.jpg',
        save: jest.fn().mockRejectedValue(new Error('Save failed')),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await expect(SnapService.handleSnapViewed('snap-id'))
        .rejects.toThrow('Save failed');
    });
  });

  describe('deleteSnapData', () => {
    it('should mark snap as deleted when snap exists', async () => {
      const mockSnap = {
        status: 'viewed',
        save: jest.fn().mockResolvedValue(undefined),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await SnapService.deleteSnapData('snap-id', 'https://s3.example.com/snap.jpg');

      expect(mockSnap.status).toBe('deleted');
      expect(mockSnap.save).toHaveBeenCalledTimes(1);
    });

    it('should log S3 deletion message', async () => {
      const mockSnap = {
        status: 'viewed',
        save: jest.fn().mockResolvedValue(undefined),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await SnapService.deleteSnapData('snap-id', 'media/snap123.jpg');

      expect(console.log).toHaveBeenCalledWith(
        '[Worker] Deleting media from S3: media/snap123.jpg'
      );
    });

    it('should log database deletion message', async () => {
      const mockSnap = {
        status: 'viewed',
        save: jest.fn().mockResolvedValue(undefined),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await SnapService.deleteSnapData('snap-id', 'media/snap123.jpg');

      expect(console.log).toHaveBeenCalledWith(
        '[Worker] Snap snap-id marked as deleted in database.'
      );
    });

    it('should handle case when snap is not found (already deleted)', async () => {
      (MockedSnap.findById as jest.Mock).mockResolvedValue(null);

      // Should not throw
      await expect(
        SnapService.deleteSnapData('nonexistent-id', 'media/snap.jpg')
      ).resolves.toBeUndefined();
    });

    it('should log error and not throw when findById fails', async () => {
      (MockedSnap.findById as jest.Mock).mockRejectedValue(new Error('DB error'));

      // Should not throw (errors are caught internally)
      await expect(
        SnapService.deleteSnapData('snap-id', 'media/snap.jpg')
      ).resolves.toBeUndefined();

      expect(console.error).toHaveBeenCalledWith(
        '[Worker] Error deleting snap snap-id:',
        expect.any(Error)
      );
    });

    it('should log error and not throw when save fails', async () => {
      const mockSnap = {
        status: 'viewed',
        save: jest.fn().mockRejectedValue(new Error('Save error')),
      };
      (MockedSnap.findById as jest.Mock).mockResolvedValue(mockSnap);

      await expect(
        SnapService.deleteSnapData('snap-id', 'media/snap.jpg')
      ).resolves.toBeUndefined();

      expect(console.error).toHaveBeenCalledWith(
        '[Worker] Error deleting snap snap-id:',
        expect.any(Error)
      );
    });
  });
});
