/**
 * TimeVerse REST API Client
 * Provides asynchronous, promise-based wrappers for interacting with TimeVerse RESTful endpoints.
 */
const TimeVerseApi = (function () {
    'use strict';

    // Internal helper for JSON fetch requests
    async function request(endpoint, options = {}) {
        const defaultHeaders = {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        };

        const config = {
            ...options,
            headers: {
                ...defaultHeaders,
                ...options.headers
            }
        };

        try {
            const response = await fetch(endpoint, config);
            const contentType = response.headers.get('content-type');
            const isJson = contentType && contentType.includes('application/json');
            const data = isJson ? await response.json() : await response.text();

            if (!response.ok) {
                const errorMessage = (data && data.message) || (data && data.title) || response.statusText || 'Network request failed';
                throw new Error(errorMessage);
            }

            return data;
        } catch (error) {
            console.error(`[TimeVerseApi] Error in request to ${endpoint}:`, error);
            throw error;
        }
    }

    // Update global navbar cart badge
    function updateCartBadge(count) {
        const badges = document.querySelectorAll('.cart-badge-count');
        badges.forEach(badge => {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'inline-block' : 'none';
        });
    }

    return {
        // Vault / Cart API
        cart: {
            async getCart() {
                return await request('/api/cart', { method: 'GET' });
            },

            async addItem(watchId, quantity = 1) {
                const result = await request('/api/cart/items', {
                    method: 'POST',
                    body: JSON.stringify({ watchId, quantity })
                });
                if (result && typeof result.totalItems === 'number') {
                    updateCartBadge(result.totalItems);
                }
                return result;
            },

            async updateQuantity(cartItemId, quantity) {
                const result = await request(`/api/cart/items/${cartItemId}`, {
                    method: 'PUT',
                    body: JSON.stringify({ cartItemId, quantity })
                });
                if (result && typeof result.totalItems === 'number') {
                    updateCartBadge(result.totalItems);
                }
                return result;
            },

            async removeItem(cartItemId) {
                const result = await request(`/api/cart/items/${cartItemId}`, {
                    method: 'DELETE'
                });
                if (result && typeof result.totalItems === 'number') {
                    updateCartBadge(result.totalItems);
                }
                return result;
            }
        },

        // Watches API
        watches: {
            async getWatches(filterParams = {}) {
                const query = new URLSearchParams(filterParams).toString();
                const url = query ? `/api/watches?${query}` : '/api/watches';
                return await request(url, { method: 'GET' });
            },

            async getWatchById(id) {
                return await request(`/api/watches/${id}`, { method: 'GET' });
            }
        },

        // Brands & Categories API
        taxonomies: {
            async getBrands() {
                return await request('/api/brands', { method: 'GET' });
            },

            async getCategories() {
                return await request('/api/categories', { method: 'GET' });
            }
        },

        // Orders API
        orders: {
            async track(orderNumber) {
                return await request(`/api/orders/track/${encodeURIComponent(orderNumber)}`, { method: 'GET' });
            }
        },

        // UI Helpers
        ui: {
            updateCartBadge
        }
    };
})();

// Export globally
window.TimeVerseApi = TimeVerseApi;
