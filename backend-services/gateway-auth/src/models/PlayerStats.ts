import mongoose, { Document, Schema, Types } from 'mongoose';

export interface IPlayerStats extends Document {
    userId: Types.ObjectId;
    wins: number;
    kills: number;
    matchesPlayed: number;
    headshots: number;
    accuracyRate: number;
    currentRankPoints: number;
    seasonHighRank: number;
}

const PlayerStatsSchema = new Schema<IPlayerStats>(
    {
        userId: {
            type: Schema.Types.ObjectId,
            ref: 'User',
            required: true,
            unique: true,
            index: true,
        },
        wins: { type: Number, default: 0, min: 0 },
        kills: { type: Number, default: 0, min: 0 },
        matchesPlayed: { type: Number, default: 0, min: 0 },
        headshots: { type: Number, default: 0, min: 0 },
        accuracyRate: { type: Number, default: 0, min: 0, max: 100 },
        currentRankPoints: { type: Number, default: 1000, min: 0 },
        seasonHighRank: { type: Number, default: 1000, min: 0 },
    },
    { timestamps: true }
);

export const PlayerStats = mongoose.model<IPlayerStats>('PlayerStats', PlayerStatsSchema);
