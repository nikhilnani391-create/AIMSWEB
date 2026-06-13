import mongoose, { Document, Schema } from 'mongoose';

export interface IUser extends Document {
    username: string;
    emailHash: string;
    passwordHash: string;
    createdAt: Date;
    lastLogin: Date;
    refreshToken: string | null;
}

const UserSchema = new Schema<IUser>(
    {
        username: {
            type: String,
            unique: true,
            required: true,
            minlength: 3,
            maxlength: 24,
            trim: true,
            index: true,
        },
        emailHash: {
            type: String,
            unique: true,
            required: true,
        },
        passwordHash: {
            type: String,
            required: true,
        },
        lastLogin: {
            type: Date,
            default: Date.now,
        },
        refreshToken: {
            type: String,
            default: null,
        },
    },
    { timestamps: true }
);

export const User = mongoose.model<IUser>('User', UserSchema);
