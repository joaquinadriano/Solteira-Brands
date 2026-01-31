app.controller("SolteiraAndBrandsController", function ($scope, SolteiraAndBrandsService) {




    //----------Navigation----------

    //to dashboard
    $scope.inventoryDashboard = function () {
        window.location.href = "/SolteiraAndBrands/AdminDashboardPage";
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


});