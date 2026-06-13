import mongoose, { Document, Schema, Types } from 'mongoose';

export interface IInventoryItem {
    itemId: string;
    itemType: 'skin' | 'character' | 'weapon_skin' | 'emote' | 'pet' | 'gloo_wall_skin';
    acquiredAt: Date;
}

export interface IInventory extends Document {
    userId: Types.ObjectId;
    items: IInventoryItem[];
    activeLoadout: {
        characterId: string | null;
        weaponSkins: Record<string, string>;
        glooWallSkin: string | null;
        petId: string | null;
    };
    maxSlots: number;
}

const InventoryItemSchema = new Schema<IInventoryItem>(
    {
        itemId: { type: String, required: true },
        itemType: {
            type: String,
            enum: ['skin', 'character', 'weapon_skin', 'emote', 'pet', 'gloo_wall_skin'],
            required: true,
        },
        acquiredAt: { type: Date, default: Date.now },
    },
    { _id: false }
);

const InventorySchema = new Schema<IInventory>(
    {
        userId: {
            type: Schema.Types.ObjectId,
            ref: 'User',
            required: true,
            unique: true,
            index: true,
        },
        items: {
            type: [InventoryItemSchema],
            default: [],
        },
        activeLoadout: {
            characterId: { type: String, default: null },
            weaponSkins: { type: Map, of: String, default: {} },
            glooWallSkin: { type: String, default: null },
            petId: { type: String, default: null },
        },
        maxSlots: { type: Number, default: 200 },
    },
    { timestamps: true }
);

export const Inventory = mongoose.model<IInventory>('Inventory', InventorySchema);
