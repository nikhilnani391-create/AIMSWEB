import mongoose, { Schema, Document, Types } from 'mongoose';

export interface ISnap extends Document {
  sender_id: Types.ObjectId;
  receiver_id: Types.ObjectId;
  media_url: string;
  media_type: 'image' | 'video';
  duration: number;
  status: 'sent' | 'delivered' | 'viewed' | 'deleted';
  viewed_at?: Date | null;
  created_at: Date;
}

const SnapSchema: Schema = new Schema({
  sender_id: { type: Schema.Types.ObjectId, ref: 'User', required: true },
  receiver_id: { type: Schema.Types.ObjectId, ref: 'User', required: true },
  media_url: { type: String, required: true },
  media_type: { type: String, enum: ['image', 'video'], required: true },
  duration: { type: Number, min: 1, max: 10, required: true }, // duration in seconds
  status: { type: String, enum: ['sent', 'delivered', 'viewed', 'deleted'], default: 'sent' },
  viewed_at: { type: Date, default: null },
  created_at: { type: Date, default: Date.now },
});

export const Snap = mongoose.model<ISnap>('Snap', SnapSchema);
