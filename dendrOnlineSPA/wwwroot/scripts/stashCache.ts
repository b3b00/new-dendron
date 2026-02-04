import type { StashCategory, StashNote } from './stashApi';

interface CachedCategories {
    data: StashCategory[];
    timestamp: number;
    expiresAt: number;
}

interface CachedNotes {
    data: StashNote[];
    categoryId: string;
    timestamp: number;
    expiresAt: number;
}

const CACHE_TTL = 5 * 60 * 1000; // 5 minutes
const CATEGORIES_KEY = 'stash_categories';
const NOTES_KEY_PREFIX = 'stash_notes_';

export const StashCache = {
    // Categories
    getCategories(): StashCategory[] | null {
        try {
            const cached = localStorage.getItem(CATEGORIES_KEY);
            if (!cached) return null;
            
            const data: CachedCategories = JSON.parse(cached);
            
            // Check expiration
            if (Date.now() > data.expiresAt) {
                this.clearCategories();
                return null;
            }
            
            return data.data;
        } catch (error) {
            console.error('Error reading categories cache:', error);
            return null;
        }
    },
    
    setCategories(categories: StashCategory[]): void {
        try {
            const cached: CachedCategories = {
                data: categories,
                timestamp: Date.now(),
                expiresAt: Date.now() + CACHE_TTL
            };
            localStorage.setItem(CATEGORIES_KEY, JSON.stringify(cached));
        } catch (error) {
            console.error('Error setting categories cache:', error);
        }
    },
    
    clearCategories(): void {
        localStorage.removeItem(CATEGORIES_KEY);
    },
    
    // Notes
    getNotes(categoryId: string): StashNote[] | null {
        try {
            const key = `${NOTES_KEY_PREFIX}${categoryId}`;
            const cached = localStorage.getItem(key);
            if (!cached) return null;
            
            const data: CachedNotes = JSON.parse(cached);
            
            // Check expiration
            if (Date.now() > data.expiresAt) {
                this.clearNotes(categoryId);
                return null;
            }
            
            return data.data;
        } catch (error) {
            console.error('Error reading notes cache:', error);
            return null;
        }
    },
    
    setNotes(categoryId: string, notes: StashNote[]): void {
        try {
            const key = `${NOTES_KEY_PREFIX}${categoryId}`;
            const cached: CachedNotes = {
                data: notes,
                categoryId,
                timestamp: Date.now(),
                expiresAt: Date.now() + CACHE_TTL
            };
            localStorage.setItem(key, JSON.stringify(cached));
        } catch (error) {
            console.error('Error setting notes cache:', error);
        }
    },
    
    clearNotes(categoryId: string): void {
        const key = `${NOTES_KEY_PREFIX}${categoryId}`;
        localStorage.removeItem(key);
    },
    
    clearAllNotes(): void {
        // Remove all notes from localStorage
        const keysToRemove: string[] = [];
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key?.startsWith(NOTES_KEY_PREFIX)) {
                keysToRemove.push(key);
            }
        }
        keysToRemove.forEach(key => localStorage.removeItem(key));
    },
    
    clearAll(): void {
        this.clearCategories();
        this.clearAllNotes();
    }
};
