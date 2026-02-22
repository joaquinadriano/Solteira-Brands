app.controller("SolteiraBrandsController", function ($scope, SolteiraBrandsService, $timeout, $interval) {

    console.log("Controller Loaded");

    $scope.viewMode = 'list';
    $scope.qty = 1;

    $scope.jewelries = [];
    $scope.defaultProductImageUrl = SolteiraBrandsService.defaultProductImageUrl;

    $scope.loadStorefrontProducts = function () {
        SolteiraBrandsService.getStorefrontProducts().then(function (res) {
            if (res.data && res.data.success && angular.isArray(res.data.data)) {
                $scope.jewelries = res.data.data;
            } else {
                $scope.jewelries = [];
            }
        }, function () {
            $scope.jewelries = [];
        });
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

    $scope.getRelatedProducts = function () {
        if (!$scope.selectedProduct) return [];

        let related = $scope.jewelries.filter(j =>
            j.category === $scope.selectedProduct.category && j.id !== $scope.selectedProduct.id
        );

        if (related.length < 4) {
            let others = $scope.jewelries.filter(j =>
                j.category !== $scope.selectedProduct.category && j.id !== $scope.selectedProduct.id
            );
            related = related.concat(others).slice(0, 4);
        }

        return related.slice(0, 4);
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
    var isBuyer = !!(window.sbIsBuyer);
    $scope.cart = isBuyer ? (JSON.parse(localStorage.getItem('sb_cart')) || []) : [];

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
        if (!$scope.cart || $scope.cart.length === 0) return;
        var ids = $scope.cart.map(function (i) { return i.id; });
        SolteiraBrandsService.getProductStocks(ids).then(function (r) {
            if (!r.data || !r.data.success || !r.data.stocks) return;
            var byId = {};
            (r.data.stocks || []).forEach(function (s) { byId[s.productId] = s.stockQuantity; });
            var messages = [];
            for (var i = $scope.cart.length - 1; i >= 0; i--) {
                var item = $scope.cart[i];
                var stock = byId[item.id] !== undefined ? parseInt(byId[item.id], 10) : 0;
                item.maxStock = stock;
                if (stock < 1) {
                    $scope.cart.splice(i, 1);
                    messages.push(item.name + ' is no longer available and was removed.');
                } else if ((item.qty || 0) > stock) {
                    item.qty = stock;
                    messages.push(item.name + ': quantity adjusted to ' + stock + ' (max available).');
                }
            }
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
            syncCartCountToLayout();
            if (messages.length > 0) {
                Swal.fire({ icon: 'info', title: 'Cart updated', text: messages.join(' ') });
            }
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
        }, function () {
            $scope.orders = [];
        });
    };
    $scope.viewOrderDetails = function (orderId) {
        if (orderId) {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsMyOrders?orderId=' + encodeURIComponent(orderId);
        }
    };

    $scope.placeOrder = function () {
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
