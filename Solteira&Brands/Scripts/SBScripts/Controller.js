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
                $scope.viewMode = 'detail';
                $scope.transitionDir = 'forward';
                window.scrollTo({ top: 0, behavior: 'smooth' });
            });
        });
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

    $scope.addToCart = function (product, quantity) {
        // Anonymous or non-BUYER user: redirect to login instead of storing a guest cart
        if (!isBuyer) {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
            return;
        }
        let cartItem = {
            id: product.id,
            name: product.name,
            price: product.price,
            img: product.img,
            stones: product.stones,
            material: product.material,
            weight: product.weight,
            qty: parseInt(quantity) || 1,
            selected: true
        };

        let existingItem = $scope.cart.find(i => i.id === product.id);

        if (existingItem) {
            existingItem.qty += cartItem.qty;
        } else {
            $scope.cart.push(cartItem);
        }

        localStorage.setItem('sb_cart', JSON.stringify($scope.cart));

        Swal.fire({
            icon: 'success',
            title: 'Added to Cart',
            text: product.name + ' is now in your bag.',
            timer: 1500,
            showConfirmButton: false
        });
    };

    $scope.removeFromCart = function (index) {
        $scope.cart.splice(index, 1);
        localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
    };

    $scope.removeFromCartByItem = function (item) {
        var idx = $scope.cart.findIndex(function (i) { return i.id === item.id; });
        if (idx !== -1) {
            $scope.cart.splice(idx, 1);
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
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
        SolteiraBrandsService.uploadProfileImage(file).then(function (r) {
            if (r.data && r.data.success && r.data.imageID) {
                SolteiraBrandsService.updateProfile(r.data.imageID, null).then(function (up) {
                    if (up.data && up.data.success) {
                        var newUrl = (r.data.imagePath || '') + (r.data.imageFileName || '');
                        $scope.profile.avatarUrl = newUrl;
                        var el = document.getElementById('layoutProfileImg');
                        var trigger = document.getElementById('layoutProfileImgTrigger');
                        if (el) el.src = newUrl;
                        if (trigger) trigger.src = newUrl;
                        Swal.fire({ icon: 'success', title: 'Photo updated', timer: 1500, showConfirmButton: false });
                    }
                });
            } else {
                Swal.fire({ icon: 'error', title: 'Upload failed', text: (r.data && r.data.message) || 'Try again.' });
            }
        }, function () {
            Swal.fire({ icon: 'error', title: 'Upload failed', text: 'Try again.' });
        });
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
        SolteiraBrandsService.updateProfile(null, username).then(function (r) {
            if (r.data && r.data.success) {
                Swal.fire({ icon: 'success', title: 'Saved', text: 'Profile updated.', timer: 1500, showConfirmButton: false });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (r.data && r.data.message) || 'Try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Try again.' });
        });
    };

    $scope.selectAll = true;
    $scope.toggleAll = function () {
        angular.forEach($scope.cart, function (item) {
            item.selected = $scope.selectAll;
        });
    };

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

    $scope.orders = JSON.parse(localStorage.getItem('sb_orders')) || [];

    $scope.viewOrderDetails = function (orderId) {
        if (orderId) {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsMyOrders?orderId=' + encodeURIComponent(orderId);
        }
    };

    $scope.placeOrder = function () {
        Swal.fire({
            title: 'Processing Order...',
            text: 'Please wait while we verify your transaction.',
            allowOutsideClick: false,
            didOpen: function () { Swal.showLoading(); }
        });
        var selected = $scope.getSelectedItems();
        var orderId = 'ORD-' + Date.now();
        var newOrder = {
            orderId: orderId,
            status: 'Pending',
            deliveryStatus: 'Preparing',
            items: selected,
            total: $scope.getCheckoutSubtotal()
        };
        $scope.orders.push(newOrder);
        localStorage.setItem('sb_orders', JSON.stringify($scope.orders));
        $scope.cart = $scope.cart.filter(function (item) { return !item.selected; });
        localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
        Swal.fire({
            title: 'Order Placed!',
            text: 'Order ' + orderId + ' has been submitted. You will receive confirmation via email.',
            icon: 'success',
            confirmButtonText: 'OK'
        }).then(function () {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsMyOrders';
        });
    };

});
