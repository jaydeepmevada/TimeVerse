/**
 * TimeVerse Main JavaScript Application
 * Handles 3D card tilt, navbar glassmorphism scroll,
 * live cart/wishlist sync, toast notifications, and product detail gallery.
 */

// Global TimeVerse State (synchronized with server & localStorage)
const TimeVerseState = {
    cartCount: 0,
    wishlistCount: 0,

    init() {
        this.updateBadgeUI();
        this.syncServerCounts();
        this.initNavbarScroll();
        this.init3DCardTilt();
        this.initGallery();
        this.initPincodeChecker();
    },

    updateBadgeUI() {
        const cartEls = [document.getElementById('tvCartBadge'), document.getElementById('tvNavCartCount')];
        const wishEls = [document.getElementById('tvWishlistBadge'), document.getElementById('tvNavWishlistCount')];
        cartEls.forEach(el => { if (el) el.textContent = this.cartCount; });
        wishEls.forEach(el => { if (el) el.textContent = this.wishlistCount; });
    },

    async syncServerCounts() {
        try {
            const res = await fetch('/api/cart');
            if (res.ok) {
                const data = await res.json();
                if (data && typeof data.totalItemCount === 'number') {
                    this.cartCount = data.totalItemCount;
                    localStorage.setItem('tv_cart_count', this.cartCount);
                    this.updateBadgeUI();
                }
            }
        } catch (e) {
            // Graceful fallback to local count
        }

        try {
            const wishRes = await fetch('/Wishlist/Count');
            if (wishRes.ok) {
                const wishData = await wishRes.json();
                if (wishData && typeof wishData.count === 'number') {
                    this.wishlistCount = wishData.count;
                    localStorage.setItem('tv_wishlist_count', this.wishlistCount);
                    this.updateBadgeUI();
                }
            }
        } catch (e) {
            // Graceful fallback
        }
    },

    showToast(message, type = 'success') {
        let container = document.querySelector('.tv-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'tv-toast-container';
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = 'tv-toast';
        toast.innerHTML = `
            <span style="color: var(--tv-accent, #C9A227); font-size: 1.1rem;">✦</span>
            <span>${message}</span>
        `;
        container.appendChild(toast);

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(10px)';
            toast.style.transition = 'all 0.3s ease';
            setTimeout(() => toast.remove(), 300);
        }, 3200);
    },

    async addToCart(watchId, watchName, btnEl) {
        if (!watchName && typeof watchId === 'string') {
            watchName = watchId;
            watchId = null;
        }

        let serverUpdated = false;
        if (watchId) {
            try {
                const res = await fetch('/api/cart/items', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ watchId: parseInt(watchId), quantity: 1 })
                });
                if (res.ok) {
                    const data = await res.json();
                    if (data && typeof data.totalCount === 'number') {
                        this.cartCount = data.totalCount;
                        serverUpdated = true;
                    }
                }
            } catch (err) {
                console.warn('Cart API sync fallback:', err);
            }
        }

        if (!serverUpdated) {
            this.cartCount += 1;
        }
        localStorage.setItem('tv_cart_count', this.cartCount);
        this.updateBadgeUI();
        this.showToast(`Added <strong>${watchName || 'watch'}</strong> to your shopping cart.`);
    },

    async toggleWishlist(watchId, watchName, btnEl) {
        if (!watchName && typeof watchId === 'string') {
            watchName = watchId;
            watchId = null;
        }

        let added = true;
        let message = `Added <strong>${watchName || 'watch'}</strong> to your wishlist.`;

        if (watchId) {
            try {
                const res = await fetch('/Wishlist/Toggle?watchId=' + watchId, {
                    method: 'POST',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });
                if (res.ok) {
                    const data = await res.json();
                    if (data) {
                        added = data.added;
                        if (typeof data.count === 'number') {
                            this.wishlistCount = data.count;
                        } else {
                            this.wishlistCount = Math.max(0, this.wishlistCount + (added ? 1 : -1));
                        }
                        message = data.message || message;
                    }
                } else {
                    this.wishlistCount += 1;
                }
            } catch (err) {
                this.wishlistCount += 1;
            }
        } else {
            this.wishlistCount += 1;
        }

        localStorage.setItem('tv_wishlist_count', this.wishlistCount);
        this.updateBadgeUI();

        if (btnEl) {
            const icon = btnEl.querySelector('i');
            if (added) {
                btnEl.style.color = 'var(--tv-accent, #C9A227)';
                btnEl.style.borderColor = 'var(--tv-accent, #C9A227)';
                if (icon) {
                    icon.classList.remove('bi-heart');
                    icon.classList.add('bi-heart-fill');
                    icon.style.color = 'var(--tv-accent, #C9A227)';
                }
            } else {
                btnEl.style.color = '';
                btnEl.style.borderColor = '';
                if (icon) {
                    icon.classList.remove('bi-heart-fill');
                    icon.classList.add('bi-heart');
                    icon.style.color = '';
                }
            }
        }

        this.showToast(message);
    },

    initNavbarScroll() {
        const navbar = document.querySelector('.tv-navbar');
        if (navbar) {
            window.addEventListener('scroll', () => {
                if (window.scrollY > 40) {
                    navbar.classList.add('scrolled');
                } else {
                    navbar.classList.remove('scrolled');
                }
            });
        }
    },

    init3DCardTilt() {
        // Handled cleanly via CSS hover scaling and border highlights
    },

    initGallery() {
        const thumbBtns = document.querySelectorAll('.tv-thumb-btn');
        const mainImg = document.getElementById('tvMainWatchImg');

        thumbBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                thumbBtns.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                const targetSrc = btn.getAttribute('data-img-src');
                if (mainImg && targetSrc) {
                    mainImg.style.opacity = '0.3';
                    setTimeout(() => {
                        mainImg.src = targetSrc;
                        mainImg.style.opacity = '1';
                    }, 150);
                }
            });
        });
    },

    initPincodeChecker() {
        const btn = document.getElementById('tvBtnCheckPincode');
        const input = document.getElementById('tvPincodeInput');
        const resEl = document.getElementById('tvPincodeResult');

        if (btn && input && resEl) {
            btn.addEventListener('click', () => {
                const val = input.value.trim();
                if (/^\d{6}$/.test(val)) {
                    resEl.innerHTML = `
                        <div class="text-success fw-bold">
                            <i class="bi bi-check-circle-fill"></i> Delivery Available for Pincode ${val}
                        </div>
                        <div class="text-muted small">Estimated delivery within 2–3 business days via armored courier.</div>
                    `;
                } else {
                    resEl.innerHTML = `
                        <div class="text-danger fw-bold">
                            <i class="bi bi-exclamation-circle-fill"></i> Please enter a valid 6-digit Indian postal code.
                        </div>
                    `;
                }
            });
        }
    }
};

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    TimeVerseState.init();
});
