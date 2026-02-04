import { StashCache } from './stashCache';

export interface StashCategory {
    id: string;
    title: string;
    description: string;
    notesCount: number;
}

export interface StashNote {
    id: string;
    title: string | null;
    content: string;
}

export interface CreateCategoryRequest {
    title: string;
    description?: string;
}

export interface UpdateCategoryRequest {
    title?: string;
    description?: string;
}

export interface CreateNoteRequest {
    content: string;
}

export interface UpdateNoteRequest {
    content: string;
}

export interface CategoryWithNotes {
    category: StashCategory;
    notes: StashNote[];
}

export interface ConflictError {
    error: string;
    conflictCode: string;
}

export interface BackEndResult<T> {
    theResult?: T;
    code: number;
    conflictCode: string;
    errorMessage: string;
    isOk: boolean;
}

const handleResponse = async <T>(response: Response): Promise<BackEndResult<T>> => {
    // Handle 401 Unauthorized - redirect to home to trigger OAuth
    if (response.status === 401) {
        window.location.href = '/';
        return {
            theResult: undefined,
            code: response.status,
            conflictCode: 'NoConflict',
            errorMessage: 'Authentication required',
            isOk: false
        };
    }

    if (response.status === 204) {
        return {
            theResult: undefined,
            code: response.status,
            conflictCode: 'NoConflict',
            errorMessage: '',
            isOk: true
        };
    }

    const data = await response.json();

    if (!response.ok) {
        return {
            theResult: undefined,
            code: response.status,
            conflictCode: data.conflictCode || 'NoConflict',
            errorMessage: data.error || data.message || 'Unknown error',
            isOk: false
        };
    }

    return {
        theResult: data,
        code: response.status,
        conflictCode: 'NoConflict',
        errorMessage: '',
        isOk: true
    };

};

/**
 * Search stash notes with excerpt highlighting for a pattern.
 * @param pattern The search pattern
 * @param searchInContent Whether to search in note content
 * @param selectedCategoryId Optional category ID to restrict search
 * @returns Array of notes with excerpt in header.description
 */
export async function searchStashNotesWithExcerpt(
    pattern: string,
    searchInContent: boolean,
    selectedCategoryId?: string
): Promise<any[]> {
    
    function getExcerpt2(body: string, pattern: string): string {
        const lines = body.split('\n');
        for (const line of lines) {
            if (line.toLowerCase().includes(pattern.toLowerCase())) {
                // Highlight
                const escapedPattern = pattern.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                const re = new RegExp(`(${escapedPattern})`, 'ig');
                let excerpt = line.replace(re, '<mark>$1</mark>');
                return excerpt.trim();
            }
        }
        return '';
    }

    let endpoint;
    if (selectedCategoryId) {
        endpoint = `/stash/search?pattern=${encodeURIComponent(pattern)}&searchInContent=${searchInContent}&categoryId=${selectedCategoryId}`;
    } else {
        endpoint = `/stash/search?pattern=${encodeURIComponent(pattern)}&searchInContent=${searchInContent}`;
    }
    try {
        const response = await fetch(endpoint, {
            headers: { 'X-Dendron-API-Call': 'true' }
        });
        if (response.ok) {
            const found = await response.json();
            return found.map((note: any) => {
                // If global search, expect note.header.categoryId and note.header.categoryTitle
                const hasToGetExcerpt = searchInContent && note.body;
                let header = note.header || {};
                if (hasToGetExcerpt) {
                    const excerpt = getExcerpt2(note.body, pattern);
                    if (excerpt) {
                        header = {
                            ...header,
                            description: excerpt || header.description
                        };
                    }
                }
                return {
                    ...note,
                    header
                };
            });
        }
    } catch (e) {
        console.error('[StashApi] Search failed:', e);
    }
    return [];
}

const handleError = <T>(error: any): BackEndResult<T> => {
    return {
        theResult: undefined,
        code: 500,
        conflictCode: 'NoConflict',
        errorMessage: error.message || 'Network error',
        isOk: false
    };
};

export const StashApi = {
    // Categories
    getCategoriesWithNotes: async (force: boolean = false): Promise<BackEndResult<CategoryWithNotes[]>> => {
        try {
            const url = force ? '/stash/categories/with-notes?force=true' : '/stash/categories/with-notes';
            const response = await fetch(url, {
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<CategoryWithNotes[]>(response);
            
            // Cache all categories and notes if successful
            if (result.isOk && result.theResult) {
                const categories: StashCategory[] = [];
                
                result.theResult.forEach(item => {
                    categories.push(item.category);
                    StashCache.setNotes(item.category.id, item.notes);
                });
                
                StashCache.setCategories(categories);
            }
            
            return result;
        } catch (error) {
            return handleError<CategoryWithNotes[]>(error);
        }
    },

    getCategories: async (): Promise<BackEndResult<StashCategory[]>> => {
        // Check cache first
        const cached = StashCache.getCategories();
        if (cached) {
            return {
                theResult: cached,
                code: 200,
                conflictCode: 'NoConflict',
                errorMessage: '',
                isOk: true
            };
        }
        
        // Cache miss - fetch from API
        try {
            const response = await fetch('/stash/categories', {
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<StashCategory[]>(response);
            
            // Cache the result if successful
            if (result.isOk && result.theResult) {
                StashCache.setCategories(result.theResult);
            }
            
            return result;
        } catch (error) {
            return handleError<StashCategory[]>(error);
        }
    },

    createCategory: async (data: CreateCategoryRequest): Promise<BackEndResult<StashCategory>> => {
        try {
            const response = await fetch('/stash/category', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Dendron-API-Call': 'true'
                },
                credentials: 'include',
                body: JSON.stringify(data)
            });
            const result = await handleResponse<StashCategory>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories();
            }
            
            return result;
        } catch (error) {
            return handleError<StashCategory>(error);
        }
    },

    updateCategory: async (categoryId: string, data: UpdateCategoryRequest): Promise<BackEndResult<StashCategory>> => {
        try {
            const response = await fetch(`/stash/category/${categoryId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Dendron-API-Call': 'true'
                },
                credentials: 'include',
                body: JSON.stringify(data)
            });
            const result = await handleResponse<StashCategory>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories();
            }
            
            return result;
        } catch (error) {
            return handleError<StashCategory>(error);
        }
    },

    deleteCategory: async (categoryId: string): Promise<BackEndResult<void>> => {
        try {
            const response = await fetch(`/stash/category/${categoryId}`, {
                method: 'DELETE',
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<void>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories();
                StashCache.clearNotes(categoryId);
            }
            
            return result;
        } catch (error) {
            return handleError<void>(error);
        }
    },

    reloadCategory: async (categoryId: string): Promise<BackEndResult<CategoryWithNotes>> => {
        try {
            const response = await fetch(`/stash/category/${categoryId}/reload`, {
                method: 'POST',
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<CategoryWithNotes>(response);
            
            // Update both caches with fresh data from backend
            if (result.isOk && result.theResult) {
                // Get current categories from cache or empty array
                const currentCategories = StashCache.getCategories() || [];
                
                // Update the reloaded category in the list
                const updatedCategories = currentCategories.map(cat => 
                    cat.id === categoryId ? result.theResult!.category : cat
                );
                
                // If category wasn't in list, add it
                if (!currentCategories.some(cat => cat.id === categoryId)) {
                    updatedCategories.push(result.theResult.category);
                }
                
                // Update both caches
                StashCache.setCategories(updatedCategories);
                StashCache.setNotes(categoryId, result.theResult.notes);
            }
            
            return result;
        } catch (error) {
            return handleError<CategoryWithNotes>(error);
        }
    },

    // Notes
    getNotes: async (categoryId: string): Promise<BackEndResult<StashNote[]>> => {
        // Check cache first
        const cached = StashCache.getNotes(categoryId);
        if (cached) {
            return {
                theResult: cached,
                code: 200,
                conflictCode: 'NoConflict',
                errorMessage: '',
                isOk: true
            };
        }
        
        // Cache miss - fetch from API
        try {
            const response = await fetch(`/stash/categories/${categoryId}`, {
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<StashNote[]>(response);
            
            // Cache the result if successful
            if (result.isOk && result.theResult) {
                StashCache.setNotes(categoryId, result.theResult);
            }
            
            return result;
        } catch (error) {
            return handleError<StashNote[]>(error);
        }
    },

    createNote: async (categoryId: string, content: string): Promise<BackEndResult<StashNote>> => {
        try {
            const response = await fetch(`/stash/categories/${categoryId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({ content })
            });
            const result = await handleResponse<StashNote>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories(); // Note count changed
                StashCache.clearNotes(categoryId);
            }
            
            return result;
        } catch (error) {
            return handleError<StashNote>(error);
        }
    },

    updateNote: async (categoryId: string, noteId: string, content: string): Promise<BackEndResult<StashNote>> => {
        try {
            const response = await fetch(`/stash/categories/${categoryId}/note/${noteId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Dendron-API-Call': 'true'
                },
                credentials: 'include',
                body: JSON.stringify({ content })
            });
            const result = await handleResponse<StashNote>(response);
            
            // Invalidate notes cache on success
            if (result.isOk) {
                StashCache.clearNotes(categoryId);
            }
            
            return result;
        } catch (error) {
            return handleError<StashNote>(error);
        }
    },

    deleteNote: async (categoryId: string, noteId: string): Promise<BackEndResult<void>> => {
        try {
            const response = await fetch(`/stash/categories/${categoryId}/note/${noteId}`, {
                method: 'DELETE',
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<void>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories(); // Note count changed
                StashCache.clearNotes(categoryId);
            }
            
            return result;
        } catch (error) {
            return handleError<void>(error);
        }
    }
};
