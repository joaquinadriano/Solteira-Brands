app.controller("SolteiraBrandsController", function ($scope, SolteiraBrandsService, $timeout, $interval) {

    console.log("Controller Loaded");

    $scope.viewMode = 'list';
    $scope.qty = 1;
    //$scope.transitionDir = 'forward';

    $scope.jewelries = [
        {
            id: 1, name: "Many Shape Tennis Bracelet", price: 55790, category: "Bracelet",
            material: "18K White Gold", weight: "9.6 grams", length: "18cm", stones: "5 Total Carat Diamond",
            img: "many-shape-bracelet.png"
        },
        {
            id: 2, name: "Alexandra Set", price: 48450, category: "Bracelet",
            material: "18K White Gold", weight: "5.1 grams", length: "", stones: "2 Total Carat Diamond",
            img: "alexandra-set.png"
        },
        {
            id: 3, name: "Tiara Ring - White Gold", price: 34478, category: "Ring",
            material: "18K White Gold", weight: "4.5 grams", length: "", stones: "1 Total Carat Diamond",
            img: "tiara-ring-whitegold.png"
        },
        {
            id: 4, name: "Water Dangling Earrings", price: 14378, category: "Earring",
            material: "18K Yellow Gold", weight: "0.3 grams", length: "", stones: "0.250 Total Carat Diamond",
            img: "water-dangling-earrings.png"
        },
        {
            id: 5, name: "Heart Citrine Diamond Ring", price: 38750, category: "Ring",
            material: "18K White Gold", weight: "2.1 grams", length: "", stones: ["0.24 Total Carat Diamond", "1.5 Total Carat Citrine"],
            img: "citrine-diamond-ring.png"
        },
        {
            id: 6, name: "Sapphire Bracelet", price: 7611, category: "Bracelet",
            material: "18K White Gold", weight: "0.88 grams", length: "", stones: "0.61 Total Carat Sapphire",
            img: "sapphire-bracelet.png"
        },
        {
            id: 7, name: "Big Square Ring", price: 27509, category: "Ring",
            material: "18K Yellow Gold", weight: "4.3 grams", length: "", stones: "1 Total Carat Diamond",
            img: "big-square-ring.png"
        },
        {
            id: 8, name: "Tiara Ring - Yellow Gold", price: 34478, category: "Ring",
            material: "18K Yellow Gold", weight: "4.5 grams", length: "", stones: "1 Total Carat Diamond",
            img: "tiara-ring-yellow-gold.png"
        },
        {
            id: 9, name: "1 Carat Station Necklace", price: 28482, category: "Necklace",
            material: "18K White Gold", weight: "", length: "", stones: "1 Total Carat Diamond",
            img: "carat-necklace.png"
        }
    ];

    $scope.isEnumerable = function (item) {
        return angular.isArray(item);
    };

    $scope.goBack = function () {
        $scope.transitionDir = 'backward'; // Set direction for "Previous" page
        $scope.viewMode = 'list';
        $scope.selectedProduct = null;
    };

    $scope.selectProduct = function (item, event) {
        // Prevent the Vanilla JS 'window.onclick' from causing issues
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
            // Use $timeout to ensure the view update happens AFTER the Swal closes
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

        // 1. Get products in the same category (excluding current)
        let related = $scope.jewelries.filter(j =>
            j.category === $scope.selectedProduct.category && j.id !== $scope.selectedProduct.id
        );

        // 2. If we have less than 4, fill the gap with items from other categories
        if (related.length < 4) {
            let others = $scope.jewelries.filter(j =>
                j.category !== $scope.selectedProduct.category && j.id !== $scope.selectedProduct.id
            );
            // Combine and take the remainder needed
            related = related.concat(others).slice(0, 4);
        }

        return related.slice(0, 4);
    };

    // default mode
    $scope.currentMode = 'registration';

    $scope.init = function () {
        const urlParams = new URLSearchParams(window.location.search);
        const productId = parseInt(urlParams.get('id'));

        if (productId) {
            const product = $scope.jewelries.find(j => j.id === productId);
            if (product) {
                // This is the key: set the mode to 'detail' as soon as the page loads
                $scope.selectedProduct = product;
                $scope.viewMode = 'detail';
            }
        }

        const mode = urlParams.get('mode');
        if (mode) {
            $scope.currentMode = mode;
        }
    };

    // Don't forget to call it!
    $scope.init();

    // REDIRECTION: Logic for the Home Page clicks
    $scope.goToProduct = function (item) {
        // This matches the href structure in your layout
        window.location.href = '/SolteiraBrands/SolteiraBrandsJewelries?id=' + item.id;
    };

    $scope.showPassword = false;
    $scope.showVerifyPassword = false;

    $scope.loginData = {
        username: '',
        password: ''
    };

    $scope.registerData = {
        email: '',
        username: '',
        password: '',
        verifyPassword: ''
    };

    $scope.login = function () {

        // alert for testing
        Swal.fire({
        title: "Welcome back!",
            icon: "success",
            customClass: {
                title: 'my-swal-title',
                htmlContainer: 'my-swal-text',
                confirmButton: 'my-swal-button'
            }  
        });

        // dialog box for login

        //if ($scope.loginForm.$valid) {
        //    SolteiraBrandsService.Login($scope.loginData).then(function (response) {
        //        // Success Callback
        //        if (response.data.success) {
        //            Swal.fire({
        //                title: "Welcome back!",
        //                text: "Elegance awaits you.",
        //                icon: "success",
        //                iconColor: #87AE73,
        //                customClass: {
        //                title: 'my-swal-title',
        //                  htmlContainer: 'my-swal-text',
        //                  confirmButton: 'my-swal-button'
        //                }  
        //            });
        //            // Optional: window.location.href = '/Home/Index';
        //        } else {
        //            Swal.fire({
        //                title: "Invalid credentials.",
        //                icon: "error"
        //            });
        //        }
        //    }, function (error) {
        //        // Error Callback (Notice the comma above and correct placement)
        //        Swal.fire({
        //            title: "Server Error",
        //            text: "Unable to connect to the service.",
        //            icon: "error"
        //        });
        //    });
        //}
    };

    // new password
    $scope.togglePassword = function () {
        $scope.showPassword = !$scope.showPassword;
    };

    $scope.toggleVerifyPassword = function () {
        $scope.showVerifyPassword = !$scope.showVerifyPassword;
    };

    // --- Mock Timer Logic ---
    $scope.timeLeft = 30;
    $scope.timerVisible = true;
    let timerInterval;

    $scope.startTimer = function () {
        // Reset values
        $scope.timeLeft = 30;
        $scope.timerVisible = true;

        // Cancel any existing interval to avoid speed-up
        if (angular.isDefined(timerInterval)) {
            $interval.cancel(timerInterval);
        }

        // Start the countdown
        timerInterval = $interval(function () {
            if ($scope.timeLeft > 0) {
                $scope.timeLeft--;
            } else {
                $scope.timerVisible = false;
                $interval.cancel(timerInterval);
            }
        }, 1000);
    };

    // Initialize the timer on page load
    $scope.startTimer();

    // --- Mock Resend Logic ---
    $scope.resendOtp = function () {
        // Show a "Success" toast immediately to simulate the email being sent
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'success',
            title: 'New OTP sent to email!',
            showConfirmButton: false,
            timer: 2500,
            timerProgressBar: true
        });

        // Restart the frontend timer
        $scope.startTimer();
    };

    // Ensure timer is cancelled if the controller is destroyed
    $scope.$on('$destroy', function () {
        if (angular.isDefined(timerInterval)) {
            $interval.cancel(timerInterval);
        }
    });

    // forgot password otp
    $scope.sendOTP = function () {
        window.location.href = '/SolteiraBrandsAccount/SolteiraBrandsOTP?mode=forgot&email=' + $scope.userEmail;
    };

    // This function will be called by the OTP submit logic
    $scope.handleOTPSuccess = function () {
        if ($scope.currentMode === 'forgot') {
            window.location.href = '/SolteiraBrandsAccount/NewPassword';
        } else {
            window.location.href = '/SolteiraBrandsAccount/SolteiraBrandsLogin';
        }
    };

    // add-to-cart logic
    $scope.cart = JSON.parse(localStorage.getItem('sb_cart')) || [];

    $scope.addToCart = function (product, quantity) {
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

    // Function to remove items
    $scope.removeFromCart = function (index) {
        $scope.cart.splice(index, 1);
        localStorage.setItem('sb_cart', JSON.stringify($scope.cart));
    };

    // total price 
    $scope.getCartTotal = function () {
        if (!$scope.cart) return 0;

        return $scope.cart.reduce(function (total, item) {
            // Only add to total if the checkbox is checked
            if (item.selected) {
                return total + (item.price * item.qty);
            }
            return total;
        }, 0);
    };

    // toggle all boxes at once
    $scope.selectAll = true;
    $scope.toggleAll = function () {
        angular.forEach($scope.cart, function (item) {
            item.selected = $scope.selectAll;
        });
    };

    // checkout button logic from cart page
    $scope.proceedToCheckout = function (url) {
        // You can add logic here to save the selected items to a session/database first
        window.location.href = url;
    };

    // gets items that are selected for checkout
    $scope.getSelectedItems = function () {
        return $scope.cart.filter(item => item.selected === true);
    };

    // calculates subtotal
    $scope.getCheckoutSubtotal = function () {
        let selected = $scope.getSelectedItems();
        return selected.reduce((total, item) => total + (item.price * item.qty), 0);
    };

    $scope.orders = JSON.parse(localStorage.getItem('sb_orders')) || [];

    $scope.placeOrder = function () {
        Swal.fire({
            title: 'Processing Order...',
            text: 'Please wait while we verify your transaction.',
            allowOutsideClick: false,
            didOpen: () => { Swal.showLoading(); }
        });

        $timeout(() => {

            const selectedItems = $scope.getSelectedItems();

            const newOrder = {
                orderId: 'SB-' + Math.floor(Math.random() * 900000 + 100000),
                date: new Date().toLocaleDateString(),
                items: angular.copy(selectedItems),
                total: $scope.getCheckoutSubtotal(),

                customerName: $scope.customerName || "Guest",
                contactNumber: $scope.contactNumber || "N/A",
                deliveryAddress: $scope.deliveryAddress || "N/A",
                paymentMethod: $scope.paymentMethod,
                subPaymentMethod: $scope.subPayment,

                meetupDate: $scope.meetupDate || null,
                meetupTime: $scope.meetupTime || null,
                meetupLocation: "I-V G/F JMS Bldg., Mc Arthur Highway, Salapungan, Angeles City, Pampanga",

                downPayment: subtotal * 0.30,
                remainingBalance: subtotal * 0.70,
                monthlyBill: (subtotal * 0.70) / 6,
                percentPaid: 30,

                status: 'Approved',
                deliveryStatus: 'In Transit',
                trackingNumber: 'TRK' + Math.floor(Math.random() * 1000000000),
                courier: 'LBC'

            };

            let allOrders = JSON.parse(localStorage.getItem('sb_orders')) || [];
            allOrders.unshift(newOrder);
            localStorage.setItem('sb_orders', JSON.stringify(allOrders));

            $scope.cart = $scope.cart.filter(item => !item.selected);
            localStorage.setItem('sb_cart', JSON.stringify($scope.cart));

            Swal.fire({
                html: `
                <div class="custom-swal-content">
                    <div class="swal-icon-stack">
                        <i class="bi bi-envelope-open" style="font-size: 5rem;"></i>
                        <i class="bi bi-heart-fill heart-overlay"></i>
                    </div>
                    <h2 class="swal-custom-title">Thank you for your order!</h2>
                    <p class="swal-custom-text">Order ID: <strong>${newOrder.orderId}</strong></p>
                    <p class="swal-custom-text">You will receive an email once approved.</p>
                </div>
            `,
                showConfirmButton: true,
                confirmButtonText: 'CLOSE',
                background: '#87AE73',
                color: '#FFFBE8',
                width: '25vw',
                padding: '3rem',
                customClass: {
                    confirmButton: 'swal-white-button',
                    popup: 'swal-no-border'
                },
                buttonsStyling: false,
                allowOutsideClick: false
            }).then(() => {
                window.location.href = '/SolteiraBrands/SolteiraBrandsMyOrders';
            });
        }, 2000);
    };

    // Function to redirect to details page
    $scope.viewOrderDetails = function (orderId) {
        window.location.href = '/SolteiraBrands/SolteiraBrandsOrderDetails?orderId=' + orderId;
    };

    // Function for the Order Details Page to load specific data
    $scope.initOrderDetails = function () {
        const urlParams = new URLSearchParams(window.location.search);
        const orderId = urlParams.get('orderId');

        if (orderId) {
            const allOrders = JSON.parse(localStorage.getItem('sb_orders')) || [];

            $scope.currentOrder = allOrders.find(o => o.orderId === orderId);

            if ($scope.currentOrder && !$scope.currentOrder.subPaymentMethod) {
                $scope.currentOrder.subPaymentMethod = $scope.currentOrder.paymentMethod;
            }
        }
    };

    $scope.goToOrders = function () {
        window.location.href = '/SolteiraBrands/SolteiraBrandsMyOrders';
    };

    // payment
    $scope.initPaymentPage = function () {
        let allOrders = JSON.parse(localStorage.getItem('sb_orders')) || [];
        if (allOrders.length > 0) {
            // Get the most recent order (the one just placed)
            let latestOrder = allOrders[0];

            $scope.customerName = latestOrder.customerName;
            $scope.contactNumber = latestOrder.contactNumber;
            $scope.deliveryAddress = latestOrder.deliveryAddress;
            $scope.paymentMethod = latestOrder.paymentMethod;
        }
    };

    $scope.initPaymentPage();

    $scope.toggleSubPayment = function (value) {
        if ($scope.subPayment === value) {
            $scope.subPayment = ''; // Deselect if already selected
        } else {
            $scope.subPayment = value; // Select new value
        }
    };

    // cash through meetup
    $scope.selectedDay = 19; 
    $scope.timeOptions = [];
    $scope.calendarDays = [];
    $scope.currentDate = new Date();
    $scope.months = ["January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"];

    $scope.generateCalendar = function () {
        $scope.calendarDays = [];

        let year = $scope.currentDate.getFullYear();
        let month = $scope.currentDate.getMonth();

        let firstDay = new Date(year, month, 1).getDay();
        let startOffset = (firstDay === 0) ? 6 : firstDay - 1;
        let daysInMonth = new Date(year, month + 1, 0).getDate();

        for (let i = 0; i < startOffset; i++) {
            $scope.calendarDays.push({ val: '', isMuted: true });
        }

        for (let d = 1; d <= daysInMonth; d++) {
            $scope.calendarDays.push({
                val: d < 10 ? '0' + d : d.toString(),
                isMuted: false
            });
        }
    };

    $scope.changeMonth = function (delta) {
        $scope.currentDate.setMonth($scope.currentDate.getMonth() + delta);
        $scope.generateCalendar();
    };

    $scope.selectDay = function (dayObj) {
        if (!dayObj.isMuted && dayObj.val !== '') {
            $scope.selectedDay = parseInt(dayObj.val);
            $scope.meetupDate = $scope.months[$scope.currentDate.getMonth()] + " " + dayObj.val + ", " + $scope.currentDate.getFullYear();
        }
    };

    $scope.generateCalendar();
    function generateTimeOptions() {
        for (let i = 9; i <= 17; i++) {
            let hour = i % 12 || 12;
            let ampm = i < 12 ? 'AM' : 'PM';

            $scope.timeOptions.push(hour + ':00 ' + ampm);

            if (i < 17) {
                $scope.timeOptions.push(hour + ':30 ' + ampm);
            }
        }
    }

    function generateCalendar() {
        for (let m = 1; m <= 7; m++) {
            $scope.calendarDays.push({ val: '0' + m, isMuted: true });
        }
        for (let d = 8; d <= 31; d++) {
            $scope.calendarDays.push({ val: d < 10 ? '0' + d : d.toString(), isMuted: false });
        }
    }

    $scope.selectDay = function (dayObj) {
        if (!dayObj.isMuted) {
            $scope.selectedDay = parseInt(dayObj.val);
            $scope.meetupDate = "2026-11-" + dayObj.val;
        }
    };

    generateTimeOptions();
    generateCalendar();

    // payment verification
    $scope.submitPayment = function () {

        if (!$scope.subPayment) {
            Swal.fire({
                icon: 'warning',
                title: 'Missing Information',
                text: 'Please select a payment method first.',
                confirmButtonColor: '#87AE73'
            });
            return;
        }

        Swal.fire({
            html: `
            <div style="padding: 20px;">
                <div style="position: relative; display: inline-block; margin-bottom: 20px;">
                    <i class="bi bi-envelope" style="font-size: 5rem; color: white;"></i>
                    <i class="bi bi-heart-fill" style="position: absolute; bottom: 5px; right: -5px; font-size: 2rem; color: #87AE73; -webkit-text-stroke: 2px white;"></i>
                </div>
                <h2 style="font-family: 'Kaisei Tokumin'; color: white; font-weight: bold; margin-bottom: 15px;">
                    Thank you for your payment!
                </h2>
                <p style="color: white; font-size: 1.1rem; opacity: 0.9;">
                    You will receive an email once your order has been verified.
                </p>
            </div>
        `,
            showConfirmButton: true,
            confirmButtonText: 'CLOSE',
            background: '#87AE73', 
            width: '25vw',
            customClass: {
                confirmButton: 'custom-swal-close-btn' 
            },
            buttonsStyling: false,
            allowOutsideClick: false
        }).then((result) => {
            if (result.isConfirmed) {
                window.location.href = '/SolteiraBrands/SolteiraBrandsMyOrders';
            }
        });
    };

    // proof of payment image
    $scope.viewProof = function (order) {
        
        const proofUrl = order.proofImage || '/Content/SolteiraBrandsImages/SBJewelryImages/' + order.items[0].img;

        Swal.fire({
            title: 'Proof of Payment',
            html: `
            <div class="swal-proof-container">
                <div class="swal-proof-image-wrapper">
                    <img src="${proofUrl}" alt="Proof of Payment" class="swal-proof-img" />
                </div>
                <div class="swal-proof-details">
                    <p><strong>Date:</strong> ${order.date}</p>
                    <p><strong>Reference Number:</strong> <span class="text-green">${order.referenceNumber || 'N/A'}</span></p>
                </div>
            </div>
        `,
            confirmButtonText: 'CLOSE',
            confirmButtonColor: '#87AE73',
            background: '#ffffff',
            width: '450px',
            customClass: {
                title: 'swal-custom-title',
                confirmButton: 'swal-white-button'
            }
        });
    };

})