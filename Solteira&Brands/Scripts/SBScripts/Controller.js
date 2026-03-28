app.controller("SolteiraBrandsController", function ($scope, SolteiraBrandsService, $timeout, $interval) {

    console.log("Controller Loaded");

    $scope.viewMode = 'list';
    $scope.qty = 1;
    /** Jewelries product detail: set when user types a quantity above available stock. */
    $scope.detailQtyExceedsStockMessage = '';

    $scope.jewelries = [];
    $scope.defaultProductImageUrl = SolteiraBrandsService.defaultProductImageUrl;

    /** Jewelries page: open detail when URL has ?id= after catalog has loaded (init ran too early before). */
    function tryOpenProductFromUrl() {
        if (!document.querySelector('.jewelry-grid-container')) return;
        var urlParams = new URLSearchParams(window.location.search);
        var productId = parseInt(urlParams.get('id'), 10);
        if (!productId) return;
        var product = ($scope.jewelries || []).find(function (j) { return j.id === productId; });
        if (!product) return;
        $scope.selectedProduct = product;
        $scope.qty = 1;
        $scope.detailQtyExceedsStockMessage = '';
        $scope.viewMode = 'detail';
        $scope.transitionDir = 'forward';
        window.scrollTo({ top: 0, behavior: 'smooth' });
        $scope.buildRelatedSlides();
        $timeout(function () { $scope.startRelatedCarousel(); }, 0);
        SolteiraBrandsService.getProductStock(product.id).then(function (r) {
            if (r.data && r.data.success && $scope.selectedProduct && $scope.selectedProduct.id === product.id) {
                $scope.selectedProduct.stockQuantity = r.data.stockQuantity;
                $scope.capDetailQtyToStock();
            }
        });
    }

    $scope.loadStorefrontProducts = function () {
        SolteiraBrandsService.getStorefrontProducts().then(function (res) {
            if (res.data && res.data.success && angular.isArray(res.data.data)) {
                $scope.jewelries = res.data.data;
                $scope.buildFeaturedSlides();
                tryOpenProductFromUrl();
                $timeout(function () {
                    if (document.querySelector('.featured-carousel-container')) {
                        $scope.startFeaturedCarousel();
                    }
                }, 0);
            } else {
                $scope.jewelries = [];
                $scope.featuredCarouselSlides = [];
            }
        }, function () {
            $scope.jewelries = [];
            $scope.featuredCarouselSlides = [];
        });
    };

    $scope.featuredCarouselSlides = [];
    $scope.currentFeaturedSlide = 0;
    $scope.featuredCarouselInterval = null;
    var FEATURED_SLIDE_INTERVAL_MS = 10000;
    var FEATURED_MAX = 12;

    /** Up to 4 featured products per slide; next slide shows the remainder (e.g. 5 items → slide of 4, then slide of 1). */
    $scope.buildFeaturedSlides = function () {
        var list = ($scope.jewelries || []).filter(function (j) { return j.isFeatured === true; }).slice(0, FEATURED_MAX);
        var slides = [];
        for (var i = 0; i < list.length; i += 4) {
            slides.push(list.slice(i, i + 4));
        }
        $scope.featuredCarouselSlides = slides;
        $scope.currentFeaturedSlide = 0;
    };

    $scope.nextFeaturedSlide = function () {
        var total = $scope.featuredCarouselSlides.length;
        if (total <= 1) return;
        $scope.currentFeaturedSlide++;
        if ($scope.currentFeaturedSlide >= total) $scope.currentFeaturedSlide = 0;
        $scope.stopFeaturedCarousel();
        $scope.startFeaturedCarousel();
    };
    $scope.prevFeaturedSlide = function () {
        var total = $scope.featuredCarouselSlides.length;
        if (total <= 1) return;
        $scope.currentFeaturedSlide--;
        if ($scope.currentFeaturedSlide < 0) $scope.currentFeaturedSlide = total - 1;
        $scope.stopFeaturedCarousel();
        $scope.startFeaturedCarousel();
    };

    $scope.goToFeaturedSlide = function (index) {
        var total = $scope.featuredCarouselSlides.length;
        if (total <= 1 || index < 0 || index >= total) return;
        $scope.currentFeaturedSlide = index;
        $scope.stopFeaturedCarousel();
        $scope.startFeaturedCarousel();
    };

    $scope.startFeaturedCarousel = function () {
        if ($scope.featuredCarouselInterval) $interval.cancel($scope.featuredCarouselInterval);
        if ($scope.featuredCarouselSlides.length <= 1) return;
        $scope.featuredCarouselInterval = $interval(function () {
            var total = $scope.featuredCarouselSlides.length;
            $scope.currentFeaturedSlide++;
            if ($scope.currentFeaturedSlide >= total) $scope.currentFeaturedSlide = 0;
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
        var params = new URLSearchParams(window.location.search);
        if (params.get('from') === 'featured') {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsHome';
            return;
        }
        $scope.stopRelatedCarousel();
        $scope.relatedCarouselSlides = [];
        $scope.currentRelatedSlide = 0;
        $scope.transitionDir = 'backward';
        $scope.viewMode = 'list';
        $scope.selectedProduct = null;
        if (window.history && window.history.replaceState) {
            if (params.has('id')) {
                params.delete('id');
                var qs = params.toString();
                window.history.replaceState(null, '', window.location.pathname + (qs ? '?' + qs : '') + window.location.hash);
            }
        }
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
                $scope.detailQtyExceedsStockMessage = '';
                $scope.viewMode = 'detail';
                $scope.transitionDir = 'forward';
                window.scrollTo({ top: 0, behavior: 'smooth' });
                $scope.buildRelatedSlides();
                $timeout(function () { $scope.startRelatedCarousel(); }, 0);
                if (window.history && window.history.replaceState) {
                    try {
                        var url = new URL(window.location.href);
                        url.searchParams.set('id', String(item.id));
                        url.searchParams.delete('from');
                        window.history.replaceState(null, '', url.pathname + url.search + url.hash);
                    } catch (e) { /* ignore */ }
                }
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
        $scope.detailQtyExceedsStockMessage = '';
        var max = parseInt($scope.selectedProduct.stockQuantity, 10) || 0;
        var current = parseInt($scope.qty, 10) || 1;
        var next = Math.max(1, Math.min(max, current + delta));
        $scope.qty = next;
    };

    $scope.capDetailQtyToStock = function () {
        if (!$scope.selectedProduct) return;
        var max = parseInt($scope.selectedProduct.stockQuantity, 10) || 0;
        var val = parseInt($scope.qty, 10);
        if (isNaN(val) || val < 1) {
            $scope.qty = 1;
            $scope.detailQtyExceedsStockMessage = '';
            return;
        }
        if (max > 0 && val > max) {
            $scope.detailQtyExceedsStockMessage = 'You entered ' + val + ', but only ' + max + ' ' + (max === 1 ? 'is' : 'are') + ' in stock. Quantity was set to ' + max + '.';
            $scope.qty = max;
            return;
        }
        $scope.detailQtyExceedsStockMessage = '';
    };

    $scope.excludeSelected = function (item) {
        return item.id !== $scope.selectedProduct.id;
    };

    $scope.relatedCarouselSlides = [];
    $scope.currentRelatedSlide = 0;
    $scope.relatedCarouselInterval = null;

    var RELATED_SLIDE_INTERVAL_MS = FEATURED_SLIDE_INTERVAL_MS; // Match featured transitions timing.
    var RELATED_MAX_PER_SLIDE = 4;

    function normalizeCategory(cat) {
        return (cat === undefined || cat === null) ? '' : cat.toString().trim().toLowerCase();
    }

    // Related products must be the same category (and never the selected product itself).
    $scope.getRelatedProducts = function () {
        if (!$scope.selectedProduct) return [];

        var selectedCategory = normalizeCategory($scope.selectedProduct.category);

        return ($scope.jewelries || []).filter(function (j) {
            if (!j) return false;
            return normalizeCategory(j.category) === selectedCategory && j.id !== $scope.selectedProduct.id;
        });
    };

    // Build 4-items-per-slide carousel.
    $scope.buildRelatedSlides = function () {
        var related = $scope.getRelatedProducts();

        var slides = [];
        for (var i = 0; i < related.length; i += RELATED_MAX_PER_SLIDE) {
            slides.push(related.slice(i, i + RELATED_MAX_PER_SLIDE));
        }

        $scope.relatedCarouselSlides = slides;
        $scope.currentRelatedSlide = 0;
    };

    $scope.nextRelatedSlide = function () {
        var total = $scope.relatedCarouselSlides.length;
        if (total <= 1) return;

        $scope.currentRelatedSlide++;
        if ($scope.currentRelatedSlide >= total) $scope.currentRelatedSlide = 0;
        $scope.stopRelatedCarousel();
        $scope.startRelatedCarousel();
    };

    $scope.prevRelatedSlide = function () {
        var total = $scope.relatedCarouselSlides.length;
        if (total <= 1) return;

        $scope.currentRelatedSlide--;
        if ($scope.currentRelatedSlide < 0) $scope.currentRelatedSlide = total - 1;
        $scope.stopRelatedCarousel();
        $scope.startRelatedCarousel();
    };

    $scope.goToRelatedSlide = function (index) {
        var total = $scope.relatedCarouselSlides.length;
        if (total <= 1 || index < 0 || index >= total) return;

        $scope.currentRelatedSlide = index;
        $scope.stopRelatedCarousel();
        $scope.startRelatedCarousel();
    };

    $scope.startRelatedCarousel = function () {
        if ($scope.relatedCarouselInterval) $interval.cancel($scope.relatedCarouselInterval);
        if ($scope.relatedCarouselSlides.length <= 1) return;

        $scope.relatedCarouselInterval = $interval(function () {
            var total = $scope.relatedCarouselSlides.length;
            $scope.currentRelatedSlide++;
            if ($scope.currentRelatedSlide >= total) $scope.currentRelatedSlide = 0;
        }, RELATED_SLIDE_INTERVAL_MS);
    };

    $scope.stopRelatedCarousel = function () {
        if ($scope.relatedCarouselInterval) {
            $interval.cancel($scope.relatedCarouselInterval);
            $scope.relatedCarouselInterval = null;
        }
    };

    /** Short description for product cards: max 2 lines / ~80 chars, ellipsis. */
    $scope.getShortDescription = function (item) {
        if (!item) return '';
        var raw = (item.description || item.stones || item.material || '').toString().trim();
        if (!raw) return '';
        if (raw.length <= 80) return raw;
        return raw.substring(0, 77) + '...';
    };

    // Jewelries page: filter and sort
    $scope.jewelriesSearch = '';
    $scope.jewelriesSort = '';
    $scope.jewelriesCategory = '';

    // Jewelries filters: make options mutually exclusive.
    // When a sort is chosen (A–Z, Z–A, Price L–H, Price H–L), clear any category filter.
    $scope.setJewelriesSort = function (sort) {
        if ($scope.jewelriesSort === sort) {
            // Clicking the same sort again clears it.
            $scope.jewelriesSort = '';
        } else {
            $scope.jewelriesSort = sort;
            // Ensure only one type of filter is active at a time.
            $scope.jewelriesCategory = '';
        }
    };

    // When a category is chosen (Bracelet, Earring, Necklace, Ring), clear any sort filter.
    $scope.setJewelriesCategory = function (cat) {
        if ($scope.jewelriesCategory === cat) {
            // Clicking the same category again clears it.
            $scope.jewelriesCategory = '';
        } else {
            $scope.jewelriesCategory = cat;
            // Ensure only one type of filter is active at a time.
            $scope.jewelriesSort = '';
        }
    };
    $scope.getFilteredJewelries = function () {
        var list = $scope.jewelries || [];
        var search = ($scope.jewelriesSearch || '').toLowerCase().trim();
        var cat = $scope.jewelriesCategory || '';
        if (search) {
            list = list.filter(function (j) {
                var name = (j.name || '').toLowerCase();
                var category = (j.category || '').toLowerCase();
                var desc = (j.description || '').toLowerCase();
                return name.indexOf(search) !== -1 || category.indexOf(search) !== -1 || desc.indexOf(search) !== -1;
            });
        }
        if (cat) {
            list = list.filter(function (j) { return (j.category || '') === cat; });
        }
        var sort = $scope.jewelriesSort || '';
        if (sort === 'az') list = list.slice().sort(function (a, b) { return (a.name || '').localeCompare(b.name || ''); });
        else if (sort === 'za') list = list.slice().sort(function (a, b) { return (b.name || '').localeCompare(a.name || ''); });
        else if (sort === 'price_lh') list = list.slice().sort(function (a, b) { return (a.price || 0) - (b.price || 0); });
        else if (sort === 'price_hl') list = list.slice().sort(function (a, b) { return (b.price || 0) - (a.price || 0); });
        return list;
    };

    $scope.currentMode = 'registration';

    $scope.init = function () {
        const urlParams = new URLSearchParams(window.location.search);

        // Load products for storefront jewelries page (?id= deep link handled after load in tryOpenProductFromUrl)
        if (document.querySelector('.jewelry-grid-container')) {
            $scope.loadStorefrontProducts();
        }

        const mode = urlParams.get('mode');
        if (mode) {
            $scope.currentMode = mode;
        }
        const emailParam = urlParams.get('email');
        if (emailParam) {
            $scope.verificationEmail = decodeURIComponent(emailParam);
        }
    };

    $scope.init();

    $scope.goToProduct = function (item) {
        if (!item || item.id == null) return;
        window.location.href = '/SolteiraAndBrands/SolteiraBrandsJewelries?id=' + encodeURIComponent(item.id) + '&from=featured';
    };

    $scope.showPassword = false;
    $scope.showVerifyPassword = false;

    $scope.loginData = {
        username: '',
        password: '',
        rememberMe: false
    };

    $scope.loginErrors = { username: '', password: '' };

    $scope.registerData = {
        email: '',
        username: '',
        password: '',
        verifyPassword: '',
        acceptedTerms: false
    };

    $scope.registerErrors = { email: '', username: '', password: '', verifyPassword: '', acceptedTerms: '' };

    function validateEmailFormat(email) {
        if (!email) return "Email is required.";
        var re = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
        if (!re.test(email)) return "Enter a valid email address.";
        return "";
    }

    function validateUsernameFormat(u) {
        if (!u) return "Username is required.";
        if (u.length < 5 || u.length > 14) return "Username must be 5-14 characters.";
        if (!/^[A-Za-z0-9_]+$/.test(u)) return "Use letters, numbers, and underscore only.";
        return "";
    }

    function validatePassword(p) {
        var msg = 'Minimum 8 characters. Must include uppercase, lowercase, number, and special character.';
        if (!p || p.length < 8) return msg;
        if (p.length > 16) return msg;
        if (!/[A-Z]/.test(p)) return msg;
        if (!/[a-z]/.test(p)) return msg;
        if (!/[0-9]/.test(p)) return msg;
        if (!/[^A-Za-z0-9]/.test(p)) return msg;
        return '';
    }

    $scope.onEmailInput = function () {
        var e = $scope.registerData.email;
        $scope.registerErrors.email = validateEmailFormat(e);
    };

    $scope.onEmailBlur = function () {
        var e = $scope.registerData.email;
        var err = validateEmailFormat(e);
        $scope.registerErrors.email = err;
    };

    $scope.onUsernameInput = function () {
        var u = $scope.registerData.username;
        $scope.registerErrors.username = validateUsernameFormat(u);
    };

    $scope.onUsernameBlur = function () {
        var u = $scope.registerData.username;
        var err = validateUsernameFormat(u);
        $scope.registerErrors.username = err;
        if (err) return;
        // Check database for uniqueness when format is valid
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
            $scope.registerErrors.verifyPassword = (p && v && p === v) ? '' : 'Passwords do not match.';
        }
    };

    $scope.onVerifyPasswordInput = function () {
        var p = $scope.registerData.password || '';
        var v = $scope.registerData.verifyPassword || '';
        if (!v) {
            $scope.registerErrors.verifyPassword = '';
            return;
        }
        $scope.registerErrors.verifyPassword = (p && v && p === v) ? '' : 'Passwords do not match.';
    };

    $scope.onAcceptedTermsChange = function () {
        if ($scope.registerData && $scope.registerData.acceptedTerms) {
            $scope.registerErrors.acceptedTerms = '';
        }
    };

    $scope.register = function (form) {
        if (!form) return;

        var termsMsg = 'You must agree to the Terms & Conditions and Privacy Policy';
        if (!$scope.registerData || !$scope.registerData.acceptedTerms) {
            $scope.registerErrors.acceptedTerms = termsMsg;
            Swal.fire({
                icon: 'error',
                title: 'Cannot create account',
                text: termsMsg
            });
            return;
        }
        $scope.registerErrors.acceptedTerms = '';

        var eErr = validateEmailFormat($scope.registerData.email);
        var uErr = validateUsernameFormat($scope.registerData.username);
        var p = $scope.registerData.password;
        var v = $scope.registerData.verifyPassword;
        var pErr = validatePassword(p);

        if (eErr || uErr || pErr || p !== v) {
            $scope.registerErrors.email = eErr;
            $scope.registerErrors.username = uErr;
            if (pErr) $scope.registerErrors.password = pErr;
            if (p !== v) $scope.registerErrors.verifyPassword = 'Passwords do not match.';
            var primaryMsg = eErr || uErr || pErr || (p !== v ? 'Passwords do not match.' : '');
            if (primaryMsg) {
                Swal.fire({
                    icon: 'error',
                    title: 'Cannot create account',
                    text: primaryMsg
                });
            }
            return;
        }

        var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        var token = tokenEl ? tokenEl.value : '';
        SolteiraBrandsService.register($scope.registerData, token).then(function (r) {
            if (r.data && r.data.success) {
                if (r.data.requireVerification && r.data.email) {
                    if (r.data.emailSent === false) {
                        Swal.fire({ icon: 'warning', title: 'Check your email', text: r.data.message || 'We could not send the verification email. Check spam or try again later.' }).then(function () {
                            window.location.href = '/SolteiraAndBrands/SolteiraBrandsOTP?email=' + encodeURIComponent(r.data.email);
                        });
                    } else {
                        window.location.href = '/SolteiraAndBrands/SolteiraBrandsOTP?email=' + encodeURIComponent(r.data.email);
                    }
                    return;
                }
                Swal.fire({ icon: 'success', title: 'Account created', text: r.data.message || 'You can now log in.' }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                });
            } else if (r.data && r.data.errors) {
                angular.extend($scope.registerErrors, r.data.errors);
                var serverEmailErr = r.data.errors.email || r.data.errors.Email;
                var serverUsernameErr = r.data.errors.username || r.data.errors.Username;
                var serverMsg = serverEmailErr || serverUsernameErr || r.data.message;
                if (serverMsg) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Cannot create account',
                        text: serverMsg
                    });
                }
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
        SolteiraBrandsService.login($scope.loginData.username, $scope.loginData.password, !!$scope.loginData.rememberMe).then(function (r) {
            if (r.data && r.data.redirectUrl) {
                // Replace history entry so Back doesn't return to the login page (avoids confusion).
                window.location.replace(r.data.redirectUrl);
                return;
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
        // If currently locked due to too many failures, do not resend.
        if ($scope.otpLockedUntil && Date.now() < $scope.otpLockedUntil) {
            var remainingMs = $scope.otpLockedUntil - Date.now();
            var remainingSec = Math.ceil(remainingMs / 1000);
            $scope.otpError = 'Too many invalid codes. Please wait ' + remainingSec + 's before requesting a new code.';
            return;
        }
        var email = ($scope.verificationEmail || '').trim();
        if (!email) {
            $scope.otpError = 'Session missing. Please request a new code from the registration or Forgot Password page.';
            return;
        }
        var mode = $scope.currentMode || '';
        var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        var token = tokenEl ? tokenEl.value : '';
        $scope.otpError = '';
        SolteiraBrandsService.resendOtp(email, mode, token).then(function (r) {
            if (r.data && r.data.success) {
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: 'New OTP sent to email!',
                    showConfirmButton: false,
                    timer: 2500,
                    timerProgressBar: true
                });
                $scope.otpAttempts = 0;
                $scope.otpLockedUntil = null;
                $scope.startTimer();
            } else {
                $scope.otpError = (r.data && r.data.message) || 'Could not send a new code. Please try again.';
            }
        }, function (err) {
            $scope.otpError = (err.data && err.data.message) || 'Could not send a new code. Please try again.';
        });
    };

    $scope.$on('$destroy', function () {
        if (angular.isDefined(timerInterval)) {
            $interval.cancel(timerInterval);
        }
        if (angular.isDefined(otpLockInterval)) {
            $interval.cancel(otpLockInterval);
        }
    });

    $scope.forgotPasswordSubmitting = false;
    $scope.forgotPasswordError = '';

    $scope.submitForgotPassword = function (ev) {
        if (ev && ev.preventDefault) ev.preventDefault();
        $scope.forgotPasswordError = '';
        var email = ($scope.userEmail || '').trim();
        var emailErr = validateEmailFormat(email);
        if (emailErr) {
            $scope.forgotPasswordError = emailErr;
            return;
        }
        // If OTP is currently locked (15‑minute cooldown), block new requests for this device.
        if ($scope.otpLockedUntil && Date.now() < $scope.otpLockedUntil) {
            var remainingMs = $scope.otpLockedUntil - Date.now();
            var remainingSec = Math.ceil(remainingMs / 1000);
            var mins = Math.floor(remainingSec / 60);
            var secs = remainingSec % 60;
            var msg = 'You have reached the maximum OTP attempts. Please wait ';
            if (mins > 0) {
                msg += mins + ' minute' + (mins > 1 ? 's' : '');
                if (secs > 0) msg += ' and ';
            }
            if (secs > 0) {
                msg += secs + ' second' + (secs > 1 ? 's' : '');
            }
            msg += ' before requesting a new code.';

            Swal.fire({
                icon: 'error',
                title: 'OTP temporarily locked',
                text: msg
            });
            return;
        }
        var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        var token = tokenEl ? tokenEl.value : '';
        $scope.forgotPasswordSubmitting = true;
        SolteiraBrandsService.forgotPassword(email, token).then(function (r) {
            $scope.forgotPasswordSubmitting = false;
            if (r.data && r.data.success) {
                Swal.fire({ icon: 'success', title: 'Check your email', text: r.data.message || 'We sent you a verification code.' }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsOTP?mode=forgot&email=' + encodeURIComponent(r.data.email || email);
                });
            } else {
                $scope.forgotPasswordError = (r.data && r.data.message) || 'Something went wrong. Please try again.';
            }
        }, function (err) {
            $scope.forgotPasswordSubmitting = false;
            $scope.forgotPasswordError = (err.data && err.data.message) || 'Request failed. Please try again.';
        });
    };

    $scope.onForgotEmailInput = function () {
        var email = ($scope.userEmail || '').trim();
        var emailErr = validateEmailFormat(email);
        $scope.forgotPasswordError = emailErr;
    };

    $scope.sendOTP = function () {
        window.location.href = '/SolteiraAndBrands/SolteiraBrandsOTP?mode=forgot&email=' + encodeURIComponent($scope.userEmail || '');
    };

    $scope.otpValue = '';
    $scope.otpVerifying = false;
    $scope.otpError = '';
    $scope.otpAttempts = 0;
    $scope.otpLockedUntil = null;
    $scope.otpLockSecondsLeft = 0;
    var otpLockInterval;

    // Restore any existing OTP lock from previous visits (persisted for 15 minutes).
    (function restoreOtpLock() {
        try {
            var stored = localStorage.getItem('sb_otp_lock_until');
            if (stored) {
                var ts = parseInt(stored, 10);
                if (!isNaN(ts) && ts > Date.now()) {
                    $scope.otpLockedUntil = ts;
                } else {
                    localStorage.removeItem('sb_otp_lock_until');
                }
            }
        } catch (e) {
            // ignore storage errors
        }
    })();

    function startOtpLock(durationMs) {
        var until = Date.now() + durationMs;
        $scope.otpLockedUntil = until;
        $scope.otpLockSecondsLeft = Math.ceil(durationMs / 1000);
        $scope.otpError = 'Too many invalid codes. Please wait ' + $scope.otpLockSecondsLeft + 's before trying again.';
        try {
            localStorage.setItem('sb_otp_lock_until', String(until));
        } catch (e) {
            // ignore storage errors
        }

        if (angular.isDefined(otpLockInterval)) {
            $interval.cancel(otpLockInterval);
        }

        otpLockInterval = $interval(function () {
            if (!$scope.otpLockedUntil) {
                $scope.otpLockSecondsLeft = 0;
                $interval.cancel(otpLockInterval);
                return;
            }
            var remainingMs = $scope.otpLockedUntil - Date.now();
            if (remainingMs <= 0) {
                $scope.otpLockedUntil = null;
                $scope.otpLockSecondsLeft = 0;
                $scope.otpAttempts = 0;
                $scope.otpError = '';
                try {
                    localStorage.removeItem('sb_otp_lock_until');
                } catch (e) {
                    // ignore
                }
                $interval.cancel(otpLockInterval);
            } else {
                $scope.otpLockSecondsLeft = Math.ceil(remainingMs / 1000);
                $scope.otpError = 'Too many invalid codes. Please wait ' + $scope.otpLockSecondsLeft + 's before trying again.';
            }
        }, 1000);
    }

    $scope.submitOtp = function (ev) {
        if (ev && ev.preventDefault) ev.preventDefault();
        $scope.otpError = '';

        // Lockout check: 5 failed attempts -> 15 min lock with live countdown.
        if ($scope.otpLockedUntil && Date.now() < $scope.otpLockedUntil) {
            return;
        }
        if ($scope.otpLockedUntil && Date.now() >= $scope.otpLockedUntil) {
            // Lock expired, reset attempts.
            $scope.otpLockedUntil = null;
            $scope.otpAttempts = 0;
        }
        if ($scope.currentMode === 'forgot') {
            var email = $scope.verificationEmail || '';
            var otp = ($scope.otpValue || '').trim();
            if (!email) {
                $scope.otpError = 'Session missing. Please request a new code from the Forgot Password page.';
                return;
            }
            if (otp.length !== 6) {
                $scope.otpError = 'Please enter the 6-digit code from your email.';
                return;
            }
            var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
            var token = tokenEl ? tokenEl.value : '';
            $scope.otpVerifying = true;
            SolteiraBrandsService.validatePasswordResetOtp(email, otp, token).then(function (r) {
                $scope.otpVerifying = false;
                if (r.data && r.data.success) {
                    Swal.fire({ icon: 'success', title: 'Code verified', text: r.data.message || 'Set your new password.' }).then(function () {
                        window.location.href = '/SolteiraAndBrands/SolteiraBrandsNewPassword?email=' + encodeURIComponent(r.data.email || email);
                    });
                } else {
                    $scope.otpAttempts++;
                    if ($scope.otpAttempts >= 5) {
                        startOtpLock(15 * 60 * 1000);
                        Swal.fire({
                            icon: 'error',
                            title: 'Too many attempts',
                            text: 'You have entered an invalid code 5 times. Please wait 15 minutes before trying again.'
                        }).then(function () {
                            window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                        });
                    } else {
                        $scope.otpError = (r.data && r.data.message) || 'Invalid or expired code. Please try again.';
                    }
                }
            }, function (err) {
                $scope.otpVerifying = false;
                $scope.otpAttempts++;
                if ($scope.otpAttempts >= 5) {
                    startOtpLock(15 * 60 * 1000);
                    Swal.fire({
                        icon: 'error',
                        title: 'Too many attempts',
                        text: 'You have entered an invalid code 5 times. Please wait 15 minutes before trying again.'
                    }).then(function () {
                        window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                    });
                } else {
                    $scope.otpError = (err.data && err.data.message) || 'Verification failed. Please try again.';
                }
            });
            return;
        }
        if (!$scope.verificationEmail) {
            $scope.otpError = 'Session missing. Please register again.';
            return;
        }
        var otp = ($scope.otpValue || '').trim();
        if (otp.length !== 6) {
            $scope.otpError = 'Please enter the 6-digit code from your email.';
            return;
        }
        var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        var token = tokenEl ? tokenEl.value : '';
        $scope.otpVerifying = true;
        SolteiraBrandsService.verifyAccount($scope.verificationEmail, otp, token).then(function (r) {
            $scope.otpVerifying = false;
            if (r.data && r.data.success) {
                Swal.fire({ icon: 'success', title: 'Email verified', text: r.data.message || 'You can now sign in.' }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                });
            } else {
                $scope.otpAttempts++;
                if ($scope.otpAttempts >= 5) {
                    startOtpLock(15 * 60 * 1000);
                    Swal.fire({
                        icon: 'error',
                        title: 'Too many attempts',
                        text: 'You have entered an invalid code 5 times. Please wait 15 minutes before trying again.'
                    }).then(function () {
                        window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                    });
                } else {
                    $scope.otpError = (r.data && r.data.message) || 'Invalid or expired code. Please try again.';
                }
            }
        }, function (err) {
            $scope.otpVerifying = false;
            $scope.otpAttempts++;
            if ($scope.otpAttempts >= 5) {
                startOtpLock(15 * 60 * 1000);
                Swal.fire({
                    icon: 'error',
                    title: 'Too many attempts',
                    text: 'You have entered an invalid code 5 times. Please wait 15 minutes before trying again.'
                }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                });
            } else {
                $scope.otpError = (err.data && err.data.message) || 'Verification failed. Please try again.';
            }
        });
    };

    // Clear OTP error when user starts typing again.
    $scope.onOtpInputChange = function () {
        $scope.otpError = '';
    };

    $scope.handleOTPSuccess = function () {
        if ($scope.currentMode === 'forgot') {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsNewPassword';
        } else {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
        }
    };

    $scope.resetPasswordSubmitting = false;
    $scope.resetPasswordError = '';
    $scope.resetPasswordErrors = { password: '', verifyPassword: '' };

    $scope.onResetPasswordInput = function () {
        var p = ($scope.registerData && $scope.registerData.password) || '';
        var v = ($scope.registerData && $scope.registerData.verifyPassword) || '';
        var pErr = validatePassword(p);
        $scope.resetPasswordErrors.password = pErr;
        if (v) {
            $scope.resetPasswordErrors.verifyPassword = (p && v && p === v) ? '' : 'Passwords do not match.';
        }
    };

    $scope.onResetVerifyPasswordInput = function () {
        var p = ($scope.registerData && $scope.registerData.password) || '';
        var v = ($scope.registerData && $scope.registerData.verifyPassword) || '';
        if (!v) {
            $scope.resetPasswordErrors.verifyPassword = '';
            return;
        }
        $scope.resetPasswordErrors.verifyPassword = (p && v && p === v) ? '' : 'Passwords do not match.';
    };

    $scope.submitResetPassword = function (ev) {
        if (ev && ev.preventDefault) ev.preventDefault();
        $scope.resetPasswordError = '';
        $scope.resetPasswordErrors.password = '';
        $scope.resetPasswordErrors.verifyPassword = '';
        var email = $scope.verificationEmail || '';
        var p = ($scope.registerData && $scope.registerData.password) || '';
        var v = ($scope.registerData && $scope.registerData.verifyPassword) || '';
        if (!email) {
            $scope.resetPasswordError = 'Session missing. Please start again from Forgot Password.';
            return;
        }
        var pErr = validatePassword(p);
        if (pErr) {
            $scope.resetPasswordErrors.password = pErr;
            return;
        }
        if (p !== v) {
            $scope.resetPasswordErrors.verifyPassword = 'Passwords do not match.';
            return;
        }
        var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        var token = tokenEl ? tokenEl.value : '';
        $scope.resetPasswordSubmitting = true;
        SolteiraBrandsService.resetPassword(email, p, v, token).then(function (r) {
            $scope.resetPasswordSubmitting = false;
            if (r.data && r.data.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Success',
                    text: 'Your password has been updated successfully. You can now sign in.',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#28a745'
                }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                });
            } else {
                $scope.resetPasswordError = (r.data && r.data.message) || 'Failed to update password. Please try again.';
            }
        }, function (err) {
            $scope.resetPasswordSubmitting = false;
            $scope.resetPasswordError = (err.data && err.data.message) || 'Request failed. Please try again.';
        });
    };

    // ----- CART: server-side per account (BUYER only) -----
    var isBuyer = !!(window.sbIsBuyer);
    var isLoggedIn = !!(window.sbIsLoggedIn);
    $scope.cart = [];

    function getSelectedCartSessionKey() {
        var username = (window.sbUsername || '').toString().trim();
        return username ? 'sb_selected_cart_items_' + username : 'sb_selected_cart_items';
    }

    function readSelectedIdsFromSession() {
        try {
            var raw = sessionStorage.getItem(getSelectedCartSessionKey());
            if (!raw) return [];
            var parsed = JSON.parse(raw);
            if (!Array.isArray(parsed)) return [];
            return parsed.map(function (x) { return parseInt(x, 10); }).filter(function (x) { return !isNaN(x); });
        } catch (e) {
            return [];
        }
    }

    function applySelectedIdsToCart(selectedIds) {
        selectedIds = selectedIds || [];
        var set = {};
        for (var i = 0; i < selectedIds.length; i++) set[selectedIds[i]] = true;
        for (var j = 0; j < ($scope.cart || []).length; j++) {
            $scope.cart[j].selected = !!set[$scope.cart[j].id];
        }
    }

    $scope.getCartCount = function () {
        if (!$scope.cart || !$scope.cart.length) return 0;
        return $scope.cart.reduce(function (sum, item) { return sum + (item.qty || 0); }, 0);
    };

    function syncCartCountToLayout() {
        if (typeof window.dispatchEvent === 'function') {
            window.dispatchEvent(new CustomEvent('sbCartUpdated', { detail: $scope.getCartCount() }));
        }
    }

    // applySessionSelection: when on checkout page, restore checkbox selection across page navigation.
    $scope.refreshCartFromServer = function (applySessionSelection) {
        if (!isBuyer) {
            $scope.cart = [];
            syncCartCountToLayout();
            return Promise.resolve();
        }

        // preserve current checkbox selection when staying on cart page
        var preservedSelectedIds = [];
        if (!applySessionSelection && $scope.cart && $scope.cart.length) {
            preservedSelectedIds = $scope.cart.filter(function (i) { return i.selected; }).map(function (i) { return i.id; });
        }

        return SolteiraBrandsService.getMyCart().then(function (r) {
            var items = (r && r.data && r.data.success && Array.isArray(r.data.items)) ? r.data.items : [];
            $scope.cart = (items || []).map(function (item) {
                return {
                    id: item.id,
                    name: item.name,
                    price: item.price,
                    img: item.img,
                    stones: item.stones,
                    material: item.material,
                    weight: item.weight,
                    length: item.length,
                    qty: item.qty,
                    maxStock: item.maxStock,
                    selected: false
                };
            });

            var selectedIds = applySessionSelection ? readSelectedIdsFromSession() : preservedSelectedIds;
            applySelectedIdsToCart(selectedIds);
            syncCartCountToLayout();
        }, function () {
            $scope.cart = [];
            syncCartCountToLayout();
        });
    };

    // Auto-load cart on checkout page so order summary renders immediately.
    (function autoLoadCartForCheckoutPage() {
        try {
            var isCheckout = !!document.querySelector('.checkout-main-container');
            if (isBuyer && isCheckout) $scope.refreshCartFromServer(true);
        } catch (e) { }
    })();

    $scope.addToCart = function (product, quantity) {
        if (!isBuyer) {
            if (!isLoggedIn) {
                Swal.fire({
                    icon: 'info',
                    title: 'Login required',
                    text: 'Please log in first to add items to your cart.'
                }).then(function () {
                    window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
                });
            } else {
                // Inventory roles (ADMIN / MANAGER / STAFF) can browse products, but cart actions are BUYER-only.
                Swal.fire({
                    icon: 'info',
                    title: 'Not allowed',
                    text: 'Your role cannot add items to cart. Only BUYERS can do this.'
                });
            }
            return;
        }
        var requestedQty = Math.max(1, parseInt(quantity, 10) || 1);
        var stockCap = parseInt(product.stockQuantity, 10) || 0;
        if (stockCap > 0 && requestedQty > stockCap) {
            requestedQty = stockCap;
            if ($scope.selectedProduct && product && $scope.selectedProduct.id === product.id) {
                $scope.qty = stockCap;
                $scope.detailQtyExceedsStockMessage = 'You tried to add more than is in stock. Only ' + stockCap + ' ' + (stockCap === 1 ? 'is' : 'are') + ' available.';
            }
        }
        SolteiraBrandsService.addToCart(product.id, requestedQty).then(function (r) {
            if (r && r.data && r.data.success) {
                var added = r.data.addedQuantity || 0;
                var msg = product.name + ' is now in your bag.';
                if (added < requestedQty) msg = 'Added ' + added + ' (max available). ' + product.name + ' is in your bag.';
                Swal.fire({ icon: 'success', title: 'Added to Cart', text: msg, timer: 1500, showConfirmButton: false });

                $scope.refreshCartFromServer(false).then(function () {
                    if (!$scope.cart) return;
                    $scope.cart.forEach(function (i) { i.selected = false; });
                    var updated = $scope.cart.find(function (i) { return i.id === product.id; });
                    if (updated) updated.selected = true;
                });
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: (r && r.data && r.data.message) ? r.data.message : 'Could not add to cart.' });
            }
        }, function () {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Could not add to cart. Please try again.' });
        });
    };

    $scope.removeFromCart = function (index) {
        if (!$scope.cart || index < 0 || index >= $scope.cart.length) return;
        var item = $scope.cart[index];
        if (!item) return;
        SolteiraBrandsService.removeCartItem(item.id).then(function () {
            $scope.refreshCartFromServer(false);
        }, function () {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Could not remove item.' });
        });
    };

    $scope.removeFromCartByItem = function (item) {
        if (!item || !item.id) return;
        SolteiraBrandsService.removeCartItem(item.id).then(function () {
            $scope.refreshCartFromServer(false);
        }, function () {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Could not remove item.' });
        });
    };

    $scope.updateCartQty = function (item, delta) {
        if (!item || !item.id) return;
        var current = parseInt(item.qty, 10) || 0;
        var desired = current + delta;
        if (desired < 1) {
            $scope.removeFromCartByItem(item);
            return;
        }

        SolteiraBrandsService.setCartItemQuantity(item.id, desired).then(function (r) {
            // server caps/removes according to stock
            $scope.refreshCartFromServer(false).then(function () {
                if (r && r.data && r.data.removed) {
                    Swal.fire({ icon: 'info', title: 'Item removed', text: item.name + ' is no longer available and was removed from your cart.' });
                }
            });
        }, function () {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Could not update quantity.' });
        });
    };

    $scope.onCartItemQtyChange = function (item) {
        if (!item || !item.id) return;
        var val = parseInt(item.qty, 10);
        if (isNaN(val) || val < 1) val = 1;
        item.qty = val;
        SolteiraBrandsService.setCartItemQuantity(item.id, val).then(function (r) {
            $scope.refreshCartFromServer(false).then(function () {
                if (r && r.data && r.data.removed) {
                    Swal.fire({ icon: 'info', title: 'Item removed', text: item.name + ' is no longer available and was removed from your cart.' });
                }
            });
        }, function () {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Could not update quantity.' });
        });
    };

    // no-op now: server-side GetMyCart caps quantities to stock.
    $scope.validateCartAgainstStock = function () { };

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
        // Persist only the selected product IDs so checkout can restore the correct checkboxes.
        try {
            var selectedIds = $scope.getSelectedItems().map(function (i) { return i.id; });
            sessionStorage.setItem(getSelectedCartSessionKey(), JSON.stringify(selectedIds));
        } catch (e) { }
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

    // Checkout gating helpers.
    // NOTE: Your checkout checkbox ng-model isn't syncing reliably, so we read the checkbox's DOM checked state.
    $scope.isCheckoutTermsAccepted = function () {
        try {
            var el = document.getElementById('agreeFull');
            if (el) return !!el.checked;
        } catch (e) { }
        return !!$scope.agreeTerms;
    };

    var CHECKOUT_NAME_MAX_LEN = 30;
    var CHECKOUT_ADDRESS_MAX_LEN = 50;

    $scope.isCheckoutReadyToPlaceOrder = function () {
        var name = ($scope.customerName || '').trim();
        var address = ($scope.deliveryAddress || '').trim();
        var nameOk = name.length > 0 && name.length <= CHECKOUT_NAME_MAX_LEN;
        var contact = ($scope.contactNumber || '').trim();
        var contactOk = /^\d{11}$/.test(contact);
        var addressOk = address.length > 0 && address.length <= CHECKOUT_ADDRESS_MAX_LEN;
        var paymentOk = !!(($scope.paymentMethod || '').trim());
        var termsOk = $scope.isCheckoutTermsAccepted();
        return nameOk && contactOk && addressOk && paymentOk && termsOk;
    };

    $scope.orders = [];
    $scope._paymentDeadlineCountdownIntervalId = null;
    $scope.initPaymentDeadlineCountdowns = function () {
        // Ensure only one interval is running.
        if ($scope._paymentDeadlineCountdownIntervalId) {
            try { clearInterval($scope._paymentDeadlineCountdownIntervalId); } catch (e) { }
            $scope._paymentDeadlineCountdownIntervalId = null;
        }
        if (!$scope.orders || $scope.orders.length === 0) return;

        function pad2(n) { return (n < 10 ? '0' : '') + n; }
        function formatRemaining(diffMs) {
            var totalSeconds = Math.max(0, Math.floor(diffMs / 1000));
            var days = Math.floor(totalSeconds / 86400);
            totalSeconds = totalSeconds % 86400;
            var hours = Math.floor(totalSeconds / 3600);
            totalSeconds = totalSeconds % 3600;
            var minutes = Math.floor(totalSeconds / 60);
            var seconds = totalSeconds % 60;
            // Display: Xd HH:MM:SS (matches the server "Expired" fallback when negative).
            return (days > 0 ? (days + 'd ') : '') + pad2(hours) + ':' + pad2(minutes) + ':' + pad2(seconds);
        }

        var tick = function () {
            if (!$scope.orders) return;
            var now = Date.now();
            var changed = false;
            angular.forEach($scope.orders, function (o) {
                if (!o) return;
                if (!o.paymentDeadlineUtcIso) return;
                var dl = new Date(o.paymentDeadlineUtcIso);
                if (isNaN(dl.getTime())) return;
                var diff = dl.getTime() - now;
                if (diff <= 0) {
                    if (o.paymentDeadlineRemainingText !== 'Expired') {
                        o.paymentDeadlineRemainingText = 'Expired';
                        o.paymentDeadlineExpired = true;
                        changed = true;
                    }
                } else {
                    var newText = formatRemaining(diff);
                    if (o.paymentDeadlineRemainingText !== newText) {
                        o.paymentDeadlineRemainingText = newText;
                        o.paymentDeadlineExpired = false;
                        changed = true;
                    }
                }
            });
            if (changed) $scope.$applyAsync();
        };

        tick();
        $scope._paymentDeadlineCountdownIntervalId = setInterval(tick, 1000);
    };
    $scope.loadMyOrders = function () {
        if (!isBuyer) return;
        SolteiraBrandsService.getMyOrders().then(function (r) {
            if (r.data && r.data.success && angular.isArray(r.data.data)) {
                $scope.orders = r.data.data;
            } else {
                $scope.orders = [];
            }
            $scope.initPaymentDeadlineCountdowns();
            $scope.checkOpenOrderDetailsFromQuery();
        }, function () {
            $scope.orders = [];
        });
    };

    // Prevent interval leaks when leaving the page.
    $scope.$on && $scope.$on('$destroy', function () {
        if ($scope._paymentDeadlineCountdownIntervalId) {
            try { clearInterval($scope._paymentDeadlineCountdownIntervalId); } catch (e) { }
            $scope._paymentDeadlineCountdownIntervalId = null;
        }
        if ($scope._paymentDeadlineCountdownIntervalIdForPayment) {
            try { clearInterval($scope._paymentDeadlineCountdownIntervalIdForPayment); } catch (e) { }
            $scope._paymentDeadlineCountdownIntervalIdForPayment = null;
        }
    });
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
      if (!order) return;
      var orderId = order.orderId || order.id || order.Id;
      if (orderId) {
          $scope.goToPayment(orderId);
      }
  };

    /** Buyer ORDER STATUS: use order.status from DB directly (source of truth). Rejection flag drives resubmit label. */
    $scope.getOverallStatus = function (order) {
        if (!order) return '-';
        var status = (order.status || order.Status || '').trim();
        var statusLower = status.toLowerCase().replace(/\s+/g, ' ');
        var deliveryStatus = (order.deliveryStatus || order.DeliveryStatus || '').toLowerCase();

        if (statusLower === 'cancelled') return 'Cancelled';
        if (statusLower === 'completed') return 'Completed';
        // If delivery is already in transit or delivered, surface that on the buyer side
        // even if the underlying order_status has not yet been updated from "Paid".
        if (deliveryStatus === 'in_transit') return 'In Delivery';
        if (deliveryStatus === 'delivered') return 'Delivered';
        if (statusLower === 'in delivery') return deliveryStatus === 'delivered' ? 'Delivered' : 'In Delivery';
        if (statusLower === 'rejected') return 'Order Rejected';
        // UI safety net: when deadline is past, show cancelled immediately even before background job updates DB.
        // Deadline enforcement applies only to states where payment is expected.
        if (order.paymentDeadlineExpired === true
            && (statusLower === 'approved'
                || statusLower === 'awaiting payment verification'
                || statusLower === 'layaway active'
                || statusLower === 'pending payment'
                || statusLower === 'pending downpayment')) return 'Cancelled';
        if (statusLower === 'pending approval') return 'Pending Approval';
        if (statusLower === 'awaiting payment verification') {
            // If there is a rejection reason but no SUBMITTED payment, resubmission is needed.
            if (order.latestPaymentRejectionReason && !order.hasSubmittedPayment) return 'Resubmit Payment Required';
            // Once a new payment has been submitted, show it as awaiting verification again.
            return 'Awaiting Payment Verification';
        }
        if (statusLower === 'pending payment') return 'Pending Payment';
        if (statusLower === 'pending downpayment') return 'Pending Downpayment';
        if (statusLower === 'layaway active') return 'Layaway Active';
        if (statusLower === 'paid') return 'Paid - Waiting for Shipment';
        if (statusLower === 'ready for meetup') return 'Ready for Meetup';
        if (statusLower === 'no show') return 'No Show';
        if (statusLower === 'meetup rescheduled') return 'Meetup Rescheduled';
        return 'Approved';
    };

    $scope.getOverallStatusTooltip = function (order) {
        if (!order) return '';
        var status = (order.status || order.Status || '').toLowerCase();
        if (status === 'rejected') return 'Order was rejected by admin.';
        if (status === 'pending approval') return 'Waiting for admin to approve your order.';
        if (status === 'cancelled') return 'Order permanently closed.';
        if (status === 'no show') return 'You did not attend the meetup. Admin may reschedule.';
        if (status === 'meetup rescheduled') return 'Meetup was rescheduled. See new date and time below.';
        return '';
    };
    /** Order status badge class. */
    $scope.getOrderStatusBadgeClass = function (order) {
        if (!order) return 'order-status-gray';
        var s = $scope.getOverallStatus(order);
        if (s === 'Pending Approval' || s === 'Pending Payment' || s === 'Pending Downpayment' || s === 'Approved') return 'order-status-gray';
        if (s === 'Awaiting Verification') return 'order-status-yellow';
        if (s === 'Layaway Active') return 'order-status-blue';
        if (s === 'Paid - Waiting for Shipment' || s === 'Ready for Meetup' || s === 'Meetup Rescheduled' || s === 'In Delivery' || s === 'Completed') return 'order-status-green';
        if (s === 'No Show') return 'order-status-yellow';
        if (s === 'Order Rejected' || s === 'Cancelled') return 'order-status-muted';
        return 'order-status-gray';
    };

    /** Action label: based on order.status (source of truth). Pay when Approved, Pending Payment, Pending Downpayment, Layaway Active. */
    $scope.getOrderActionLabel = function (order) {
        if (!order) return 'View Details';
        if (order.isArchived === true || order.IsArchived === true) return 'View Details';
        var displayStatus = $scope.getOverallStatus(order);
        if (displayStatus === 'Order Rejected' || displayStatus === 'Pending Approval') return 'View Details';
        if (displayStatus === 'Awaiting Payment Verification') {
            // Only allow "Pay Now" when a rejection reason exists AND no new payment is currently submitted.
            if (order.latestPaymentRejectionReason && !order.hasSubmittedPayment) return 'Pay Now';
            return 'View Details';
        }
        if (displayStatus === 'Resubmit Payment Required') return 'Pay Now';
        if (displayStatus === 'Pending Payment') return 'Pay Now';
        if (displayStatus === 'Pending Downpayment') return 'Pay Downpayment';
        if (displayStatus === 'Paid - Waiting for Shipment' || displayStatus === 'Ready for Meetup' || displayStatus === 'Meetup Rescheduled' || displayStatus === 'No Show' || displayStatus === 'In Delivery' || displayStatus === 'Completed' || displayStatus === 'Cancelled') return 'View Details';
        if (displayStatus === 'Layaway Active') return 'Pay Installment';
        if (displayStatus === 'Approved') return (order.paymentType || order.PaymentType || '').toUpperCase() === 'LAYAWAY' ? 'Pay Downpayment' : 'Pay Now';
        return 'View Details';
    };

    $scope.goToTracking = function (trackingNumberRaw, trackingLinkExplicit, courierRaw) {
        var raw = (trackingNumberRaw || '').toString().trim();
        var courier = (courierRaw || '').toString().trim();
        var courierLower = courier.toLowerCase();
        var defaultTrackingUrl = (courierLower.indexOf('fedex') !== -1) ? 'https://www.fedex.com/en-us/tracking.html' : 'https://www.lbcexpress.com/track/';
        if (!raw) {
            if (trackingLinkExplicit && /^https?:\/\//i.test(trackingLinkExplicit)) {
                window.open(trackingLinkExplicit, '_blank');
                return;
            } else {
                // No tracking number or explicit link: open carrier default as a fallback.
                window.open(defaultTrackingUrl, '_blank');
                return;
            }
        }
        var parts = raw.split('||');
        var link = parts.length > 1 ? (parts[1] || '').trim() : '';
        if (!link && trackingLinkExplicit) {
            link = trackingLinkExplicit;
        }
        if (link && /^https?:\/\//i.test(link)) {
            window.open(link, '_blank');
        } else {
            // When there is no valid URL stored, fall back to carrier tracking page.
            window.open(defaultTrackingUrl, '_blank');
        }
    };

    $scope.copyTrackingNumber = function (trackingNumber) {
        var raw = (trackingNumber || '').toString().trim();
        var value = raw.split('||')[0] || raw;
        if (!value) {
            Swal.fire({ icon: 'info', title: 'Tracking', text: 'Tracking number is not yet available. It will appear when the order is in transit.' });
            return;
        }
        if (navigator && navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(value).then(function () {
                Swal.fire({ icon: 'success', title: 'Copied', text: 'Tracking number copied to clipboard.' });
            }).catch(function () {
                Swal.fire({ icon: 'info', title: 'Tracking', text: 'Unable to copy automatically. You can copy the tracking number manually.' });
            });
        } else {
            Swal.fire({ icon: 'info', title: 'Tracking', text: 'Your browser does not allow automatic copy. You can copy the tracking number manually.' });
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
        if (label === 'Pay Now' || label === 'Pay Installment' || label === 'Pay Downpayment') {
            $scope.goToPayment(order.orderId);
        } else if (label === 'Track Now') {
            $scope.goToTracking(order.trackingNumber, order.trackingLink, order.courier);
        } else {
            // Always show the full Order Summary first to avoid confusion.
            // Payment proof can then be viewed from the Order Summary footer.
            $scope.openOrderDetailsModal(order);
        }
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

    $scope._paymentDeadlineCountdownIntervalIdForPayment = null;
    $scope.initPaymentDeadlineCountdownsForPaymentPage = function () {
        if ($scope._paymentDeadlineCountdownIntervalIdForPayment) {
            try { clearInterval($scope._paymentDeadlineCountdownIntervalIdForPayment); } catch (e) { }
            $scope._paymentDeadlineCountdownIntervalIdForPayment = null;
        }
        if (!$scope.paymentOrder || !$scope.paymentOrder.paymentDeadlineUtcIso) return;

        function pad2(n) { return (n < 10 ? '0' : '') + n; }
        function formatRemaining(diffMs) {
            var totalSeconds = Math.max(0, Math.floor(diffMs / 1000));
            var days = Math.floor(totalSeconds / 86400);
            totalSeconds = totalSeconds % 86400;
            var hours = Math.floor(totalSeconds / 3600);
            totalSeconds = totalSeconds % 3600;
            var minutes = Math.floor(totalSeconds / 60);
            var seconds = totalSeconds % 60;
            return (days > 0 ? (days + 'd ') : '') + pad2(hours) + ':' + pad2(minutes) + ':' + pad2(seconds);
        }

        var tick = function () {
            if (!$scope.paymentOrder || !$scope.paymentOrder.paymentDeadlineUtcIso) return;
            var dl = new Date($scope.paymentOrder.paymentDeadlineUtcIso);
            if (isNaN(dl.getTime())) return;
            var diff = dl.getTime() - Date.now();
            if (diff <= 0) {
                if ($scope.paymentOrder.paymentDeadlineRemainingText !== 'Expired') {
                    $scope.paymentOrder.paymentDeadlineRemainingText = 'Expired';
                    $scope.paymentOrder.paymentDeadlineExpired = true;
                    $scope.$applyAsync();
                }
            } else {
                var newText = formatRemaining(diff);
                if ($scope.paymentOrder.paymentDeadlineRemainingText !== newText) {
                    $scope.paymentOrder.paymentDeadlineRemainingText = newText;
                    $scope.paymentOrder.paymentDeadlineExpired = false;
                    $scope.$applyAsync();
                }
            }
        };

        tick();
        $scope._paymentDeadlineCountdownIntervalIdForPayment = setInterval(tick, 1000);
    };

    // Payment page: use only API values. Never compute remainingBalance, progressPercent, amountDue, installmentsRemaining locally.
    $scope.getLayawayPaidPercent = function () {
        if (!$scope.paymentOrder || $scope.paymentOrder.paymentType !== 'LAYAWAY') return 0;
        if ($scope.paymentOrder.progressPercent != null && !isNaN($scope.paymentOrder.progressPercent)) return Number($scope.paymentOrder.progressPercent);
        return 0;
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
                $scope.initPaymentDeadlineCountdownsForPaymentPage();
                $scope.paymentMode = ($scope.paymentOrder.paymentType === 'LAYAWAY') ? 'Layaway' : 'Full Payment';
                if ($scope.paymentOrder.paymentType === 'LAYAWAY') {
                    if ($scope.paymentFormData.channel === 'Cash through Meetup' || !$scope.paymentFormData.channel) {
                        $scope.paymentFormData.channel = 'GCash';
                    }
                }
                // For resubmit scenario, fix channel to the last rejected payment's channel if available.
                var latest = $scope.paymentOrder.latestPaymentForProof || $scope.paymentOrder.LatestPaymentForProof;
                if (latest && (latest.paymentStatus || latest.PaymentStatus) === 'REJECTED' && latest.allowResubmit) {
                    var ch = latest.paymentChannel || latest.PaymentChannel || '';
                    if (ch) {
                        $scope.paymentFormData.channel = ch;
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
                $scope.initPaymentDeadlineCountdownsForPaymentPage();
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
            $scope.paymentValidation.file = 'Must submit JPG or PNG only. Maximum size 5MB.';
            Swal.fire({ icon: 'warning', title: 'Invalid file', text: 'Must submit JPG or PNG only. Maximum size 5MB.' });
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
                Swal.fire({ icon: 'success', title: 'Uploaded', text: 'Proof uploaded. Enter reference number (letters and numbers only, max 20) and click PAY.', timer: 2500, showConfirmButton: false });
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

    /** Show pay form when order is APPROVED, Layaway Active, Pending Payment, or when a payment was REJECTED with resubmit allowed. */
    $scope.canShowPayForm = function () {
        if (!$scope.paymentOrder) return false;
        var status = ($scope.paymentOrder.order_status != null ? $scope.paymentOrder.order_status : $scope.paymentOrder.orderStatus) || '';
        var statusU = (status || '').toUpperCase().replace(/\s+/g, ' ');
        if ($scope.paymentOrder.paymentDeadlineExpired === true) return false;
        var remaining = $scope.paymentOrder.remainingBalance != null ? Number($scope.paymentOrder.remainingBalance) : null;
        var latest = $scope.paymentOrder.latestPaymentForProof || $scope.paymentOrder.LatestPaymentForProof;
        var paymentRejectedCanResubmit = latest && (latest.paymentStatus || latest.PaymentStatus) === 'REJECTED' && latest.allowResubmit;

        if (statusU === 'PENDING APPROVAL') return false;
        if (statusU === 'REJECTED') return false;
        if (remaining != null && remaining <= 0) return false;
        // When last payment was rejected and resubmission is allowed, always show the form,
        // even if a previous payment existed or the order status is Awaiting Payment Verification.
        if (paymentRejectedCanResubmit) return true;
        if ($scope.paymentOrder.hasSubmittedPayment) return false;
        if (statusU === 'AWAITING PAYMENT VERIFICATION' || statusU === 'AWAITING MEETUP') return false;
        if (statusU === 'LAYAWAY ACTIVE') return true;
        if ((statusU === 'PENDING PAYMENT' || statusU === 'PENDING DOWNPAYMENT') && paymentRejectedCanResubmit) return true;
        if (statusU !== 'APPROVED') return false;
        if (latest && (latest.paymentStatus || latest.PaymentStatus) === 'REJECTED' && !latest.allowResubmit) return false;
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

    var PAYMENT_REFERENCE_ALNUM_REGEX = /^[A-Za-z0-9]+$/;
    /** True while the field contains any character outside A–Z / a–z / 0–9 (allows empty = no error). */
    $scope.paymentReferenceFormatError = function () {
        var s = ($scope.paymentForm && $scope.paymentForm.referenceNumber != null) ? String($scope.paymentForm.referenceNumber) : '';
        if (!s.length) return false;
        return !/^[A-Za-z0-9]*$/.test(s);
    };
    function isPaymentReferenceValidForSubmit(ref) {
        var t = (ref || '').trim();
        return t.length >= 1 && t.length <= 20 && PAYMENT_REFERENCE_ALNUM_REGEX.test(t);
    }

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
        if (!isPaymentReferenceValidForSubmit(ref)) return false;
        return true;
    };

    $scope.isScheduleButtonEnabled = function () {
        if ($scope.submitting) return false;
        if (!$scope.isMeetupChannel()) return false;
        var data = $scope.paymentFormData;
        if (!data || !data.meetupDate || !data.meetupTime) return false;
        if (!$scope.hasValidProof()) return false;
        var ref = (($scope.paymentForm && $scope.paymentForm.referenceNumber) || '').trim();
        if (!isPaymentReferenceValidForSubmit(ref)) return false;
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
                Swal.fire({ icon: 'warning', title: 'Reference number required', text: 'Enter your payment reference (letters and numbers only, max 20 characters).' });
                return;
            }
            if (ref.length > 20) {
                $scope.paymentValidation.reference = 'Reference number must be 20 characters or less.';
                $scope.paymentError = $scope.paymentValidation.reference;
                Swal.fire({ icon: 'warning', title: 'Too long', text: 'Reference number must be 20 characters or less.' });
                return;
            }
            if (!PAYMENT_REFERENCE_ALNUM_REGEX.test(ref)) {
                $scope.paymentValidation.reference = 'Reference number may only contain letters and numbers.';
                $scope.paymentError = $scope.paymentValidation.reference;
                Swal.fire({ icon: 'warning', title: 'Invalid reference number', text: 'Use only letters and numbers (no spaces or special characters).' });
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
                Swal.fire({ icon: 'warning', title: 'Reference required', text: 'Enter your payment reference (letters and numbers only, max 20 characters).' });
                return;
            }
            if (refMeetup.length > 20) {
                $scope.paymentValidation.reference = 'Reference number must be 20 characters or less.';
                $scope.paymentError = $scope.paymentValidation.reference;
                Swal.fire({ icon: 'warning', title: 'Too long', text: 'Reference number must be 20 characters or less.' });
                return;
            }
            if (!PAYMENT_REFERENCE_ALNUM_REGEX.test(refMeetup)) {
                $scope.paymentValidation.reference = 'Reference number may only contain letters and numbers.';
                $scope.paymentError = $scope.paymentValidation.reference;
                Swal.fire({ icon: 'warning', title: 'Invalid reference number', text: 'Use only letters and numbers (no spaces or special characters).' });
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
        var channel = ($scope.paymentFormData && $scope.paymentFormData.channel) ? $scope.paymentFormData.channel : (isMeetup ? 'Cash through Meetup' : '');
        SolteiraBrandsService.submitPayment($scope.paymentOrder.orderId, amountToSubmit, proofId, refNum, mDate, mTime, channel).then(function (r) {
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
        if (name.length > CHECKOUT_NAME_MAX_LEN) { Swal.fire({ icon: 'warning', title: 'Name too long', text: 'Name must be ' + CHECKOUT_NAME_MAX_LEN + ' characters or fewer.' }); return; }
        if (!contact) { Swal.fire({ icon: 'warning', title: 'Required', text: 'Enter your contact number.' }); return; }
        if (!address) { Swal.fire({ icon: 'warning', title: 'Required', text: 'Enter your delivery address.' }); return; }
        if (address.length > CHECKOUT_ADDRESS_MAX_LEN) { Swal.fire({ icon: 'warning', title: 'Address too long', text: 'Delivery address must be ' + CHECKOUT_ADDRESS_MAX_LEN + ' characters or fewer.' }); return; }
        if (!payment) { Swal.fire({ icon: 'warning', title: 'Required', text: 'Select a payment option.' }); return; }
        // Terms checkbox: rely on actual DOM state (ng-model is unreliable on this checkout view).
        if (!$scope.isCheckoutTermsAccepted()) { Swal.fire({ icon: 'warning', title: 'Terms required', text: 'Please acknowledge the payment Terms & Conditions before placing the order.' }); return; }

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
                // Remove ordered items from the server-side cart.
                var productIdsToRemove = selected.map(function (i) { return i.id; });
                try { sessionStorage.removeItem(getSelectedCartSessionKey()); } catch (e) { }

                Promise.all(productIdsToRemove.map(function (pid) {
                    return SolteiraBrandsService.removeCartItem(pid).catch(function () { });
                })).then(function () {
                    $scope.refreshCartFromServer(false).then(function () {
                        Swal.fire({
                            title: 'Order Placed!',
                            text: 'Order ' + (r.data.orderNumber || '') + ' has been submitted. You will receive confirmation via email.',
                            icon: 'success',
                            confirmButtonText: 'OK'
                        }).then(function () {
                            window.location.href = '/SolteiraAndBrands/SolteiraBrandsMyOrders';
                        });
                    });
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
