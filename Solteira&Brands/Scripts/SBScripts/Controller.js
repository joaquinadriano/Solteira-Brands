app.controller("SolteiraBrandsController", function ($scope, SolteiraBrandsService, $timeout, $interval) {

    console.log("Controller Loaded");

    $scope.viewMode = 'list';
    $scope.qty = 1;

    $scope.jewelries = [];
    $scope.defaultProductImageUrl = SolteiraBrandsService.defaultProductImageUrl;
    $scope.categoryFilter = null;
    $scope.filterDropdownOpen = false;
    $scope.jewelrySearch = '';
    $scope.searchOpen = false;

    $scope.setCategoryFilter = function (category) {
        $scope.categoryFilter = category;
        $scope.filterDropdownOpen = false;
    };

    $scope.filteredJewelries = function () {
        if (!$scope.jewelries || !$scope.jewelries.length) return [];
        var list = $scope.jewelries;
        if ($scope.categoryFilter) {
            var cat = ($scope.categoryFilter || '').toLowerCase();
            list = list.filter(function (j) { return (j.category || '').toLowerCase() === cat; });
        }
        var q = ($scope.jewelrySearch || '').toString().trim().toLowerCase();
        if (q) {
            list = list.filter(function (j) {
                var name = (j.name || '').toLowerCase();
                var category = (j.category || '').toLowerCase();
                var desc = (j.description || j.stones || j.material || '').toString().toLowerCase();
                return name.indexOf(q) >= 0 || category.indexOf(q) >= 0 || desc.indexOf(q) >= 0;
            });
        }
        return list;
    };

    $scope.loadStorefrontProducts = function () {
        SolteiraBrandsService.getStorefrontProducts().then(function (res) {
            if (res.data && res.data.success && angular.isArray(res.data.data)) {
                $scope.jewelries = res.data.data;
                $scope.buildFeaturedSlides();
                $scope.startFeaturedCarousel();
            } else {
                $scope.jewelries = [];
                $scope.featuredProductsSlides = [];
            }
        }, function () {
            $scope.jewelries = [];
            $scope.featuredProductsSlides = [];
        });
    };

    $scope.featuredProductsSlides = [];
    $scope.currentFeaturedSlide = 0;
    $scope.featuredCarouselInterval = null;
    var FEATURED_SLIDE_INTERVAL_MS = 10000;

    $scope.buildFeaturedSlides = function () {
        var featured = ($scope.jewelries || []).filter(function (j) { return j.isBestSeller === true || j.isBestSeller === 1; });
        var slides = [];
        for (var i = 0; i < featured.length; i += 4) {
            slides.push(featured.slice(i, i + 4));
        }
        $scope.featuredProductsSlides = slides.length ? slides : [];
        $scope.currentFeaturedSlide = 0;
    };

    $scope.nextFeaturedSlide = function () {
        if ($scope.featuredProductsSlides.length <= 1) return;
        $scope.currentFeaturedSlide = ($scope.currentFeaturedSlide + 1) % $scope.featuredProductsSlides.length;
    };
    $scope.prevFeaturedSlide = function () {
        if ($scope.featuredProductsSlides.length <= 1) return;
        $scope.currentFeaturedSlide = $scope.currentFeaturedSlide <= 0
            ? $scope.featuredProductsSlides.length - 1
            : $scope.currentFeaturedSlide - 1;
    };

    $scope.startFeaturedCarousel = function () {
        if ($scope.featuredCarouselInterval) $interval.cancel($scope.featuredCarouselInterval);
        if ($scope.featuredProductsSlides.length <= 1) return;
        $scope.featuredCarouselInterval = $interval(function () {
            $scope.nextFeaturedSlide();
        }, FEATURED_SLIDE_INTERVAL_MS);
    };
    $scope.stopFeaturedCarousel = function () {
        if ($scope.featuredCarouselInterval) {
            $interval.cancel($scope.featuredCarouselInterval);
            $scope.featuredCarouselInterval = null;
        }
    };

    $scope.isEnumerable = function (item) {
        return angular.isArray(item);
    };

    $scope.goBack = function () {
        $scope.transitionDir = 'backward';
        $scope.viewMode = 'list';
        $scope.selectedProduct = null;
    };

    $scope.selectProduct = function (item, event) {
        if (event) {
            event.stopPropagation();
        }

        const gemLoader = `<i class="bi bi-gem custom-gem-loader"></i>`;

        Swal.fire({
            title: 'REFINING...',
            html: 'Preparing your selection.',
            allowOutsideClick: false,
            showConfirmButton: false,
            background: '#FFFBE8',
            loaderHtml: gemLoader,
            timer: 1000,
            didOpen: () => {
                Swal.showLoading();
            }
        }).then(() => {
            $timeout(() => {
                $scope.selectedProduct = item;
                $scope.qty = 1;
                $scope.viewMode = 'detail';
                $scope.transitionDir = 'forward';
                window.scrollTo({ top: 0, behavior: 'smooth' });
                SolteiraBrandsService.getProductStock(item.id).then(function (r) {
                    if (r.data && r.data.success && $scope.selectedProduct && $scope.selectedProduct.id === item.id) {
                        $scope.selectedProduct.stockQuantity = r.data.stockQuantity;
                        $scope.capDetailQtyToStock();
                    }
                });
            });
        });
    };

    $scope.changeDetailQty = function (delta) {
        if (!$scope.selectedProduct) return;
        var max = parseInt($scope.selectedProduct.stockQuantity, 10) || 0;
        var current = parseInt($scope.qty, 10) || 1;
        var next = Math.max(1, Math.min(max, current + delta));
        $scope.qty = next;
    };

    $scope.capDetailQtyToStock = function () {
        if (!$scope.selectedProduct) return;
        var max = parseInt($scope.selectedProduct.stockQuantity, 10) || 0;
        var val = parseInt($scope.qty, 10);
        if (isNaN(val) || val < 1) $scope.qty = 1;
        else if (max > 0 && val > max) $scope.qty = max;
    };

    $scope.excludeSelected = function (item) {
        return item.id !== $scope.selectedProduct.id;
    };

    /** Related products: same category only (max 4). */
    $scope.getRelatedProducts = function () {
        if (!$scope.selectedProduct) return [];
        var cat = ($scope.selectedProduct.category || '').toString().trim();
        return $scope.jewelries.filter(function (j) {
            return j.id !== $scope.selectedProduct.id && (j.category || '').toString().trim() === cat;
        }).slice(0, 4);
    };

    /** Short description for product cards: max 2 lines / ~80 chars, ellipsis. */
    $scope.getShortDescription = function (item) {
        if (!item) return '';
        var raw = (item.description || item.stones || item.material || '').toString().trim();
        if (!raw) return '';
        if (raw.length <= 80) return raw;
        return raw.substring(0, 77) + '...';
    };

    $scope.currentMode = 'registration';

    $scope.init = function () {
        const urlParams = new URLSearchParams(window.location.search);

        // Load products for storefront jewelries page
        if (document.querySelector('.jewelry-grid-container')) {
            $scope.loadStorefrontProducts();
        }
        const productId = parseInt(urlParams.get('id'));

        if (productId) {
            const product = $scope.jewelries.find(j => j.id === productId);
            if (product) {
                $scope.selectedProduct = product;
                $scope.viewMode = 'detail';
            }
        }

        const mode = urlParams.get('mode');
        if (mode) {
            $scope.currentMode = mode;
        }
    };

    $scope.init();

    $scope.goToProduct = function (item) {
        window.location.href = '/SolteiraAndBrands/SolteiraBrandsJewelries?id=' + item.id;
    };

    $scope.showPassword = false;
    $scope.showVerifyPassword = false;

    $scope.loginData = {
        username: '',
        password: ''
    };

    $scope.loginErrors = { username: '', password: '' };

    $scope.registerData = {
        email: '',
        username: '',
        password: '',
        verifyPassword: ''
    };

    $scope.registerErrors = { email: '', username: '', password: '', verifyPassword: '' };

    function validateEmailFormat(email) {
        if (!email) return "Email is required.";
        var re = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
        if (!re.test(email)) return "Enter a valid email address.";
        return "";
    }

    function validateUsernameFormat(u) {
        if (!u) return "Username is required.";
        if (u.length < 5 || u.length > 20) return "Username must be 5–20 characters.";
        if (!/^[A-Za-z0-9_]+$/.test(u)) return "Use letters, numbers, and underscore only.";
        return "";
    }

    function validatePassword(p) {
        var msg = 'Minimum 8 characters. Must include uppercase, lowercase, number, and special character.';
        if (!p || p.length < 8) return msg;
        if (p.length > 64) return msg;
        if (!/[A-Z]/.test(p)) return msg;
        if (!/[a-z]/.test(p)) return msg;
        if (!/[0-9]/.test(p)) return msg;
        if (!/[^A-Za-z0-9]/.test(p)) return msg;
        return '';
    }

    $scope.onEmailBlur = function () {
        var e = $scope.registerData.email;
        var err = validateEmailFormat(e);
        $scope.registerErrors.email = err;
        if (err) return;

        SolteiraBrandsService.isEmailAvailable(e).then(function (r) {
            if (r.data && r.data.available === false) {
                $scope.registerErrors.email = "Email already registered.";
            }
        });
    };

    $scope.onUsernameBlur = function () {
        var u = $scope.registerData.username;
        var err = validateUsernameFormat(u);
        $scope.registerErrors.username = err;
        if (err) return;

        SolteiraBrandsService.isUsernameAvailable(u, false).then(function (r) {
            if (r.data && r.data.available === false) {
                $scope.registerErrors.username = "Username already taken.";
            }
        });
    };

    function clearRegisterErrors() {
        $scope.registerErrors = { email: '', username: '', password: '', verifyPassword: '' };
    }

    $scope.onPasswordInput = function () {
        var p = $scope.registerData.password || '';
        var v = $scope.registerData.verifyPassword || '';

        // Re-validate password strength on each input
        var pErr = validatePassword(p);
        $scope.registerErrors.password = pErr;

        // Re-validate match only when there is something typed
        if (v) {
            $scope.registerErrors.verifyPassword = (p && p === v) ? '' : 'Passwords do not match.';
        }
    };

    $scope.onVerifyPasswordInput = function () {
        var p = $scope.registerData.password || '';
        var v = $scope.registerData.verifyPassword || '';
        if (!v) {
            $scope.registerErrors.verifyPassword = '';
            return;
        }
        $scope.registerErrors.verifyPassword = (p && p === v) ? '' : 'Passwords do not match.';
    };

    $scope.register = function (form) {
        clearRegisterErrors();
        if (!form) return;

        var eErr = validateEmailFormat($scope.registerData.email);
        var uErr = validateUsernameFormat($scope.registerData.username);
        var p = $scope.registerData.password;
        var v = $scope.registerData.verifyPassword;
        var pErr = validatePassword(p);

        if (eErr || uErr || pErr || p !== v) {
            $scope.registerErrors.email = eErr;
            $scope.registerErrors.username = uErr;
            if (pErr) $scope.registerErrors.password = pErr;
            if (p !== v) $scope.registerErrors.verifyPassword = "Passwords do not match.";
            return;
        }

        var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        var token = tokenEl ? tokenEl.value : '';
        SolteiraBrandsService.register($scope.registerData, token).then(function (r) {
            if (r.data && r.data.success) {
                Swal.fire({ icon: 'success', title: 'Account created', text: r.data.message || 'You can now log in.' }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                });
            } else if (r.data && r.data.errors) {
                angular.extend($scope.registerErrors, r.data.errors);
            } else {
                Swal.fire({ icon: 'error', title: 'Registration failed', text: (r.data && r.data.message) || 'Please try again.' });
            }
        }, function (err) {
            var msg = (err.data && err.data.message) || (err.data && err.data.Message) || 'Registration failed. Please try again.';
            Swal.fire({ icon: 'error', title: 'Registration failed', text: msg });
        });
    };

    // Account Settings: async username validation on blur
    $scope.profileErrors = { username: '' };

    $scope.onLoginFieldChange = function (field) {
        if (field === 'username') {
            if ($scope.loginData.username) {
                $scope.loginErrors.username = '';
            }
        } else if (field === 'password') {
            if ($scope.loginData.password) {
                $scope.loginErrors.password = '';
            }
        }
    };

    $scope.login = function () {
        $scope.loginErrors = { username: '', password: '' };
        if (!$scope.loginData || !$scope.loginData.username) {
            $scope.loginErrors.username = 'Username is required.';
        }
        if (!$scope.loginData || !$scope.loginData.password) {
            $scope.loginErrors.password = 'Password is required.';
        }
        if ($scope.loginErrors.username || $scope.loginErrors.password) {
            return;
        }
        SolteiraBrandsService.login($scope.loginData.username, $scope.loginData.password).then(function (r) {
            if (r.data && r.data.success && r.data.redirectUrl) {
                window.location.href = r.data.redirectUrl;
            } else {
                $scope.loginErrors.username = 'Invalid username or password.';
                $scope.loginErrors.password = 'Invalid username or password.';
                Swal.fire({ icon: 'error', title: 'Login failed', text: (r.data && r.data.message) || 'Invalid username or password.' });
            }
        }, function (err) {
            var msg = (err.data && err.data.message) || (err.data && err.data.Message) || 'Invalid username or password.';
            $scope.loginErrors.username = 'Invalid username or password.';
            $scope.loginErrors.password = 'Invalid username or password.';
            Swal.fire({ icon: 'error', title: 'Login failed', text: msg });
        });
    };

    $scope.togglePassword = function () {
        $scope.showPassword = !$scope.showPassword;
    };

    $scope.toggleVerifyPassword = function () {
        $scope.showVerifyPassword = !$scope.showVerifyPassword;
    };

    $scope.timeLeft = 30;
    $scope.timerVisible = true;
    let timerInterval;

    $scope.startTimer = function () {
        $scope.timeLeft = 30;
        $scope.timerVisible = true;

        if (angular.isDefined(timerInterval)) {
            $interval.cancel(timerInterval);
        }

        timerInterval = $interval(function () {
            if ($scope.timeLeft > 0) {
                $scope.timeLeft--;
            } else {
                $scope.timerVisible = false;
                $interval.cancel(timerInterval);
            }
        }, 1000);
    };

    $scope.startTimer();

    $scope.resendOtp = function () {
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'success',
            title: 'New OTP sent to email!',
            showConfirmButton: false,
            timer: 2500,
            timerProgressBar: true
        });

        $scope.startTimer();
    };

    $scope.$on('$destroy', function () {
        if (angular.isDefined(timerInterval)) {
            $interval.cancel(timerInterval);
        }
        $scope.stopFeaturedCarousel();
    });

    $scope.sendOTP = function () {
        window.location.href = '/SolteiraAndBrands/SolteiraBrandsOTP?mode=forgot&email=' + $scope.userEmail;
    };

    $scope.handleOTPSuccess = function () {
        if ($scope.currentMode === 'forgot') {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsNewPassword';
        } else {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
        }
    };

    // Only BUYERs should have a persisted cart. Anonymous or non-buyer roles see an empty cart.
    // Keep a single array reference so ng-repeat and bindings stay in sync.
    var isBuyer = !!(window.sbIsBuyer);
    $scope.cart = [];
    try {
        if (isBuyer) {
            var raw = localStorage.getItem('sb_cart');
            if (raw) {
                var parsed = JSON.parse(raw);
                if (Array.isArray(parsed)) {
                    for (var i = 0; i < parsed.length; i++) $scope.cart.push(parsed[i]);
                }
            }
        }
    } catch (e) {
        $scope.cart = [];
    }

    // Re-load cart from localStorage (e.g. on Cart page) so list and badge always show current data.
    $scope.refreshCartFromStorage = function () {
        if (!isBuyer) return;
        try {
            var raw = localStorage.getItem('sb_cart');
            var next = [];
            if (raw) {
                var parsed = JSON.parse(raw);
                if (Array.isArray(parsed)) {
                    for (var j = 0; j < parsed.length; j++) next.push(parsed[j]);
                }
            }
            $scope.cart.length = 0;
            for (var k = 0; k < next.length; k++) $scope.cart.push(next[k]);
            syncCartCountToLayout();
        } catch (err) { }
    };

    $scope.getCartCount = function () {
        if (!$scope.cart || !$scope.cart.length) return 0;
        return $scope.cart.reduce(function (sum, item) { return sum + (item.qty || 0); }, 0);
    };

    function syncCartCountToLayout() {
        if (typeof window.dispatchEvent === 'function') {
            window.dispatchEvent(new CustomEvent('sbCartUpdated', { detail: $scope.getCartCount() }));
        }
    }

    $scope.addToCart = function (product, quantity) {
        if (!isBuyer) {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
            return;
        }
        var requestedQty = Math.max(1, parseInt(quantity, 10) || 1);
        SolteiraBrandsService.getProductStock(product.id).then(function (r) {
            if (!r.data || !r.data.success) {
                Swal.fire({ icon: 'error', title: 'Error', text: (r.data && r.data.message) || 'Could not verify stock.' });
                return;
            }
            var available = parseInt(r.data.stockQuantity, 10) || 0;
            if (available < 1) {
                Swal.fire({ icon: 'warning', title: 'Out of stock', text: product.name + ' is currently sold out.' });
                if ($scope.selectedProduct && $scope.selectedProduct.id === product.id) $scope.selectedProduct.stockQuantity = 0;
                return;
            }
            var existingItem = $scope.cart.find(function (i) { return i.id === product.id; });
            var alreadyInCart = existingItem ? (existingItem.qty || 0) : 0;
            var canAdd = Math.min(requestedQty, available - alreadyInCart);
            if (canAdd < 1) {
                Swal.fire({ icon: 'info', title: 'Maximum reached', text: 'You already have ' + alreadyInCart + ' of ' + product.name + ' in your cart. Available stock: ' + available + '.' });
                return;
            }
            var cartItem = {
                id: product.id,
                name: product.name,
                price: product.price,
                img: product.img,
                stones: product.stones,
                material: product.material,
                weight: product.weight,
                qty: canAdd,
                selected: true
            };
            if (existingItem) {
                existingItem.qty += canAdd;
            } else {
                $scope.cart.push(cartItem);
            }
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
            syncCartCountToLayout();
            var msg = product.name + ' is now in your bag.';
            if (canAdd < requestedQty) msg = 'Added ' + canAdd + ' (max available: ' + available + '). ' + product.name + ' is in your bag.';
            Swal.fire({ icon: 'success', title: 'Added to Cart', text: msg, timer: 1500, showConfirmButton: false });
        }, function () {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Could not verify stock. Please try again.' });
        });
    };

    $scope.removeFromCart = function (index) {
        $scope.cart.splice(index, 1);
        localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
        syncCartCountToLayout();
    };

    $scope.removeFromCartByItem = function (item) {
        var idx = $scope.cart.findIndex(function (i) { return i.id === item.id; });
        if (idx !== -1) {
            $scope.cart.splice(idx, 1);
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
            syncCartCountToLayout();
        }
    };

    $scope.updateCartQty = function (item, delta) {
        var current = parseInt(item.qty, 10) || 0;
        if (current + delta < 1) {
            $scope.removeFromCartByItem(item);
            return;
        }
        if (delta > 0) {
            SolteiraBrandsService.getProductStock(item.id).then(function (r) {
                if (!r.data || !r.data.success) return;
                var maxStock = parseInt(r.data.stockQuantity, 10) || 0;
                item.maxStock = maxStock;
                var newQty = Math.min(current + delta, maxStock);
                if (newQty < 1) $scope.removeFromCartByItem(item);
                else item.qty = newQty;
                localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
                syncCartCountToLayout();
            });
        } else {
            item.qty = current + delta;
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
            syncCartCountToLayout();
        }
    };

    $scope.onCartItemQtyChange = function (item) {
        var val = parseInt(item.qty, 10);
        if (isNaN(val) || val < 1) {
            item.qty = 1;
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
            syncCartCountToLayout();
            return;
        }
        SolteiraBrandsService.getProductStock(item.id).then(function (r) {
            if (!r.data || !r.data.success) return;
            var maxStock = parseInt(r.data.stockQuantity, 10) || 0;
            item.maxStock = maxStock;
            if (maxStock < 1) {
                $scope.removeFromCartByItem(item);
                Swal.fire({ icon: 'info', title: 'Item removed', text: item.name + ' is no longer available and was removed from your cart.' });
            } else if (val > maxStock) {
                item.qty = maxStock;
                Swal.fire({ icon: 'info', title: 'Quantity adjusted', text: item.name + ': max available is ' + maxStock + '. Quantity updated.' });
            }
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
            syncCartCountToLayout();
        });
    };

    $scope.validateCartAgainstStock = function () {
        if (!$scope.cart || !$scope.cart.length) return;
        var ids = $scope.cart.map(function (i) { return i.id; });
        if (!ids.length) return;
        SolteiraBrandsService.getProductStocks(ids).then(function (r) {
            if (!r.data || !r.data.success) return;
            var stocks = r.data.stocks;
            if (!stocks || !Array.isArray(stocks)) return;
            var byId = {};
            stocks.forEach(function (s) { byId[s.productId] = parseInt(s.stockQuantity, 10) || 0; });
            var messages = [];
            for (var i = $scope.cart.length - 1; i >= 0; i--) {
                var item = $scope.cart[i];
                var stock = byId[item.id] !== undefined ? byId[item.id] : -1;
                item.maxStock = stock >= 0 ? stock : (item.maxStock || 999);
                if (stock === 0) {
                    $scope.cart.splice(i, 1);
                    messages.push(item.name + ' is no longer available and was removed.');
                } else if (stock > 0 && (item.qty || 0) > stock) {
                    item.qty = stock;
                    messages.push(item.name + ': quantity adjusted to ' + stock + ' (max available).');
                }
            }
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
            syncCartCountToLayout();
            if (messages.length > 0) {
                Swal.fire({ icon: 'info', title: 'Cart updated', text: messages.join(' ') });
            }
        }, function () {
            // API error (e.g. after DB reset): keep cart as-is so user still sees items
        });
    };

    $scope.decrementOrRemoveFromCart = function (item, index) {
        if ((item.qty || 0) <= 1) {
            $scope.removeFromCart(index);
        } else {
            $scope.updateCartQty(item, -1);
        }
    };

    $scope.getCartTotal = function () {
        if (!$scope.cart) return 0;

        return $scope.cart.reduce(function (total, item) {
            if (item.selected) {
                return total + (item.price * item.qty);
            }
            return total;
        }, 0);
    };

    $scope.profile = { username: '', email: '', avatarUrl: '' };
    $scope.defaultAvatarUrl = SolteiraBrandsService.defaultAvatarUrl;
    $scope.pendingProfileImageFile = null;
    $scope.profilePreviewUrl = null;

    $scope.loadProfile = function () {
        SolteiraBrandsService.getMyProfile().then(function (r) {
            if (r.data && r.data.success) {
                $scope.profile.username = r.data.username || '';
                $scope.profile.email = r.data.email || '';
                $scope.profile.avatarUrl = r.data.avatarUrl || $scope.defaultAvatarUrl;
            }
        });
    };

    $scope.onProfileImageSelect = function (event) {
        var file = event.target.files[0];
        if (!file) return;
        var fn = (file.name || '').toLowerCase();
        if (fn.indexOf('.jpg') === -1 && fn.indexOf('.jpeg') === -1 && fn.indexOf('.png') === -1) {
            Swal.fire({ icon: 'warning', title: 'Invalid file', text: 'Only JPG, JPEG, or PNG allowed.' });
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            Swal.fire({ icon: 'warning', title: 'Too large', text: 'File must be under 5MB.' });
            return;
        }
        if ($scope.profilePreviewUrl) {
            URL.revokeObjectURL($scope.profilePreviewUrl);
        }
        $scope.pendingProfileImageFile = file;
        $scope.profilePreviewUrl = URL.createObjectURL(file);
        $timeout(function () {
            Swal.fire({ icon: 'info', title: 'Photo selected', text: 'Click Save Changes to save your new photo.' });
        }, 0);
    };

    $scope.onProfileUsernameBlur = function () {
        $scope.profileErrors.username = '';
        var u = ($scope.profile && $scope.profile.username) ? $scope.profile.username.trim() : '';
        var err = validateUsernameFormat(u);
        if (err) {
            $scope.profileErrors.username = err;
            return;
        }
        SolteiraBrandsService.isUsernameAvailable(u, true).then(function (r) {
            if (r.data && r.data.available === false) {
                $scope.profileErrors.username = "Username already taken.";
            }
        });
    };

    $scope.saveProfile = function () {
        if ($scope.profileErrors.username) {
            Swal.fire({ icon: 'warning', title: 'Fix errors', text: $scope.profileErrors.username });
            return;
        }
        var username = ($scope.profile && $scope.profile.username) ? $scope.profile.username.trim() : '';
        if (!username) {
            Swal.fire({ icon: 'warning', title: 'Required', text: 'Enter a name (username).' });
            return;
        }
        var doUpdate = function (avatarImageId) {
            SolteiraBrandsService.updateProfile(avatarImageId, username).then(function (r) {
                if (r.data && r.data.success) {
                    if ($scope.profilePreviewUrl) {
                        URL.revokeObjectURL($scope.profilePreviewUrl);
                        $scope.profilePreviewUrl = null;
                    }
                    $scope.pendingProfileImageFile = null;
                    var inputEl = document.getElementById('profileImageInput');
                    if (inputEl) inputEl.value = '';
                    var avatarUrl = $scope.profile.avatarUrl;
                    if (avatarUrl) {
                        var el = document.getElementById('layoutProfileImg');
                        var trigger = document.getElementById('layoutProfileImgTrigger');
                        if (el) el.src = avatarUrl;
                        if (trigger) trigger.src = avatarUrl;
                    }
                    Swal.fire({ icon: 'success', title: 'Saved', text: 'Your changes have been saved.', timer: 1500, showConfirmButton: false });
                } else {
                    Swal.fire({ icon: 'error', title: 'Failed', text: (r.data && r.data.message) || 'Try again.' });
                }
            }, function (err) {
                Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Try again.' });
            });
        };
        if ($scope.pendingProfileImageFile) {
            SolteiraBrandsService.uploadProfileImage($scope.pendingProfileImageFile).then(function (r) {
                if (r.data && r.data.success && r.data.imageID) {
                    var newUrl = (r.data.imagePath || '') + (r.data.imageFileName || '');
                    $scope.profile.avatarUrl = newUrl;
                    doUpdate(r.data.imageID);
                } else {
                    Swal.fire({ icon: 'error', title: 'Upload failed', text: (r.data && r.data.message) || 'Try again.' });
                }
            }, function () {
                Swal.fire({ icon: 'error', title: 'Upload failed', text: 'Try again.' });
            });
        } else {
            doUpdate(null);
        }
    };

    $scope.selectAll = false;
    $scope.toggleSelectAll = function (event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        $scope.selectAll = !$scope.selectAll;
        angular.forEach($scope.cart, function (item) {
            item.selected = $scope.selectAll;
        });
    };
    // Keep selectAll checkbox in sync when user ticks/unticks individual items
    $scope.$watch('cart', function () {
        if ($scope.cart && $scope.cart.length > 0) {
            var allSelected = $scope.cart.every(function (item) { return item.selected === true; });
            $scope.selectAll = allSelected;
        }
    }, true);

    $scope.proceedToCheckout = function (url) {
        var isBuyer = !!(window.sbIsBuyer);
        if (!isBuyer) {
            Swal.fire({ icon: 'info', title: 'Checkout', text: 'Only buyers can complete checkout.', confirmButtonColor: '#28a745' });
            return;
        }
        if (!url) url = '/SolteiraAndBrands/SolteiraBrandsCheckout';
        window.location.href = url;
    };

    $scope.getSelectedItems = function () {
        if (!$scope.cart) return [];
        return $scope.cart.filter(function (item) { return item.selected === true; });
    };

    $scope.getCheckoutSubtotal = function () {
        var selected = $scope.getSelectedItems();
        return selected.reduce(function (total, item) { return total + (item.price * item.qty); }, 0);
    };

    $scope.orders = [];
    $scope.loadMyOrders = function () {
        if (!isBuyer) return;
        SolteiraBrandsService.getMyOrders().then(function (r) {
            if (r.data && r.data.success && angular.isArray(r.data.data)) {
                $scope.orders = r.data.data;
            } else {
                $scope.orders = [];
            }
            $scope.checkOpenOrderDetailsFromQuery();
            $timeout(function () { $scope.initMyOrdersDataTable(); }, 150);
        }, function () {
            $scope.orders = [];
        });
    };
    $scope.myOrdersDataTableInstance = null;
    $scope.initMyOrdersDataTable = function () {
        var el = document.getElementById('myOrdersTable');
        if (!el || !$scope.orders || $scope.orders.length === 0) return;
        if (window.jQuery && window.jQuery.fn.DataTable) {
            if ($scope.myOrdersDataTableInstance) {
                try { $scope.myOrdersDataTableInstance.destroy(); } catch (e) { }
                $scope.myOrdersDataTableInstance = null;
            }
            $timeout(function () {
                var $t = window.jQuery('#myOrdersTable');
                if ($t.length && !$t.hasClass('dataTable')) {
                    $scope.myOrdersDataTableInstance = $t.DataTable({ pageLength: 10, order: [[1, 'desc']], responsive: true });
                }
            }, 50);
        }
    };
    $scope.viewOrderDetails = function (orderId) {
        if (orderId) {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsMyOrders?orderId=' + encodeURIComponent(orderId);
        }
    };

    $scope.goToPayment = function (orderId) {
        if (orderId) {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsPayment?orderId=' + encodeURIComponent(orderId);
        }
    };
    $scope.goToPaymentFromOrderModal = function (order) {
        if (!order || !order.orderId) return;
        var orderId = order.orderId;
        $scope.closeOrderDetailsModal();
        $timeout(function () {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsPayment?orderId=' + encodeURIComponent(orderId);
        }, 100);
    };

    /** Unified overall status for buyer display. Never show raw DB values. Layaway: show Layaway Active when RemainingBalance > 0 and at least one verified payment. */
    $scope.getOverallStatus = function (order) {
        if (!order) return '-';
        var status = (order.status || '').trim();
        var statusLower = status.toLowerCase();
        var deliveryStatus = (order.deliveryStatus || '').toLowerCase();
        var paymentType = (order.paymentType || '').toUpperCase();
        var remaining = order.remainingBalance != null ? Number(order.remainingBalance) : null;
        var total = order.total != null ? Number(order.total) : null;
        var verifiedCount = order.verifiedPaymentsCount != null ? Number(order.verifiedPaymentsCount) : 0;
        if (statusLower === 'cancelled') return 'Cancelled';
        if (statusLower === 'completed') return 'Completed';
        if (paymentType === 'LAYAWAY' && remaining != null && remaining > 0 && verifiedCount >= 1) return 'Layaway Active';
        if (paymentType === 'LAYAWAY' && remaining != null && total != null && remaining > 0 && remaining < total) return 'Layaway Active';
        if (statusLower === 'layaway active') return 'Layaway Active';
        if (statusLower === 'awaiting payment verification') return (order.hasSubmittedPayment ? 'Under Review' : 'Awaiting Payment');
        if (statusLower === 'paid') return 'Paid';
        if (statusLower === 'ready for meetup') return 'Ready for Meetup';
        if (statusLower === 'in delivery') return deliveryStatus === 'delivered' ? 'Completed' : 'In Delivery';
        if (statusLower === 'pending') return 'Pending Approval';
        if (statusLower === 'approved' && paymentType === 'LAYAWAY') return 'Awaiting Payment';
        if (statusLower === 'approved') return 'Awaiting Payment';
        if (statusLower === 'rejected') return 'Rejected';
        return status || '-';
    };

    $scope.getOrderActionLabel = function (order) {
        if (!order) return 'View Details';
        var status = (order.status || '').toLowerCase();
        var deliveryStatus = (order.deliveryStatus || '').toLowerCase();
        var paymentType = (order.paymentType || '').toUpperCase();
        var remaining = order.remainingBalance != null ? Number(order.remainingBalance) : 0;
        var isLayawayActive = (paymentType === 'LAYAWAY' && remaining > 0 && (status === 'layaway active' || (order.verifiedPaymentsCount >= 1 && (status === 'approved' || status === 'awaiting payment verification'))));
        if (status === 'rejected' || status === 'cancelled') return 'View Details';
        if (isLayawayActive) {
            if (order.hasSubmittedPayment) return 'View Details';
            return 'Pay Installment';
        }
        if (status === 'approved') {
            var latest = order.latestPaymentForProof;
            if (latest && latest.paymentStatus === 'REJECTED' && latest.allowResubmit) return 'Resubmit';
            return 'Pay Now';
        }
        if (status === 'layaway active' && order.hasSubmittedPayment) return 'View Details';
        if (status === 'layaway active') return 'Pay Installment';
        if (status === 'awaiting payment verification') return 'View Details';
        if (status === 'paid' || status === 'ready for meetup') return 'View Details';
        if (status === 'in delivery') return 'View Details';
        if (status === 'completed') return 'View Details';
        if (status === 'pending' || status.indexOf('pending') >= 0) return 'View Details';
        if (deliveryStatus === 'in_transit' || deliveryStatus === 'delivered') return 'View Details';
        return 'View Details';
    };

    $scope.goToTracking = function (trackingNumber) {
        if (trackingNumber && (trackingNumber + '').trim()) {
            window.open('https://www.lbcexpress.com/track/?trackingNo=' + encodeURIComponent((trackingNumber + '').trim()), '_blank');
        } else {
            Swal.fire({ icon: 'info', title: 'Tracking', text: 'Tracking number is not yet available. It will appear when the order is in transit.' });
        }
    };

    $scope.selectedOrderForDetails = null;
    $scope.openOrderDetailsModal = function (order) {
        if (!order) return;
        $scope.selectedOrderForDetails = order;
        var el = document.getElementById('buyerOrderDetailsModal');
        if (el && window.bootstrap) {
            var m = new bootstrap.Modal(el);
            m.show();
        }
    };
    $scope.closeOrderDetailsModal = function () {
        var el = document.getElementById('buyerOrderDetailsModal');
        if (el && window.bootstrap) {
            var m = bootstrap.Modal.getInstance(el);
            if (m) m.hide();
        }
        $scope.selectedOrderForDetails = null;
    };

    $scope.selectedOrderForProof = null;
    $scope.openViewPaymentProofModalFromOrders = function (order) {
        if (!order || !order.latestPaymentForProof) { $scope.openOrderDetailsModal(order); return; }
        $scope.selectedOrderForProof = order;
        var el = document.getElementById('viewPaymentProofModalMyOrders');
        if (el && window.bootstrap) {
            var m = new bootstrap.Modal(el);
            m.show();
        }
    };
    $scope.closeViewPaymentProofModal = function () {
        var el = document.getElementById('viewPaymentProofModalMyOrders');
        if (el && window.bootstrap) {
            var m = bootstrap.Modal.getInstance(el);
            if (m) m.hide();
        }
        $scope.selectedOrderForProof = null;
    };
    $scope.handleOrderAction = function (order) {
        if (!order) return;
        var label = $scope.getOrderActionLabel(order);
        if (label === 'Pay Now' || label === 'Pay Installment' || label === 'Resubmit') $scope.goToPayment(order.orderId);
        else if (label === 'Track Now') $scope.goToTracking(order.trackingNumber);
        else if (label === 'Under Review') $scope.openViewPaymentProofModalFromOrders(order);
        else $scope.openOrderDetailsModal(order);
    };

    $scope.checkOpenOrderDetailsFromQuery = function () {
        var params = new URLSearchParams(window.location.search);
        var orderIdParam = params.get('orderId');
        if (!orderIdParam || !$scope.orders || !$scope.orders.length) return;
        var orderId = parseInt(orderIdParam, 10);
        var order = $scope.orders.find(function (o) { return o.orderId === orderId; });
        if (order) $scope.openOrderDetailsModal(order);
    };

    $scope.paymentOrder = null;
    $scope.paymentLoadError = null;
    $scope.proofFileName = '';
    $scope.paymentError = '';
    $scope.paymentValidation = { reference: '', proof: '', file: '' };
    $scope.submitting = false;
    $scope.paymentSettings = { gcash: null, bankTransfer: null };
    $scope.meetupSettings = { locationName: '', address: '' };
    $scope.paymentMode = 'Full Payment';
    $scope.paymentFormData = { channel: 'GCash', meetupDate: null, meetupTime: null };
    $scope.paymentForm = { referenceNumber: '', proofImageId: null };
    // Business hours 10:00 AM – 5:00 PM (centralized)
    var MEETUP_TIME_SLOTS = ['10:00 AM', '10:30 AM', '11:00 AM', '11:30 AM', '12:00 PM', '12:30 PM', '1:00 PM', '1:30 PM', '2:00 PM', '2:30 PM', '3:00 PM', '3:30 PM', '4:00 PM', '4:30 PM', '5:00 PM'];
    $scope.meetupTimeOptions = [];
    $scope.meetupNoSlotsForToday = false;

    function parseTimeToMinutes(timeStr) {
        if (!timeStr) return -1;
        var m = timeStr.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
        if (!m) return -1;
        var h = parseInt(m[1], 10);
        var min = parseInt(m[2], 10);
        if (m[3].toUpperCase() === 'PM' && h !== 12) h += 12;
        if (m[3].toUpperCase() === 'AM' && h === 12) h = 0;
        return h * 60 + min;
    }

    function updateMeetupTimeOptions() {
        var selected = $scope.paymentFormData && $scope.paymentFormData.meetupDate;
        if (!selected) {
            $scope.meetupTimeOptions = MEETUP_TIME_SLOTS.slice();
            $scope.meetupNoSlotsForToday = false;
            return;
        }
        var now = new Date();
        var todayStr = now.toISOString().split('T')[0];
        var currentMinutes = now.getHours() * 60 + now.getMinutes();
        var afterFivePM = currentMinutes >= 17 * 60;
        if (selected === todayStr) {
            if (afterFivePM) {
                $scope.meetupTimeOptions = [];
                $scope.meetupNoSlotsForToday = true;
                $scope.paymentFormData.meetupTime = null;
                Swal.fire({ icon: 'warning', title: 'No meetup time for today', text: 'No available meetup time for today. Please select another date.' });
                return;
            }
            var filtered = MEETUP_TIME_SLOTS.filter(function (t) {
                var slotMin = parseTimeToMinutes(t);
                return slotMin > currentMinutes && slotMin >= 10 * 60 && slotMin <= 17 * 60;
            });
            $scope.meetupTimeOptions = filtered;
            $scope.meetupNoSlotsForToday = filtered.length === 0;
            if (filtered.length === 0) {
                $scope.paymentFormData.meetupTime = null;
                Swal.fire({ icon: 'warning', title: 'No meetup time for today', text: 'No available meetup time for today. Please select another date.' });
            } else if ($scope.paymentFormData.meetupTime && filtered.indexOf($scope.paymentFormData.meetupTime) === -1) {
                $scope.paymentFormData.meetupTime = null;
            }
        } else {
            $scope.meetupTimeOptions = MEETUP_TIME_SLOTS.slice();
            $scope.meetupNoSlotsForToday = false;
        }
    }

    function getMinMeetupDate() {
        var now = new Date();
        var yyyy = now.getFullYear();
        var mm = String(now.getMonth() + 1).padStart(2, '0');
        var dd = String(now.getDate()).padStart(2, '0');
        var todayStr = yyyy + '-' + mm + '-' + dd;
        if (now.getHours() >= 17) {
            var tomorrow = new Date(now);
            tomorrow.setDate(tomorrow.getDate() + 1);
            var y2 = tomorrow.getFullYear();
            var m2 = String(tomorrow.getMonth() + 1).padStart(2, '0');
            var d2 = String(tomorrow.getDate()).padStart(2, '0');
            return y2 + '-' + m2 + '-' + d2;
        }
        return todayStr;
    }

    $scope.getMinMeetupDate = getMinMeetupDate;
    $scope.minMeetupDate = getMinMeetupDate();

    // Layaway paid percentage for Payment page (used for progress bar).
    $scope.getLayawayPaidPercent = function () {
        if (!$scope.paymentOrder || $scope.paymentOrder.paymentType !== 'LAYAWAY') return 0;
        var total = $scope.paymentOrder.totalAmount;
        if (!total || total <= 0) return 0;
        var remaining = ($scope.paymentOrder.remainingBalance != null ? $scope.paymentOrder.remainingBalance : 0);
        var paid = total - remaining;
        if (paid <= 0) return 0;
        var pct = (paid / total) * 100;
        return Math.round(pct * 100) / 100;
    };

    /** Ensure backend always receives yyyy-MM-dd (HTML date input can be string or Date, locale display is MM/dd/yyyy). */
    function toMeetupDateYYYYMMDD(val) {
        if (val == null || val === '') return null;
        if (typeof val === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(val)) return val;
        var d = new Date(val);
        if (isNaN(d.getTime())) return null;
        var y = d.getFullYear();
        var m = String(d.getMonth() + 1).padStart(2, '0');
        var day = String(d.getDate()).padStart(2, '0');
        return y + '-' + m + '-' + day;
    }

    $scope.calculateDownpayment = function (total, pct) {
        pct = pct != null ? pct : 30;
        var down = total * (pct / 100);
        var remaining = total - down;
        return { downpayment: down, remaining: remaining };
    };

    $scope.getMeetupDownpaymentAmount = function () {
        if (!$scope.paymentOrder || $scope.paymentOrder.totalAmount == null) return 0;
        var pct = $scope.paymentOrder.downpaymentPercentage != null ? $scope.paymentOrder.downpaymentPercentage : 30;
        var d = $scope.calculateDownpayment($scope.paymentOrder.totalAmount, pct);
        return Math.round(d.downpayment * 100) / 100;
    };

    $scope.onMeetupDateChange = function () {
        updateMeetupTimeOptions();
    };

    $scope.isMeetupChannel = function () {
        var c = (($scope.paymentFormData && $scope.paymentFormData.channel) || '').toString().toUpperCase();
        return c.indexOf('MEETUP') >= 0;
    };

    $scope.initPaymentPage = function () {
        var params = new URLSearchParams(window.location.search);
        var orderId = parseInt(params.get('orderId'), 10);
        if (!orderId) {
            $scope.paymentLoadError = 'No order specified.';
            return;
        }
        SolteiraBrandsService.getPaymentSettings().then(function (r) {
            if (r.data) {
                $scope.paymentSettings = { gcash: r.data.gcash || null, bankTransfer: r.data.bankTransfer || null };
            }
        });
        SolteiraBrandsService.getMeetupSettings().then(function (r) {
            if (r.data && (r.data.locationName || r.data.address)) {
                $scope.meetupSettings = { locationName: r.data.locationName || '', address: r.data.address || '' };
            } else {
                $scope.meetupSettings = { locationName: '', address: '' };
            }
        }).catch(function () {
            $scope.meetupSettings = { locationName: '', address: '' };
        });
        SolteiraBrandsService.getOrderForPayment(orderId).then(function (r) {
            if (r.data && r.data.success && r.data.data) {
                $scope.paymentOrder = r.data.data;
                $scope.paymentMode = ($scope.paymentOrder.paymentType === 'LAYAWAY') ? 'Layaway' : 'Full Payment';
                if ($scope.paymentOrder.paymentType === 'LAYAWAY') {
                    if ($scope.paymentFormData.channel === 'Cash through Meetup' || !$scope.paymentFormData.channel) {
                        $scope.paymentFormData.channel = 'GCash';
                    }
                }
                $scope.paymentLoadError = null;
                $scope.minMeetupDate = getMinMeetupDate();
                updateMeetupTimeOptions();
                var os = $scope.paymentOrder.order_status != null ? $scope.paymentOrder.order_status : $scope.paymentOrder.orderStatus;
                console.log('[PAYMENT DEBUG] Order loaded:', { order_status: os, order_status_Approved: os === 'Approved', hasExistingPayment: $scope.paymentOrder.hasExistingPayment, orderId: $scope.paymentOrder.orderId });
            } else {
                $scope.paymentOrder = null;
                $scope.paymentLoadError = (r.data && r.data.message) || 'Could not load order.';
            }
        }, function (err) {
            $scope.paymentOrder = null;
            $scope.paymentLoadError = (err.data && err.data.message) || 'Could not load order.';
        });
    };

    $scope.reloadPaymentOrder = function () {
        if (!$scope.paymentOrder || !$scope.paymentOrder.orderId) return;
        $scope.paymentLoadError = null;
        SolteiraBrandsService.getOrderForPayment($scope.paymentOrder.orderId).then(function (r) {
            if (r.data && r.data.success && r.data.data) {
                $scope.paymentOrder = r.data.data;
                $scope.paymentMode = ($scope.paymentOrder.paymentType === 'LAYAWAY') ? 'Layaway' : 'Full Payment';
                if ($scope.paymentOrder.paymentType === 'LAYAWAY' && ($scope.paymentFormData.channel === 'Cash through Meetup' || !$scope.paymentFormData.channel)) {
                    $scope.paymentFormData.channel = 'GCash';
                }
            }
        });
    };

    $scope.triggerProofUpload = function () {
        document.getElementById('proofUpload').click();
    };

    $scope.onProofFileSelected = function (file) {
        if (!file) return;
        $scope.paymentError = '';
        $scope.paymentValidation.proof = '';
        $scope.paymentValidation.file = '';
        $scope.proofFileName = file.name || 'Selected file';
        $scope.paymentForm.proofImageId = null;
        var fn = (file.name || '').toLowerCase();
        if (fn.indexOf('.jpg') === -1 && fn.indexOf('.jpeg') === -1 && fn.indexOf('.png') === -1) {
            $scope.paymentValidation.file = 'Only JPG or PNG allowed. PDF is not accepted.';
            Swal.fire({ icon: 'warning', title: 'Invalid file', text: 'Only JPG or PNG allowed. PDF is not accepted.' });
            $scope.proofFileName = '';
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            $scope.paymentValidation.file = 'File must be under 5MB.';
            Swal.fire({ icon: 'warning', title: 'File too large', text: 'File must be under 5MB. Please choose a smaller image.' });
            $scope.proofFileName = '';
            return;
        }
        SolteiraBrandsService.uploadPaymentProof(file).then(function (r) {
            var rawId = (r.data && (r.data.imageID !== undefined ? r.data.imageID : r.data.imageId));
            var id = rawId != null && rawId !== '' ? (typeof rawId === 'number' ? rawId : parseInt(rawId, 10)) : null;
            if (r.data && r.data.success && id > 0) {
                $timeout(function () {
                    $scope.paymentForm.proofImageId = id;
                    $scope.proofFileName = file.name || 'Uploaded';
                    $scope.paymentError = '';
                    $scope.paymentValidation.proof = '';
                    $scope.paymentValidation.file = '';
                }, 0);
                Swal.fire({ icon: 'success', title: 'Uploaded', text: 'Proof uploaded. Enter reference number (max 20 chars) and click PAY.', timer: 2500, showConfirmButton: false });
            } else {
                var msg = (r.data && r.data.message) || 'Upload failed.';
                $scope.paymentError = msg;
                $scope.paymentValidation.file = msg;
                Swal.fire({ icon: 'error', title: 'Upload failed', text: msg });
            }
        }, function (err) {
            var msg = (err.data && err.data.message) || 'Upload failed. Please try again.';
            $scope.paymentError = msg;
            $scope.paymentValidation.file = msg;
            Swal.fire({ icon: 'error', title: 'Upload failed', text: msg });
        });
    };

    $scope.canShowPayForm = function () {
        if (!$scope.paymentOrder) return false;
        var status = ($scope.paymentOrder.order_status != null ? $scope.paymentOrder.order_status : $scope.paymentOrder.orderStatus) || '';
        var statusU = (status || '').toUpperCase();
        var paymentType = ($scope.paymentOrder.paymentType || '').toUpperCase();
        var remaining = $scope.paymentOrder.remainingBalance != null ? Number($scope.paymentOrder.remainingBalance) : 0;
        var latest = $scope.paymentOrder.latestPaymentForProof;
        if (statusU === 'CANCELLED') return false;
        if (remaining <= 0) return false;
        if (latest && (latest.paymentStatus || '').toUpperCase() === 'SUBMITTED') return false;
        if (statusU === 'AWAITING MEETUP') return false;
        if (statusU === 'PAID' || statusU === 'READY FOR MEETUP') return false;
        if (paymentType === 'LAYAWAY' && statusU === 'LAYAWAY ACTIVE' && remaining > 0) return true;
        if (paymentType === 'LAYAWAY' && (statusU === 'APPROVED' || statusU === 'AWAITING PAYMENT VERIFICATION') && remaining > 0) return true;
        if (statusU === 'AWAITING PAYMENT VERIFICATION') return false;
        if (statusU === 'LAYAWAY ACTIVE') return remaining > 0;
        if (statusU !== 'APPROVED') return false;
        if ($scope.paymentOrder.hasExistingPayment) return false;
        if (latest && (latest.paymentStatus || '').toUpperCase() === 'REJECTED' && !latest.allowResubmit) return false;
        return true;
    };

    $scope.openViewPaymentProofModal = function () {
        if (!$scope.paymentOrder || !$scope.paymentOrder.latestPaymentForProof) return;
        var el = document.getElementById('viewPaymentProofModal');
        if (el && window.bootstrap) {
            var m = new bootstrap.Modal(el);
            m.show();
        }
    };

    $scope.hasValidProof = function () {
        var p = $scope.paymentForm && $scope.paymentForm.proofImageId;
        if (p == null || p === '') return false;
        var n = typeof p === 'number' ? p : parseInt(p, 10);
        return !isNaN(n) && n >= 1;
    };

    function logPaymentButtonState(reason) {
        var order = $scope.paymentOrder;
        var orderStatus = order ? (order.order_status != null ? order.order_status : order.orderStatus) : null;
        var ref = (($scope.paymentForm && $scope.paymentForm.referenceNumber) || '').trim();
        var payload = {
            reason: reason,
            selectedPaymentMethod: $scope.paymentMode,
            selectedChannel: ($scope.paymentFormData && $scope.paymentFormData.channel),
            referenceNumber: ref,
            referenceLength: ref.length,
            proofFile: $scope.paymentForm && $scope.paymentForm.proofImageId,
            proofFileName: $scope.proofFileName,
            order_order_status: orderStatus,
            order_status_Approved: orderStatus ? String(orderStatus).toUpperCase() === 'APPROVED' : false,
            hasExistingPayment: order ? !!order.hasExistingPayment : null,
            submitting: $scope.submitting
        };
        console.log('[PAYMENT DEBUG]', payload);
        return payload;
    }

    $scope.isPayButtonEnabled = function () {
        if ($scope.submitting) return false;
        if (!$scope.paymentMode || !($scope.paymentFormData && $scope.paymentFormData.channel)) return false;
        if (!$scope.hasValidProof()) return false;
        var ref = (($scope.paymentForm && $scope.paymentForm.referenceNumber) || '').trim();
        if (ref.length < 1 || ref.length > 20) return false;
        return true;
    };

    $scope.isScheduleButtonEnabled = function () {
        if ($scope.submitting) return false;
        if (!$scope.isMeetupChannel()) return false;
        var data = $scope.paymentFormData;
        if (!data || !data.meetupDate || !data.meetupTime) return false;
        if (!$scope.hasValidProof()) return false;
        var ref = (($scope.paymentForm && $scope.paymentForm.referenceNumber) || '').trim();
        if (ref.length < 1 || ref.length > 20) return false;
        return true;
    };
    $scope.clearProofSelection = function () {
        $scope.paymentError = '';
        $scope.proofFileName = '';
        $scope.paymentForm.proofImageId = null;
    };

    $scope.submitPayment = function () {
        if (!$scope.paymentOrder) return;
        var isMeetup = $scope.isMeetupChannel();
        $scope.paymentError = '';
        $scope.paymentValidation.reference = '';
        $scope.paymentValidation.proof = '';
        if (!isMeetup) {
            if (!$scope.hasValidProof()) {
                $scope.paymentValidation.proof = 'Please upload proof of payment.';
                $scope.paymentError = $scope.paymentValidation.proof;
                Swal.fire({ icon: 'warning', title: 'Proof required', text: 'Please upload proof of payment first.' });
                return;
            }
            var ref = (($scope.paymentForm && $scope.paymentForm.referenceNumber) || '').trim();
            if (ref.length < 1) {
                $scope.paymentValidation.reference = 'Reference number is required.';
                $scope.paymentError = $scope.paymentValidation.reference;
                Swal.fire({ icon: 'warning', title: 'Reference number required', text: 'Enter your payment reference number (max 20 characters).' });
                return;
            }
            if (ref.length > 20) {
                $scope.paymentValidation.reference = 'Reference number must be 20 characters or less.';
                $scope.paymentError = $scope.paymentValidation.reference;
                Swal.fire({ icon: 'warning', title: 'Too long', text: 'Reference number must be 20 characters or less.' });
                return;
            }
        } else {
            var mDate = $scope.paymentFormData && $scope.paymentFormData.meetupDate;
            var mTime = $scope.paymentFormData && $scope.paymentFormData.meetupTime;
            if (!mDate || !mTime) {
                $scope.paymentError = 'Please select meetup date and time.';
                Swal.fire({ icon: 'warning', title: 'Required', text: 'Please select meetup date and time.' });
                return;
            }
            if (!$scope.hasValidProof()) {
                $scope.paymentValidation.proof = 'Please upload proof of 30% down payment.';
                $scope.paymentError = $scope.paymentValidation.proof;
                Swal.fire({ icon: 'warning', title: 'Proof required', text: 'Please upload proof of your 30% down payment first.' });
                return;
            }
            var refMeetup = (($scope.paymentForm && $scope.paymentForm.referenceNumber) || '').trim();
            if (refMeetup.length < 1) {
                $scope.paymentValidation.reference = 'Reference number for 30% down payment is required.';
                $scope.paymentError = $scope.paymentValidation.reference;
                Swal.fire({ icon: 'warning', title: 'Reference required', text: 'Enter your payment reference number (max 20 characters).' });
                return;
            }
            if (refMeetup.length > 20) {
                $scope.paymentValidation.reference = 'Reference number must be 20 characters or less.';
                $scope.paymentError = $scope.paymentValidation.reference;
                Swal.fire({ icon: 'warning', title: 'Too long', text: 'Reference number must be 20 characters or less.' });
                return;
            }
        }
        $scope.submitting = true;
        var proofId = ($scope.paymentForm && $scope.paymentForm.proofImageId) || null;
        var refNum = (($scope.paymentForm && $scope.paymentForm.referenceNumber) || '').trim();
        var mDate = isMeetup ? toMeetupDateYYYYMMDD($scope.paymentFormData && $scope.paymentFormData.meetupDate) : null;
        var mTime = isMeetup ? ($scope.paymentFormData && $scope.paymentFormData.meetupTime) : null;
        if (isMeetup) console.log('Submitting meetup payment...', { mDate: mDate, mTime: mTime, proofId: proofId, refNum: refNum });
        var amountToSubmit = isMeetup ? $scope.getMeetupDownpaymentAmount() : $scope.paymentOrder.amountDue;
        logPaymentButtonState('Submitting payment');
        console.log('[PAYMENT DEBUG] Submit payload:', { orderId: $scope.paymentOrder.orderId, amount: amountToSubmit, proofImageId: proofId, referenceNumber: refNum, isMeetup: isMeetup });
        SolteiraBrandsService.submitPayment($scope.paymentOrder.orderId, amountToSubmit, proofId, refNum, mDate, mTime).then(function (r) {
            $scope.submitting = false;
            if (r.data && r.data.success) {
                Swal.fire({ icon: 'success', title: isMeetup ? 'Meetup scheduled & payment submitted' : 'Payment submitted', text: r.data.message || 'We will verify and update your order status.', confirmButtonText: 'OK' }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsMyOrders';
                });
            } else {
                var msg = (r.data && r.data.message) || 'Submission failed.';
                $scope.paymentError = msg;
                console.warn('[PAYMENT DEBUG] Server returned non-success:', r.data);
                Swal.fire({ icon: 'error', title: 'Payment failed', text: msg });
            }
        }, function (err) {
            $scope.submitting = false;
            var msg = (err.data && err.data.message) || (err.status && ('Server error ' + err.status)) || 'Submission failed. Please try again.';
            $scope.paymentError = msg;
            console.error('[PAYMENT DEBUG] Submit error:', err.status, err.data, err);
            Swal.fire({ icon: 'error', title: 'Payment failed', text: msg });
        });
    };

    $scope.placeOrder = function () {
        var isBuyer = !!(window.sbIsBuyer);
        if (!isBuyer) {
            Swal.fire({ icon: 'info', title: 'Checkout', text: 'Only buyers can complete checkout.', confirmButtonColor: '#28a745' });
            return;
        }
        var selected = $scope.getSelectedItems();
        if (!selected || selected.length === 0) {
            Swal.fire({ icon: 'warning', title: 'Empty order', text: 'Select at least one item to order.' });
            return;
        }
        var name = ($scope.customerName || '').trim();
        var contact = ($scope.contactNumber || '').trim();
        var address = ($scope.deliveryAddress || '').trim();
        var payment = ($scope.paymentMethod || '').trim();
        if (!name) { Swal.fire({ icon: 'warning', title: 'Required', text: 'Enter your full name.' }); return; }
        if (!contact) { Swal.fire({ icon: 'warning', title: 'Required', text: 'Enter your contact number.' }); return; }
        if (!address) { Swal.fire({ icon: 'warning', title: 'Required', text: 'Enter your delivery address.' }); return; }
        if (!payment) { Swal.fire({ icon: 'warning', title: 'Required', text: 'Select a payment option.' }); return; }

        var payload = {
            CustomerName: name,
            ContactNumber: contact,
            DeliveryAddress: address,
            PaymentMethod: payment,
            TotalAmount: $scope.getCheckoutSubtotal(),
            Items: selected.map(function (item) {
                return {
                    ProductId: item.id,
                    ProductName: item.name,
                    Quantity: item.qty,
                    UnitPrice: item.price,
                    ImageUrl: item.img || null
                };
            })
        };

        Swal.fire({
            title: 'Processing Order...',
            text: 'Please wait while we verify your transaction.',
            allowOutsideClick: false,
            didOpen: function () { Swal.showLoading(); }
        });

        SolteiraBrandsService.createOrder(payload).then(function (r) {
            if (r.data && r.data.success) {
                $scope.cart = $scope.cart.filter(function (item) { return !item.selected; });
                localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
                Swal.fire({
                    title: 'Order Placed!',
                    text: 'Order ' + (r.data.orderNumber || '') + ' has been submitted. You will receive confirmation via email.',
                    icon: 'success',
                    confirmButtonText: 'OK'
                }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsMyOrders';
                });
            } else {
                Swal.fire({ icon: 'error', title: 'Order failed', text: (r.data && r.data.message) || 'Please try again.' });
            }
        }, function (err) {
            var msg = (err.data && err.data.message) || (err.data && err.data.Message) || 'Could not place order. Please try again.';
            Swal.fire({ icon: 'error', title: 'Order failed', text: msg });
        });
    };

});
