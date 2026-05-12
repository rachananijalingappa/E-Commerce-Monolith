const productGrid = document.getElementById('productGrid');
const cartItemsContainer = document.getElementById('cartItems');
const cartCount = document.getElementById('cartCount');
const cartTotalEl = document.getElementById('cartTotal');
const checkoutBtn = document.getElementById('checkoutBtn');
const checkoutStatus = document.getElementById('checkoutStatus');

let cart = [];
let products = [];

const formatPrice = (price) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(price);

async function loadProducts() {
    try {
        const res = await fetch('/internal/catalog/products');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        products = await res.json();
        renderProducts();
    } catch (err) {
        console.error('Failed to load catalog:', err);
        productGrid.innerHTML = '<div class="status-error" style="opacity:1">Could not load products.</div>';
    }
}

function renderProducts() {
    productGrid.innerHTML = products.map(p => `
        <div class="product-card">
            <h3>${p.name}</h3>
            <div class="price">${formatPrice(p.price)}</div>
            <button class="add-to-cart" onclick="addToCart('${p.id}')">Add to Cart</button>
        </div>
    `).join('');
}

window.addToCart = (id) => {
    const product = products.find(p => p.id === id);
    if (!product) return;
    cart.push(product);
    renderCart();
    checkoutStatus.className = 'checkout-status';
    checkoutStatus.textContent = '';
};

function renderCart() {
    cartCount.textContent = cart.length;

    if (cart.length === 0) {
        cartItemsContainer.innerHTML = '<p class="empty-cart">Cart is empty.</p>';
        cartTotalEl.textContent = formatPrice(0);
        checkoutBtn.disabled = true;
        return;
    }

    checkoutBtn.disabled = false;
    cartItemsContainer.innerHTML = cart.map(item => `
        <div class="cart-item">
            <span class="cart-item-title">${item.name}</span>
            <span class="cart-item-price">${formatPrice(item.price)}</span>
        </div>
    `).join('');

    const total = cart.reduce((sum, item) => sum + item.price, 0);
    cartTotalEl.textContent = formatPrice(total);
}

checkoutBtn.addEventListener('click', async () => {
    if (cart.length === 0) return;
    checkoutBtn.disabled = true;
    checkoutBtn.innerHTML = '<span>Processing...</span>';
    checkoutStatus.className = 'checkout-status';

    try {
        const tokenRes = await fetch('/internal/auth/token');
        if (!tokenRes.ok) throw new Error('Authentication failed');
        const { token } = await tokenRes.json();

        const orderRes = await fetch('/internal/orders', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(cart.map(item => item.id))
        });

        if (!orderRes.ok) {
            const text = await orderRes.text();
            let msg = `Order failed (${orderRes.status})`;
            try { msg = JSON.parse(text).error || msg; } catch { /* non-JSON response */ }
            throw new Error(msg);
        }

        const { orderId } = await orderRes.json();
        cart = [];
        renderCart();

        checkoutStatus.textContent = `Order placed — ${orderId.substring(0, 8)}`;
        checkoutStatus.className = 'checkout-status status-success';
    } catch (err) {
        console.error('Checkout error:', err);
        checkoutStatus.textContent = err.message;
        checkoutStatus.className = 'checkout-status status-error';
        checkoutBtn.disabled = false;
    } finally {
        checkoutBtn.innerHTML = '<span>Secure Checkout</span><div class="btn-glow"></div>';
    }
});

loadProducts();
