app.controller("SolteiraAndBrandsController", function ($scope, $timeout, SolteiraAndBrandsService) {




    //----------Navigation----------

    //to dashboard
    $scope.inventoryDashboard = function () {
        window.location.href = "/SolteiraAndBrands/AdminDashboardPage";
    }

    //to inventory page
    $scope.inventoryInventory = function () {
        window.location.href = "/SolteiraAndBrands/AdminInventoryPage";
    }

    //to inventory orders page
    $scope.inventoryOrders = function () {
        window.location.href = "/SolteiraAndBrands/AdminOrdersPage";
    }

    //to inventory payment page
    $scope.inventoryPayment = function () {
        window.location.href = "/SolteiraAndBrands/AdminPaymentPage";
    }

    //to delivery shipment page
    $scope.deliveryShipment = function () {
        window.location.href = "/SolteiraAndBrands/AdminDeliveryShipmentsPage";
    }
    //to delivery cash through meetup page
    $scope.deliveryMeetup = function () {
        window.location.href = "/SolteiraAndBrands/AdminDeliveryMeetupPage";
    }
    //to orders archive
    $scope.orderArchive = function () {
        window.location.href = "/SolteiraAndBrands/AdminOrdersArchivePage";
    }

    //to reports page
    $scope.inventoryReports = function () {
        window.location.href = "/SolteiraAndBrands/AdminReportsPage";
    }

    //to logs page
    $scope.inventoryLogs = function () {
        window.location.href = "/SolteiraAndBrands/AdminLogsPage";
    }

    //to users page
    $scope.inventoryUsers = function () {
        window.location.href = "/SolteiraAndBrands/AdminUsersPage";
    }



    //----------Navigation end----------


    //---------Sweet Alert Dialogue Box--------

    //for when logout is clicked
    $scope.logoutAlert = function () {
        Swal.fire({
            title: 'Logout Confirmation',
            text: 'Please confirm you wish to log out.',
            icon: 'info',
            iconColor: '#8eb078', 
            showCancelButton: true,
            confirmButtonText: 'Log out',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#8eb078',
            background: '#FFFBE8', 
            }).then((result) => {
            if (result.isConfirmed) {

                //code for sessionStorage.clear?? or kayo na bahala
                //redirect to login page
                window.location.href = "/SolteiraAndBrands/AdminLoginPage";
            } 
        });
    }

    //for when status is changed in the ORDERS Page
    $scope.statusChange = function () {
        Swal.fire({
            title: 'Order status changed!',
            icon: 'success',
            iconColor: '#FFFBE8', 
            background: '#8eb078', 
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8', 
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        });
    }

    //for when an item is added in the INVENTORY Page
    $scope.inventoryAddStatus = function () {
        Swal.fire({
            title: 'Inventory item added!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        }).then(() => {
            $('#inventoryAddItemModal').modal('hide');
        });
    }

    //for when an item is edited in the INVENTORY Page
    $scope.inventoryEditStatus = function () {
        Swal.fire({
            title: 'Inventory item updated!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        }).then(() => {
            $('#inventoryEditItemModal').modal('hide');
        });
    }

    //for when status is changed in the DELIVERY SHIPMENTS Page
    $scope.shipmentsStatus = function () {
        Swal.fire({
            title: 'Delivery status changed!',
            icon: 'success',
            iconColor: '#FFFBE8', 
            background: '#8eb078', 
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8', 
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        });
    }

    //for when status is changed in the DELIVERY MEETUP Page
    $scope.meetupStatus = function () {
        Swal.fire({
            title: 'Delivery status changed!',
            icon: 'success',
            iconColor: '#FFFBE8', 
            background: '#8eb078', 
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8', 
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        });
    }

    //for when an order is added in the DELIVERY 
    $scope.addShipmentStatus = function () {
        Swal.fire({
            title: 'Delivery added successfully!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        }).then(() => {
            $('#deliveryAddShipmentModal').modal('hide');
        });
    };
    //for when a delivery is updated in the DELIVERY SHIPMENT
    $scope.updateShipmentStatus = function () {
        Swal.fire({
            title: 'Delivery updated successfully!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        }).then(() => {
            $('#deliveryEditShipmentModal').modal('hide');
        });
    };

    //for when an order is added in the DELIVERY meetup
    $scope.addMeetupStatus = function () {
        Swal.fire({
            title: 'Delivery added successfully!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        }).then(() => {
            $('#deliveryAddMeetupModal').modal('hide');

        });
    };
    //for when a delivery is updated in the DELIVERY CASH THOUGH MEETUP
    $scope.updateMeetupStatus = function () {
        Swal.fire({
            title: 'Meetup details successfully updated!!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        }).then(() => {
            $('#deliveryEditMeetupModal').modal('hide');
        });
    };

    //for when a file is uploaded as POF in DELIVERY meetup
    $scope.uploadPOFStatus = function () {
        Swal.fire({
            title: 'Proof of Transaction Uploaded!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        }).then(() => {
            $('#uploadFileModal').modal('hide');
        });
    };


    //for when restore is clicked
    $scope.restoreAlert = function () {
        Swal.fire({
            title: 'Restore Confirmation',
            text: 'Please confirm you wish to restore this order',
            icon: 'info',
            iconColor: '#8eb078',
            showCancelButton: true,
            confirmButtonText: 'Restore',
            confirmButtonColor: '#8eb078',
            cancelButtonText: 'Cancel',
            background: '#FFFBE8',
        }).then((result) => {
            if (result.isConfirmed) {
                $scope.orderRestoreAlert();
            }
        });
    };

    //for when an order is restored in archive - order restored alert
    $scope.orderRestoreAlert = function () {
        Swal.fire({
            title: 'Order restored successfully!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        });
    }

    //for when archvie is clicked in ORDERS Page
    $scope.archiveOrder = function () {
        Swal.fire({
            title: 'Archive Confirmation',
            text: 'Please confirm you wish to archive this order',
            icon: 'info',
            iconColor: '#8eb078',
            showCancelButton: true,
            confirmButtonText: 'Archive',
            confirmButtonColor: '#8eb078',
            cancelButtonText: 'Cancel',
            background: '#FFFBE8',
        }).then((result) => {
            if (result.isConfirmed) {
                $scope.archiveOrderAlert();
            }
        });
    };

    //for when an order is archived in ORDERS Page - order archive alert
    $scope.archiveOrderAlert = function () {
        Swal.fire({
            title: 'Order archived successfully!',
            icon: 'success',
            iconColor: '#FFFBE8',
            background: '#8eb078',
            confirmButtonText: 'OK',
            confirmButtonColor: '#FFFBE8',
            buttonsStyling: true,
            customClass: {
                title: 'status-title',
                confirmButton: 'status-button'
            }
        });
    }

    //for when archive is clicked in INVENTORY Page
    $scope.inventoryArchiveAlert = function () {
        Swal.fire({
            title: 'Archive Confirmation',
            text: 'Please confirm you wish to archive this inventory item',
            icon: 'info',
            iconColor: '#8eb078',
            showCancelButton: true,
            confirmButtonText: 'Archive',
            confirmButtonColor: '#8eb078',
            cancelButtonText: 'Cancel',
            background: '#FFFBE8',
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: 'Inventory item archived successfully!',
                    icon: 'success',
                    iconColor: '#FFFBE8',
                    background: '#8eb078',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#FFFBE8',
                    buttonsStyling: true,
                    customClass: {
                        title: 'status-title',
                        confirmButton: 'status-button'
                    }
                });
            }
        });
    }
    //---------Sweet Alert Dialogue Box end---------

    //----------Charts--------- 
    //Temporary Datas *Please modify/remove once the database is implemented
    $scope.labels = ["January", "February", "March", "April", "May", "June", "July"];
    $scope.series = ['Series A', 'Series B'];
    $scope.data = [
        [65, 59, 80, 81, 56, 55, 40],
        [28, 48, 40, 19, 86, 27, 90]
    ];
    $scope.onClick = function (points, evt) {
        console.log(points, evt);
    };
    $scope.datasetOverride = [{ yAxisID: 'y-axis-1' }, { yAxisID: 'y-axis-2' }];
    $scope.options = {
        scales: {
            yAxes: [
                {
                    id: 'y-axis-1',
                    type: 'linear',
                    display: true,
                    position: 'left'
                },
                {
                    id: 'y-axis-2',
                    type: 'linear',
                    display: true,
                    position: 'right'
                }
            ]
        }
    };


    $scope.labels = ["Download Sales", "In-Store Sales", "Mail-Order Sales"];
    $scope.data = [300, 500, 100];
    //----------Charts end---------

    //----------Dashboard Page----------

    //----------Dashboard Page End----------


    //----------Inventory Orders Page----------
    //----------Inventory Orders End----------

    //----------Inventory Payment----------

    //----------Inventory Payment End----------

    //----------Inventory Delivery Page----------
    //----------Shipments----------

    //----------Shipments End----------


    //----------Meetups----------
    //----------Meetups End----------

    //----------Inventory Delivery End----------

    //-------------------------------------------------------FRONT END // E-COMMERCE //////

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
        window.location.href = '/SolteiraAndBrands/SolteiraBrandsJewelries?id=' + item.id;
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
        // 1. Create a cart item object
        let cartItem = {
            id: product.id,
            name: product.name,
            price: product.price,
            img: product.img,
            stones: product.stones,
            material: product.material,
            weight: product.weight,
            qty: parseInt(quantity) || 1,
            selected: true // For the checkbox
        };

        // 2. Check if item already exists in cart
        let existingItem = $scope.cart.find(i => i.id === product.id);

        if (existingItem) {
            existingItem.qty += cartItem.qty;
        } else {
            $scope.cart.push(cartItem);
        }

        // 3. Save to localStorage
        localStorage.setItem('sb_cart', JSON.stringify($scope.cart));

        // 4. Feedback to user
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

});