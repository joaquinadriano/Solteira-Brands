app.controller("SolteiraAndBrandsController", ['$scope', '$timeout', '$interval', '$sce', 'SolteiraAndBrandsService', function ($scope, $timeout, $interval, $sce, SolteiraAndBrandsService) {

    // Low-stock notifications (navbar bell) — display list is mutable for dismiss
    $scope.lowStockItems = [];
    $scope.notificationItemsDisplay = [];
    $scope.loadLowStockNotifications = function () {
        SolteiraAndBrandsService.getLowStockNotifications().then(function (res) {
            if (res.data && res.data.success) {
                var items = res.data.items || [];
                $scope.lowStockItems = items;
                $scope.notificationItemsDisplay = angular.copy(items);
            }
        }, function () {
            $scope.lowStockItems = [];
            $scope.notificationItemsDisplay = [];
        });
    };
    $scope.getNotificationCount = function () {
        return ($scope.notificationItemsDisplay || []).length;
    };
    $scope.dismissNotification = function (index) {
        if ($scope.notificationItemsDisplay && index >= 0 && index < $scope.notificationItemsDisplay.length) {
            $scope.notificationItemsDisplay.splice(index, 1);
        }
    };
    $scope.isOutOfStock = function (item) {
        return item && (item.stockQuantity == null || item.stockQuantity === 0);
    };
    $scope.loadLowStockNotifications();

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
        window.location.href = "/SolteiraAndBrands/AdminOrdersPage";
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

    // ----- Users (Admin): registered users list -----
    $scope.usersList = [];
    $scope.selectedUserForView = null;

    $scope.openViewUserModal = function (user) {
        if (!user) return;
        $scope.selectedUserForView = user;
        $timeout(function () {
            var el = document.getElementById('viewUserModal');
            if (el && window.bootstrap && window.bootstrap.Modal) {
                var modal = window.bootstrap.Modal.getOrCreateInstance(el);
                modal.show();
            }
        }, 0);
    };

    $scope.closeViewUserModal = function () {
        var el = document.getElementById('viewUserModal');
        if (el && window.bootstrap && window.bootstrap.Modal) {
            var modal = window.bootstrap.Modal.getInstance(el) || window.bootstrap.Modal.getOrCreateInstance(el);
            if (modal) modal.hide();
        }
        $scope.selectedUserForView = null;
    };

    $scope.loadUsersForAdmin = function () {
        SolteiraAndBrandsService.getUsersForAdmin().then(function (res) {
            var d = res.data;
            var raw = [];
            if (d && angular.isArray(d.data)) raw = d.data;
            else if (d && angular.isArray(d.Data)) raw = d.Data;
            else if (angular.isArray(d)) raw = d;
            // Normalize to camelCase so the view can use u.id, u.username, etc. Skip null items.
            $scope.usersList = (raw || []).filter(function (u) { return u != null; }).map(function (u) {
                return {
                    id: u.id != null ? u.id : u.Id,
                    username: (u.username != null ? u.username : u.Username) || '',
                    email: (u.email != null ? u.email : u.Email) || '',
                    roleName: (u.roleName != null ? u.roleName : u.RoleName) || 'UNKNOWN',
                    isActive: u.isActive != null ? u.isActive : u.IsActive,
                    isVerified: u.isVerified != null ? u.isVerified : u.IsVerified,
                    joinedAtString: (u.joinedAtString != null ? u.joinedAtString : u.JoinedAtString) || '-'
                };
            });
        }, function (err) {
            $scope.usersList = [];
            var ed = err && err.data;
            if (ed && (angular.isArray(ed.data) || angular.isArray(ed.Data))) {
                $scope.usersList = ed.data || ed.Data || [];
            }
        });
    };


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
                window.location.href = "/SolteiraAndBrands/SolteiraBrandsLogin";
            } 
        });
    }

    // ----- Orders (Admin): list, details modal, status update -----
    $scope.ordersList = [];
    $scope.selectedOrderDetails = null;
    $scope.orderStatusOptions = ['Pending Approval', 'Approved', 'Rejected', 'Awaiting Payment Verification', 'Pending Payment', 'Pending Downpayment', 'Paid', 'Layaway Active', 'In Delivery', 'Ready for Meetup', 'No Show', 'Meetup Rescheduled', 'Completed', 'Cancelled'];

    $scope.loadOrders = function () {
        SolteiraAndBrandsService.getOrders().then(function (res) {
            if (res.data && res.data.success && angular.isArray(res.data.data)) {
                $scope.ordersList = res.data.data;
            } else {
                $scope.ordersList = [];
            }
            $timeout(function () {
                if (typeof window.initializeStandardTable !== 'function') return;
                try {
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable && $('#adminOrdersTable').length && $.fn.DataTable.isDataTable('#adminOrdersTable')) {
                        $('#adminOrdersTable').DataTable().destroy();
                    }
                } catch (e) { }
                if ($scope.ordersList && $scope.ordersList.length > 0) {
                    // Sort by Order ID column (index 0) descending so most recent (highest ID) appears first.
                    window.initializeStandardTable('#adminOrdersTable', 0);
                }
            }, 150);
        }, function () {
            $scope.ordersList = [];
        });
    };

    // ----- Orders Archive (ADMIN): list, restore -----
    $scope.archiveOrdersList = [];
    $scope.loadArchiveOrders = function () {
        SolteiraAndBrandsService.getArchiveOrders().then(function (res) {
            if (res.data && res.data.success && angular.isArray(res.data.data)) {
                $scope.archiveOrdersList = res.data.data;
            } else {
                $scope.archiveOrdersList = [];
            }
            $timeout(function () {
                if (typeof window.initializeStandardTable !== 'function') return;
                try {
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable && $('#orderArchiveTable').length && $.fn.DataTable.isDataTable('#orderArchiveTable')) {
                        $('#orderArchiveTable').DataTable().destroy();
                    }
                } catch (e) { }
                if ($scope.archiveOrdersList && $scope.archiveOrdersList.length > 0) {
                    // Sort archived orders by Order ID column (index 0) descending so most recent is on top.
                    window.initializeStandardTable('#orderArchiveTable', 0);
                }
            }, 150);
        }, function () {
            $scope.archiveOrdersList = [];
        });
    };

    $scope.openOrderDetails = function (order) {
        if (!order || !order.id) return;
        $scope.selectedOrderDetails = null;
        SolteiraAndBrandsService.getOrderDetails(order.id).then(function (res) {
            if (res.data && res.data.success && res.data.order) {
                $scope.selectedOrderDetails = res.data.order;
                var modalEl = document.getElementById('viewOrderDetailsModal');
                if (modalEl && window.bootstrap) {
                    var modal = new bootstrap.Modal(modalEl);
                    modal.show();
                }
            }
        });
    };

    $scope.updateOrderStatusFor = function (orderId, newStatus, rejectionReason, restoreStock) {
        if (!orderId || !newStatus) return;
        SolteiraAndBrandsService.updateOrderStatus(orderId, newStatus, rejectionReason, restoreStock).then(function (res) {
            if (res.data && res.data.success) {
                // Optimistic local update so status reflects instantly in the list.
                var updatedStatus = (res.data.orderStatus || newStatus);
                if ($scope.ordersList && $scope.ordersList.length) {
                    for (var i = 0; i < $scope.ordersList.length; i++) {
                        var o = $scope.ordersList[i];
                        if (o && (o.id === orderId || o.Id === orderId)) {
                            o.orderStatus = updatedStatus;
                            o.status = updatedStatus;
                            break;
                        }
                    }
                }
                $scope.loadOrders();
                if ($scope.selectedOrderDetails && $scope.selectedOrderDetails.id === orderId) {
                    $scope.refreshSelectedOrderDetails();
                }
                if (newStatus === 'Approved') {
                    Swal.fire({ title: 'Order approved', text: 'Order has been approved. Buyer can now submit payment.', icon: 'success', confirmButtonColor: '#28a745' }).then(function () {
                        window.location.reload();
                    });
                } else if (newStatus === 'Rejected') {
                    Swal.fire({ title: 'Order rejected', text: 'Order has been rejected. Rejection reason was saved.', icon: 'success', confirmButtonColor: '#dc3545' }).then(function () {
                        window.location.reload();
                    });
                } else {
                    Swal.fire({ title: 'Order status updated', icon: 'success', confirmButtonText: 'OK' }).then(function () {
                        window.location.reload();
                    });
                }
            } else {
                Swal.fire({ icon: 'error', title: 'Update failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Update failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };

    $scope.statusChange = function (orderId, newStatus) {
        if (newStatus === 'Rejected') {
            Swal.fire({
                title: 'Reject order',
                input: 'textarea',
                inputLabel: 'Rejection reason (required)',
                inputPlaceholder: 'Enter reason for rejection...',
                inputAttributes: { 'aria-label': 'Rejection reason', rows: 4 },
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Reject order',
                cancelButtonText: 'Cancel',
                confirmButtonColor: '#dc3545',
                inputValidator: function (value) {
                    if (!value || !value.trim()) return 'Please enter a rejection reason.';
                    return null;
                }
            }).then(function (result) {
                if (result.isConfirmed && result.value) {
                    var reason = result.value.trim();
                    Swal.fire({
                        title: 'Return items to stock?',
                        text: 'Order will be cancelled. Do you want to return these items to active inventory?',
                        icon: 'question',
                        showCancelButton: true,
                        confirmButtonText: 'Yes, return to stock',
                        cancelButtonText: 'No, keep stock as is'
                    }).then(function (choice) {
                        if (choice.isConfirmed) {
                            // Yes → reject and restore stock.
                            $scope.updateOrderStatusFor(orderId, 'Rejected', reason, true);
                        } else if (choice.dismiss === 'cancel') {
                            // "No, keep stock as is" → proceed with reject but do not change stock.
                            $scope.updateOrderStatusFor(orderId, 'Rejected', reason, false);
                        } else {
                            // X / backdrop / Esc → abort without changing anything.
                            return;
                        }
                    });
                }
            });
        } else {
            $scope.updateOrderStatusFor(orderId, newStatus);
        }
    };

    $scope.refreshSelectedOrderDetails = function () {
        if ($scope.selectedOrderDetails && $scope.selectedOrderDetails.id) {
            SolteiraAndBrandsService.getOrderDetails($scope.selectedOrderDetails.id).then(function (res) {
                if (res.data && res.data.success && res.data.order) {
                    $scope.selectedOrderDetails = res.data.order;
                }
            });
            SolteiraAndBrandsService.getOrders().then(function (res) {
                if (res.data && res.data.success && angular.isArray(res.data.data)) {
                    $scope.ordersList = res.data.data;
                }
            });
        }
    };

    $scope.revokeApproval = function (orderId) {
        if (!orderId) return;
        Swal.fire({
            title: 'Revoke approval?',
            text: 'This will move the order back to Pending Approval and the buyer will not be able to proceed with payment until you approve again.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, revoke approval',
            cancelButtonText: 'Cancel'
        }).then(function (result) {
            if (!result.isConfirmed) return;
            SolteiraAndBrandsService.revokeApproval(orderId).then(function (res) {
                if (res.data && res.data.success) {
                    var updatedStatus = (res.data.orderStatus || 'Pending Approval');
                    if ($scope.ordersList && $scope.ordersList.length) {
                        for (var i = 0; i < $scope.ordersList.length; i++) {
                            var o = $scope.ordersList[i];
                            if (o && (o.id === orderId || o.Id === orderId)) {
                                o.orderStatus = updatedStatus;
                                o.status = updatedStatus;
                                break;
                            }
                        }
                    }
                    if ($scope.selectedOrderDetails && $scope.selectedOrderDetails.id === orderId) {
                        $scope.selectedOrderDetails.orderStatus = updatedStatus;
                        $scope.selectedOrderDetails.status = updatedStatus;
                    }
                    $scope.loadOrders();
                    $scope.refreshSelectedOrderDetails();
                    Swal.fire({ title: 'Approval revoked', icon: 'success', confirmButtonText: 'OK' }).then(function () {
                        window.location.reload();
                    });
                } else {
                    Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Cannot revoke (e.g. payment already submitted).' });
                }
            }, function (err) {
                Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
            });
        });
    };

    $scope.openCreateDelivery = function (order) {
        var orderId = order.id || order.Id;
        if (!orderId) return;
        Swal.fire({
            title: 'Create Delivery',
            html: '<label class="text-start d-block mb-1">Delivery type *</label><select id="delivery-type" class="form-control mb-3"><option value="COURIER">Courier</option><option value="MEETUP">Meetup</option></select><label class="text-start d-block mb-1">Tracking number (optional)</label><input id="delivery-tracking" class="form-control" placeholder="e.g. 1234567890" />',
            showCancelButton: true,
            confirmButtonText: 'Create',
            confirmButtonColor: '#87AE73'
        }).then(function (result) {
            if (result.isConfirmed) {
                var typeEl = document.getElementById('delivery-type');
                var trackEl = document.getElementById('delivery-tracking');
                var deliveryType = typeEl ? typeEl.value : 'COURIER';
                var trackingNumber = trackEl && trackEl.value ? trackEl.value.trim() : null;
                SolteiraAndBrandsService.createDelivery(orderId, deliveryType, trackingNumber).then(function (res) {
                    if (res.data && res.data.success) {
                        $scope.refreshSelectedOrderDetails();
                        Swal.fire({ title: 'Delivery created', icon: 'success', confirmButtonText: 'OK' });
                    } else {
                        Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
                    }
                }, function (err) {
                    Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
                });
            }
        });
    };

    $scope.updateDeliveryStatus = function (deliveryId, status) {
        if (!deliveryId || !status) return;
        SolteiraAndBrandsService.updateDeliveryStatus(deliveryId, status).then(function (res) {
            if (res.data && res.data.success) {
                // Update local shipments list immediately.
                var newDeliveryStatus = (res.data.deliveryStatus || status);
                if ($scope.shipmentsList && $scope.shipmentsList.length) {
                    for (var i = 0; i < $scope.shipmentsList.length; i++) {
                        var d = $scope.shipmentsList[i];
                        if (d && d.id === deliveryId) {
                            d.deliveryStatus = newDeliveryStatus;
                            break;
                        }
                    }
                }
                $scope.loadOrders();
                $scope.refreshSelectedOrderDetails();
                $scope.loadDeliveryShipments();
                Swal.fire({ title: 'Delivery status updated', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };
    $scope.updateDeliveryStatusFromViewModal = function (deliveryId, status) {
        if (!deliveryId || !status) return;
        SolteiraAndBrandsService.updateDeliveryStatus(deliveryId, status).then(function (res) {
            if (res.data && res.data.success) {
                $scope.closeDeliveryViewModal();
                // Update local shipments list immediately.
                var newDeliveryStatus = (res.data.deliveryStatus || status);
                if ($scope.shipmentsList && $scope.shipmentsList.length) {
                    for (var i = 0; i < $scope.shipmentsList.length; i++) {
                        var d = $scope.shipmentsList[i];
                        if (d && d.id === deliveryId) {
                            d.deliveryStatus = newDeliveryStatus;
                            break;
                        }
                    }
                }
                $scope.loadOrders();
                $scope.refreshSelectedOrderDetails();
                $scope.loadDeliveryShipments();
                Swal.fire({ title: 'Delivery status updated', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };

    $scope.deliveryDetail = null;
    $scope.openDeliveryViewModal = function (d) {
        if (!d || !d.id) return;
        $scope.deliveryDetail = null;
        SolteiraAndBrandsService.getDeliveryDetailForAdmin(d.id).then(function (res) {
            if (res.data && res.data.success) {
                $scope.deliveryDetail = res.data;
                var el = document.getElementById('deliveryViewModal');
                if (el && window.bootstrap) { var m = new bootstrap.Modal(el); m.show(); }
            }
        }, function () { Swal.fire({ icon: 'error', title: 'Failed', text: 'Could not load delivery details.' }); });
    };
    $scope.closeDeliveryViewModal = function () {
        var el = document.getElementById('deliveryViewModal');
        if (el && window.bootstrap) { var m = bootstrap.Modal.getInstance(el); if (m) m.hide(); }
        $scope.deliveryDetail = null;
    };

    $scope.editingDelivery = null;
    $scope.editDeliveryForm = { courierChoice: 'LBC', otherCourierName: '', trackingNumber: '', trackingLink: '', dispatchDate: '', eta: '', deliveryStatus: '' };
    $scope.openDeliveryEditModal = function (d) {
        if (!d || !d.id) return;
        if ((d.deliveryStatus || '').toUpperCase() === 'DELIVERED') {
            Swal.fire({ icon: 'warning', title: 'Cannot edit', text: 'Delivered shipments cannot be edited.' });
            return;
        }
        SolteiraAndBrandsService.getDeliveryDetailForAdmin(d.id).then(function (res) {
            if (res.data && res.data.success) {
                var ov = res.data.deliveryOverview;
                $scope.editingDelivery = res.data;
                var courierVal = (ov.courier || '').trim();
                var courierChoice = 'LBC';
                // Only allow LBC + Fedex on the UI, but keep legacy values from breaking the edit modal.
                if (courierVal && courierVal.toLowerCase().indexOf('fedex') !== -1) {
                    courierChoice = 'Fedex';
                } else if (courierVal && courierVal.toLowerCase().indexOf('lbc') !== -1) {
                    courierChoice = 'LBC';
                } else {
                    courierChoice = 'LBC';
                }
                var tn = ov.trackingNumber || '';
                var tnParts = tn.split('||');
                var tnNumber = tnParts[0] || '';
                var tnLink = tnParts.length > 1 ? tnParts[1] : '';
                $scope.editTrackingNumberTouched = false;
                $scope.editDeliveryForm = {
                    courierChoice: courierChoice,
                    otherCourierName: '',
                    trackingNumber: tnNumber,
                    trackingLink: tnLink,
                    dispatchDate: ov.dispatchDateIso || '',
                    eta: ov.etaIso || '',
                    deliveryStatus: ov.deliveryStatus || 'IN_TRANSIT'
                };
                var el = document.getElementById('deliveryEditModal');
                if (el && window.bootstrap) { var m = new bootstrap.Modal(el); m.show(); }
            }
        }, function () { Swal.fire({ icon: 'error', title: 'Failed', text: 'Could not load delivery.' }); });
    };
    $scope.closeDeliveryEditModal = function () {
        var el = document.getElementById('deliveryEditModal');
        if (el && window.bootstrap) { var m = bootstrap.Modal.getInstance(el); if (m) m.hide(); }
        $scope.editingDelivery = null;
    };
    $scope.saveDeliveryEdit = function () {
        if (!$scope.editingDelivery || !$scope.editingDelivery.deliveryOverview) return;
        var id = $scope.editingDelivery.deliveryOverview.id;
        var currentStatus = ($scope.editingDelivery.deliveryOverview.deliveryStatus || '').toUpperCase();
        var courierChoice = ($scope.editDeliveryForm.courierChoice || '').trim();
        try {
            var editCourierSelectEl = document.getElementById('editCourierChoiceSelect');
            if (editCourierSelectEl && editCourierSelectEl.value) {
                courierChoice = (editCourierSelectEl.value || '').toString().trim() || courierChoice;
            }
        } catch (e) { }
        var courier = (courierChoice && courierChoice.toLowerCase().indexOf('fedex') !== -1) ? 'Fedex' : 'LBC';
        var rawNumberEdit = ($scope.editDeliveryForm.trackingNumber || '').trim();
        var rawLinkEdit = ($scope.editDeliveryForm.trackingLink || '').trim();
        var trackingNumber = null;
        if (currentStatus === 'PENDING') {
            if (!rawNumberEdit) {
                Swal.fire({ icon: 'warning', title: 'Tracking number required', text: 'Please enter a tracking number.' });
                return;
            }
            if (!$scope.isValidTrackingNumber(rawNumberEdit)) {
                Swal.fire({ icon: 'warning', title: 'Invalid tracking number', text: 'Tracking number must be 10-22 digits.' });
                return;
            }
            if (!$scope.editDeliveryForm.dispatchDate) {
                Swal.fire({ icon: 'warning', title: 'Dispatch date required', text: 'Please select a dispatch date.' });
                return;
            }
            if (!$scope.editDeliveryForm.eta) {
                Swal.fire({ icon: 'warning', title: 'ETA required', text: 'Please select an ETA.' });
                return;
            }
            if ($scope.editDeliveryForm.dispatchDate && $scope.editDeliveryForm.eta && $scope.editDeliveryForm.eta < $scope.editDeliveryForm.dispatchDate) {
                Swal.fire({ icon: 'warning', title: 'Invalid dates', text: 'ETA cannot be earlier than the dispatch date.' });
                return;
            }
        } else {
            // IN_TRANSIT: validate only if the user provided a new tracking number.
            if (rawNumberEdit && !$scope.isValidTrackingNumber(rawNumberEdit)) {
                Swal.fire({ icon: 'warning', title: 'Invalid tracking number', text: 'Tracking number must be 10-22 digits.' });
                return;
            }
        }
        if (rawLinkEdit) {
            // Only persist link alongside a valid tracking number.
            trackingNumber = rawNumberEdit ? (rawNumberEdit + '||' + rawLinkEdit) : null;
        } else {
            trackingNumber = rawNumberEdit || null;
        }
        var dispatchDate = (currentStatus === 'PENDING') ? ($scope.editDeliveryForm.dispatchDate || null) : null;
        var eta = (currentStatus === 'PENDING') ? ($scope.editDeliveryForm.eta || null) : null;
        var deliveryStatus = ($scope.editDeliveryForm.deliveryStatus || '').trim() || null;
        SolteiraAndBrandsService.updateDelivery(id, courier, trackingNumber, dispatchDate, eta, deliveryStatus).then(function (res) {
            if (res.data && res.data.success) {
                $scope.loadDeliveryShipments();
                $scope.closeDeliveryEditModal();
                Swal.fire({ title: 'Delivery updated', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) { Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' }); });
    };

    $scope.markReadyForMeetup = function (orderId) {
        if (!orderId) return;
        SolteiraAndBrandsService.markReadyForMeetup(orderId).then(function (res) {
            if (res.data && res.data.success) {
                $scope.loadOrders();
                $scope.refreshSelectedOrderDetails();
                Swal.fire({ title: 'Ready for Meetup', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };

    $scope.completeMeetup = function (orderId) {
        if (!orderId) return;
        SolteiraAndBrandsService.completeMeetup(orderId).then(function (res) {
            if (res.data && res.data.success) {
                $scope.loadOrders();
                $scope.refreshSelectedOrderDetails();
                Swal.fire({ title: 'Order completed', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };

    $scope.ordersForShipments = [];
    $scope.shipmentsList = [];
    $scope.loadDeliveryShipments = function () {
        SolteiraAndBrandsService.getOrdersForDeliveryShipments().then(function (res) {
            $scope.ordersForShipments = (res.data && res.data.success && res.data.data) ? res.data.data : [];
        });
        SolteiraAndBrandsService.getDeliveryShipments().then(function (res) {
            $scope.shipmentsList = (res.data && res.data.success && res.data.data) ? res.data.data : [];
            $timeout(function () {
                if (typeof window.initializeStandardTable !== 'function') return;
                try {
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable && $('#deliveryShipmentsTable').length && $.fn.DataTable.isDataTable('#deliveryShipmentsTable')) {
                        $('#deliveryShipmentsTable').DataTable().destroy();
                    }
                } catch (e) { }
                // Wait one more tick to ensure ng-repeat rows are in the DOM.
                $timeout(function () {
                    if ($scope.shipmentsList && $scope.shipmentsList.length > 0) {
                        window.initializeStandardTable('#deliveryShipmentsTable', 0);
                    }
                }, 0);
            }, 250);
        });
    };
    $scope.getDisplayTrackingNumber = function (raw) {
        if (!raw) return '-';
        var s = (raw + '').trim();
        if (!s) return '-';
        var parts = s.split('||');
        return parts[0] || parts[1] || '-';
    };
    var TRACKING_NUMBER_REGEX = /^\d{10,22}$/;
    $scope.isValidTrackingNumber = function (val) {
        var s = (val || '').toString().trim();
        return !!s && TRACKING_NUMBER_REGEX.test(s);
    };
    $scope.trackingNumberForNewTouched = false;
    $scope.editTrackingNumberTouched = false;
    $scope.onTrackingNumberForNewInputChange = function () {
        $scope.trackingNumberForNewTouched = true;
    };
    $scope.onEditTrackingNumberInputChange = function () {
        $scope.editTrackingNumberTouched = true;
    };
    function toYyyyMmDd(val) {
        if (!val && val !== 0) return null;
        if (val instanceof Date) {
            var d = val;
            var yyyy = d.getFullYear();
            var mm = d.getMonth() + 1;
            var dd = d.getDate();
            var mmStr = mm < 10 ? '0' + mm : '' + mm;
            var ddStr = dd < 10 ? '0' + dd : '' + dd;
            return yyyy + '-' + mmStr + '-' + ddStr;
        }
        var s = (typeof val === 'string') ? val.trim() : String(val).trim();
        return s || null;
    }
    /** Read yyyy-MM-dd from native date input (reliable when ng-model does not sync in modals). */
    function readDateInputYyyyMmDd(elementId) {
        try {
            var el = document.getElementById(elementId);
            if (el && el.value && /^\d{4}-\d{2}-\d{2}$/.test(el.value.trim())) {
                return el.value.trim();
            }
        } catch (e) { /* ignore */ }
        return null;
    }
    $scope.createDeliveryFromShipmentsPage = function () {
        var orderId = $scope.selectedOrderForDelivery ? ($scope.selectedOrderForDelivery.id || $scope.selectedOrderForDelivery.orderId) : null;
        if (!orderId) {
            Swal.fire({ icon: 'warning', title: 'Select an order', text: 'Choose a Paid order from the list.' });
            return;
        }

        var deliveryType = $scope.deliveryTypeForNew || 'COURIER';
        // Read courier from DOM as well (modal select binding can occasionally lag behind click).
        var courierChoice = ($scope.courierForNewShipment || 'LBC').trim() || 'LBC';
        try {
            var courierSelectEl = document.getElementById('courierForNewShipmentSelect');
            if (courierSelectEl && courierSelectEl.value) {
                courierChoice = (courierSelectEl.value || '').toString().trim() || courierChoice;
            }
        } catch (e) { }
        // Only persist LBC + Fedex from this page.
        var courier = (courierChoice && courierChoice.toLowerCase().indexOf('fedex') !== -1) ? 'Fedex' : 'LBC';

        // Read tracking number directly from the dedicated input (most reliable).
        var rawNumber = '';
        try {
            var tnInput = document.getElementById('trackingNumberForNewInput');
            if (tnInput && tnInput.value) {
                rawNumber = tnInput.value.trim();
                $scope.trackingNumberForNew = rawNumber;
            } else if ($scope.trackingNumberForNew) {
                rawNumber = String($scope.trackingNumberForNew).trim();
            }
        } catch (e) {
            rawNumber = ($scope.trackingNumberForNew || '').trim();
        }

        if (!rawNumber) {
            Swal.fire({ icon: 'warning', title: 'Tracking number required', text: 'Please enter a tracking number.' });
            return;
        }
        if (!$scope.isValidTrackingNumber(rawNumber)) {
            Swal.fire({ icon: 'warning', title: 'Invalid tracking number', text: 'Tracking number must be 10-22 digits.' });
            return;
        }

        // Date inputs: read DOM first (Angular date + modal often leaves scope empty).
        var dispatchDate = readDateInputYyyyMmDd('dispatchDateForNewInput') || toYyyyMmDd($scope.dispatchDateForNew);
        var eta = readDateInputYyyyMmDd('etaForNewInput') || toYyyyMmDd($scope.etaForNew);
        if (dispatchDate) { $scope.dispatchDateForNew = dispatchDate; }
        if (eta) { $scope.etaForNew = eta; }

        if (!dispatchDate) {
            Swal.fire({ icon: 'warning', title: 'Dispatch date required', text: 'Please select a dispatch date.' });
            return;
        }
        if (!eta) {
            Swal.fire({ icon: 'warning', title: 'ETA required', text: 'Please select an ETA.' });
            return;
        }
        if (dispatchDate && eta && eta < dispatchDate) {
            Swal.fire({ icon: 'warning', title: 'Invalid dates', text: 'ETA cannot be earlier than the dispatch date.' });
            return;
        }

        var trackingNumber = rawNumber || null;

        SolteiraAndBrandsService.createDelivery(orderId, deliveryType, trackingNumber, courier, dispatchDate, eta).then(function (res) {
            if (res.data && res.data.success) {
                $scope.loadDeliveryShipments();
                $scope.selectedOrderForDelivery = null;
                $scope.trackingNumberForNew = '';
                $scope.trackingNumberForNewTouched = false;
                $scope.courierForNewShipment = 'LBC';
                $scope.dispatchDateForNew = '';
                $scope.etaForNew = '';
                var modalEl = document.getElementById('deliveryAddShipmentModal');
                if (modalEl && window.bootstrap) { var m = bootstrap.Modal.getInstance(modalEl); if (m) m.hide(); }
                Swal.fire({ title: 'Delivery created', icon: 'success', confirmButtonText: 'OK' });
            } else { Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' }); }
        }, function (err) { Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' }); });
    };

    $scope.meetupOrdersList = [];
    $scope.loadDeliveryMeetup = function () {
        SolteiraAndBrandsService.getOrdersForDeliveryMeetup().then(function (res) {
            var raw = res.data;
            var list = (raw && raw.success && raw.data) ? raw.data : [];
            $scope.meetupOrdersList = list;
            $timeout(function () {
                if (typeof window.initializeStandardTable !== 'function') return;
                try {
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable && $('#deliveryMeetupTable').length && $.fn.DataTable.isDataTable('#deliveryMeetupTable')) {
                        $('#deliveryMeetupTable').DataTable().destroy();
                    }
                } catch (e) { }
                if ($scope.meetupOrdersList && $scope.meetupOrdersList.length > 0) {
                    // Sort meetup list by Order ID column (index 0) so latest ORD-* appears first.
                    window.initializeStandardTable('#deliveryMeetupTable', 0);
                }
            }, 150);
        }, function (err) {
            if (window.console && window.console.error) { window.console.error('GetOrdersForDeliveryMeetup failed', err && err.data); }
            $scope.meetupOrdersList = [];
        });
    };
    $scope.meetupCompletedOrderId = null;
    $scope.completeMeetupWithReceipt = function (orderId) {
        $scope.meetupCompletedOrderId = orderId;
        var el = document.getElementById('meetupReceiptFile');
        if (el) el.value = '';
        var modalEl = document.getElementById('meetupReceiptModal');
        if (modalEl && window.bootstrap) { var m = new bootstrap.Modal(modalEl); m.show(); }
    };
    $scope.confirmMeetupCompleted = function () {
        var orderId = $scope.meetupCompletedOrderId;
        if (!orderId) { Swal.fire({ icon: 'warning', title: 'Error', text: 'No order selected.' }); return; }
        var fileInput = document.getElementById('meetupReceiptFile');
        if (!fileInput || !fileInput.files || !fileInput.files[0]) { Swal.fire({ icon: 'warning', title: 'Select file', text: 'Please select a receipt image (JPG or PNG, max 5MB).' }); return; }
        var file = fileInput.files[0];
        SolteiraAndBrandsService.uploadMeetupReceipt(file).then(function (res) {
            if (res.data && res.data.success && res.data.imageID) {
                var receiptId = res.data.imageID;
                SolteiraAndBrandsService.updateMeetupOutcome(orderId, 'COMPLETED', receiptId).then(function (r) {
                    if (r.data && r.data.success) {
                        $scope.meetupCompletedOrderId = null;
                        var modalEl = document.getElementById('meetupReceiptModal');
                        if (modalEl && window.bootstrap) { var m = bootstrap.Modal.getInstance(modalEl); if (m) m.hide(); }
                        $scope.loadDeliveryMeetup();
                        Swal.fire({ title: 'Meetup completed', icon: 'success', confirmButtonText: 'OK' });
                    } else { Swal.fire({ icon: 'error', title: 'Failed', text: (r.data && r.data.message) || 'Please try again.' }); }
                }, function (err) { Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' }); });
            } else { Swal.fire({ icon: 'error', title: 'Upload failed', text: (res.data && res.data.message) || 'Please try again.' }); }
        }, function (err) { Swal.fire({ icon: 'error', title: 'Upload failed', text: (err.data && err.data.message) || 'Please try again.' }); });
    };
    $scope.updateMeetupOutcome = function (orderId, status, receiptImageId) {
        if (!orderId || !status) return;

        // Special handling for final cancellation: capture reason + stock decision,
        // then pass through to backend so order is Cancelled with optional stock restore.
        if (status === 'CANCELLED') {
            Swal.fire({
                title: 'Cancel meetup order',
                input: 'textarea',
                inputLabel: 'Cancellation reason (required)',
                inputPlaceholder: 'Enter reason for cancelling this meetup order...',
                inputAttributes: { 'aria-label': 'Cancellation reason', rows: 4 },
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Cancel order',
                cancelButtonText: 'Back',
                confirmButtonColor: '#dc3545',
                inputValidator: function (value) {
                    if (!value || !value.trim()) return 'Please enter a cancellation reason.';
                    return null;
                }
            }).then(function (result) {
                if (!result.isConfirmed || !result.value) return;
                var reason = result.value.trim();
                Swal.fire({
                    title: 'Return items to stock?',
                    text: 'Order will be cancelled. Do you want to return these items to active inventory?',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'Yes, return to stock',
                    cancelButtonText: 'No, keep stock as is'
                }).then(function (choice) {
                    if (!choice.isConfirmed && !(choice.dismiss || choice.isDismissed || choice.isDenied)) {
                        return;
                    }
                    var restoreStock = !!choice.isConfirmed;
                    SolteiraAndBrandsService.updateMeetupOutcome(orderId, 'CANCELLED', null, reason, restoreStock).then(function (res) {
                        if (res.data && res.data.success) {
                            $scope.loadDeliveryMeetup();
                            if ($scope.loadOrders) $scope.loadOrders();
                            Swal.fire({ title: 'Order cancelled', icon: 'success', confirmButtonText: 'OK' });
                        } else {
                            Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
                        }
                    }, function (err) {
                        Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
                    });
                });
            });
            return;
        }

        SolteiraAndBrandsService.updateMeetupOutcome(orderId, status, receiptImageId).then(function (res) {
            if (res.data && res.data.success) {
                $scope.loadDeliveryMeetup();
                Swal.fire({ title: 'Updated', icon: 'success', confirmButtonText: 'OK' });
            } else { Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' }); }
        }, function (err) { Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' }); });
    };

    $scope.meetupDetail = null;
    $scope.openMeetupViewModal = function (o) {
        if (!o || !o.id) return;
        $scope.meetupDetail = null;
        SolteiraAndBrandsService.getMeetupOrderDetail(o.id).then(function (res) {
            if (res.data && res.data.success) {
                $scope.meetupDetail = res.data;
                var el = document.getElementById('meetupViewModal');
                if (el && window.bootstrap) { var m = new bootstrap.Modal(el); m.show(); }
            }
        }, function () { Swal.fire({ icon: 'error', title: 'Failed', text: 'Could not load meetup details.' }); });
    };
    $scope.closeMeetupViewModal = function () {
        var el = document.getElementById('meetupViewModal');
        if (el && window.bootstrap) { var m = bootstrap.Modal.getInstance(el); if (m) m.hide(); }
        $scope.meetupDetail = null;
    };

    // Reschedule meetup (same date/time format as buyer: yyyy-MM-dd + time slots 10:00 AM – 5:00 PM)
    var MEETUP_TIME_SLOTS = ['10:00 AM', '10:30 AM', '11:00 AM', '11:30 AM', '12:00 PM', '12:30 PM', '1:00 PM', '1:30 PM', '2:00 PM', '2:30 PM', '3:00 PM', '3:30 PM', '4:00 PM', '4:30 PM', '5:00 PM'];
    function getMinMeetupDateReschedule() {
        var now = new Date();
        var yyyy = now.getFullYear();
        var mm = String(now.getMonth() + 1).padStart(2, '0');
        var dd = String(now.getDate()).padStart(2, '0');
        if (now.getHours() >= 17) {
            var tomorrow = new Date(now);
            tomorrow.setDate(tomorrow.getDate() + 1);
            return tomorrow.getFullYear() + '-' + String(tomorrow.getMonth() + 1).padStart(2, '0') + '-' + String(tomorrow.getDate()).padStart(2, '0');
        }
        return yyyy + '-' + mm + '-' + dd;
    }
    $scope.rescheduleOrderId = null;
    $scope.rescheduleDate = null;
    $scope.rescheduleTime = null;
    $scope.rescheduleTimeOptions = MEETUP_TIME_SLOTS.slice();
    $scope.getMinMeetupDateReschedule = getMinMeetupDateReschedule;
    function parseTimeToMinutesReschedule(timeStr) {
        if (!timeStr) return -1;
        var m = (timeStr + '').match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
        if (!m) return -1;
        var h = parseInt(m[1], 10);
        var min = parseInt(m[2], 10);
        if (m[3].toUpperCase() === 'PM' && h !== 12) h += 12;
        if (m[3].toUpperCase() === 'AM' && h === 12) h = 0;
        return h * 60 + min;
    }
    $scope.onRescheduleDateChange = function () {
        var selected = $scope.rescheduleDate;
        if (!selected) {
            $scope.rescheduleTimeOptions = MEETUP_TIME_SLOTS.slice();
            return;
        }
        var now = new Date();
        var todayStr = now.toISOString().split('T')[0];
        var currentMinutes = now.getHours() * 60 + now.getMinutes();
        if (selected === todayStr) {
            $scope.rescheduleTimeOptions = MEETUP_TIME_SLOTS.filter(function (t) {
                var slotMin = parseTimeToMinutesReschedule(t);
                return slotMin > currentMinutes && slotMin >= 10 * 60 && slotMin <= 17 * 60;
            });
            if ($scope.rescheduleTime && $scope.rescheduleTimeOptions.indexOf($scope.rescheduleTime) === -1)
                $scope.rescheduleTime = null;
        } else {
            $scope.rescheduleTimeOptions = MEETUP_TIME_SLOTS.slice();
        }
    };
    $scope.openRescheduleModal = function (o) {
        if (!o || !o.id) return;
        $scope.rescheduleOrderId = o.id;
        $scope.rescheduleDate = getMinMeetupDateReschedule();
        $scope.rescheduleTime = $scope.rescheduleTimeOptions.length ? $scope.rescheduleTimeOptions[0] : null;
        $scope.onRescheduleDateChange();
        var modalEl = document.getElementById('rescheduleMeetupModal');
        if (modalEl && window.bootstrap) { var m = new bootstrap.Modal(modalEl); m.show(); }
    };
    $scope.closeRescheduleModal = function () {
        $scope.rescheduleOrderId = null;
        $scope.rescheduleDate = null;
        $scope.rescheduleTime = null;
        var modalEl = document.getElementById('rescheduleMeetupModal');
        if (modalEl && window.bootstrap) { var m = bootstrap.Modal.getInstance(modalEl); if (m) m.hide(); }
    };
    $scope.confirmReschedule = function () {
        var orderId = $scope.rescheduleOrderId;
        if (!orderId || !$scope.rescheduleDate || !$scope.rescheduleTime) {
            Swal.fire({ icon: 'warning', title: 'Required', text: 'Please select date and time.' });
            return;
        }

        // Normalise date value coming from <input type="date"> / ng-model.
        // The backend expects a string in yyyy-MM-dd format.
        var rawDate = $scope.rescheduleDate;
        var dateStr;
        if (rawDate instanceof Date) {
            var yyyy = rawDate.getFullYear();
            var mm = String(rawDate.getMonth() + 1).padStart(2, '0');
            var dd = String(rawDate.getDate()).padStart(2, '0');
            dateStr = yyyy + '-' + mm + '-' + dd;
        } else {
            // Fallback: strip time component if present (e.g. 2026-03-15T00:00:00.000Z)
            var asStr = (rawDate + '');
            dateStr = asStr.indexOf('T') > -1 ? asStr.split('T')[0] : asStr;
        }

        SolteiraAndBrandsService.rescheduleMeetup(orderId, dateStr, $scope.rescheduleTime).then(function (res) {
            if (res.data && res.data.success) {
                $scope.loadDeliveryMeetup();
                $scope.closeRescheduleModal();
                if ($scope.meetupDetail && $scope.meetupDetail.orderSummary && $scope.meetupDetail.orderSummary.id === orderId) {
                    $scope.meetupDetail.orderSummary.orderStatus = res.data.orderStatus;
                    if (res.data.scheduledAtFormatted) $scope.meetupDetail.scheduledAtFormatted = res.data.scheduledAtFormatted;
                }
                Swal.fire({ title: 'Meetup rescheduled', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };

    $scope.verifyPayment = function (paymentId) {
        if (!paymentId) return;
        SolteiraAndBrandsService.updatePaymentStatus(paymentId, 'VERIFIED').then(function (res) {
            if (res.data && res.data.success) {
                // Update local payments list immediately.
                if ($scope.paymentsList && $scope.paymentsList.length) {
                    for (var i = 0; i < $scope.paymentsList.length; i++) {
                        var p = $scope.paymentsList[i];
                        if (p && p.paymentId === paymentId) {
                            p.paymentStatus = 'VERIFIED';
                            break;
                        }
                    }
                }
                if ($scope.loadOrders) $scope.loadOrders();
                $scope.refreshSelectedOrderDetails();
                if ($scope.loadDeliveryMeetup) $scope.loadDeliveryMeetup();
                Swal.fire({ title: 'Payment verified', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };

    $scope.rejectPayment = function (paymentId) {
        if (!paymentId) return;
        Swal.fire({
            title: 'Reject payment',
            input: 'textarea',
            inputLabel: 'Rejection reason (required)',
            inputPlaceholder: 'Enter reason...',
            inputAttributes: { 'aria-label': 'Rejection reason', rows: 4 },
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Reject',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#dc3545',
            inputValidator: function (value) {
                if (!value || !value.trim()) return 'Please enter a rejection reason.';
                return null;
            }
        }).then(function (result) {
            if (result.isConfirmed && result.value) {
                // Quick reject from list: default to allow resubmission
                SolteiraAndBrandsService.updatePaymentStatus(paymentId, 'REJECTED', result.value.trim(), true).then(function (res) {
                    if (res.data && res.data.success) {
                        // Immediate local update for rejected payment.
                        if ($scope.paymentsList && $scope.paymentsList.length) {
                            for (var i = 0; i < $scope.paymentsList.length; i++) {
                                var p = $scope.paymentsList[i];
                                if (p && p.paymentId === paymentId) {
                                    p.paymentStatus = 'REJECTED';
                                    p.rejectionReason = result.value.trim();
                                    break;
                                }
                            }
                        }
                        $scope.refreshSelectedOrderDetails();
                        if ($scope.loadPayments) $scope.loadPayments();
                        $scope.closePaymentProofModal();
                        Swal.fire({ title: 'Payment rejected', icon: 'success', confirmButtonText: 'OK' });
                    } else {
                        Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
                    }
                }, function (err) {
                    Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
                });
            }
        });
    };

    $scope.paymentsList = [];
    $scope.selectedPayment = null;
    $scope.paymentDetail = null;
    $scope.loadPayments = function () {
        SolteiraAndBrandsService.getPaymentsForAdmin().then(function (res) {
            if (res.data && res.data.success && angular.isArray(res.data.data)) {
                $scope.paymentsList = res.data.data;
            } else {
                $scope.paymentsList = [];
            }
            $timeout(function () {
                if (typeof window.initializeStandardTable !== 'function') return;
                try {
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable && $('#paymentTable').length && $.fn.DataTable.isDataTable('#paymentTable')) {
                        $('#paymentTable').DataTable().destroy();
                    }
                } catch (e) { }
                if ($scope.paymentsList && $scope.paymentsList.length > 0) {
                    // Sort by PMT ID column (index 0) so highest/newest ID is on top.
                    window.initializeStandardTable('#paymentTable', 0);
                }
            }, 150);
        }, function () {
            $scope.paymentsList = [];
        });
    };
    $scope.paymentRefreshInterval = null;
    $scope.startPaymentRefresh = function () {
        if ($scope.paymentRefreshInterval) return;
        $scope.paymentRefreshInterval = $interval(function () { $scope.loadPayments(); }, 30000);
    };
    $scope.$on('$destroy', function () {
        if ($scope.paymentRefreshInterval) { $interval.cancel($scope.paymentRefreshInterval); $scope.paymentRefreshInterval = null; }
    });
    $scope.paymentModalForm = { rejectionReason: '', allowResubmit: true };
    $scope.paymentSuccessSwalPending = false;
    $scope.openPaymentProofModal = function (payment) {
        if (!payment || !payment.paymentId) return;
        $scope.paymentDetail = null;
        $scope.selectedPayment = null;
        $scope.paymentModalForm.rejectionReason = '';
        $scope.paymentModalForm.allowResubmit = true;
        SolteiraAndBrandsService.getPaymentDetailForAdmin(payment.paymentId).then(function (res) {
            if (res.data && res.data.success) {
                $scope.paymentDetail = res.data;
                $scope.selectedPayment = res.data.selectedPayment;
                var el = document.getElementById('paymentDetailModal');
                if (el && window.bootstrap) {
                    if (!el._paymentModalReloadAttached) {
                        el._paymentModalReloadAttached = true;
                        el.addEventListener('hidden.bs.modal', function () {
                            $scope.$evalAsync(function () {
                                if (!$scope.paymentSuccessSwalPending) {
                                    window.location.reload();
                                }
                            });
                        });
                    }
                    var m = new bootstrap.Modal(el);
                    m.show();
                }
            }
        }, function () {
            Swal.fire({ icon: 'error', title: 'Failed', text: 'Could not load payment details.' });
        });
    };
    $scope.closePaymentProofModal = function () {
        var el = document.getElementById('paymentDetailModal');
        if (el && window.bootstrap) {
            var m = bootstrap.Modal.getInstance(el);
            if (m) m.hide();
        }
        $scope.selectedPayment = null;
        $scope.paymentDetail = null;
        $scope.paymentModalForm.rejectionReason = '';
    };
    $scope.getPaymentStatusBadgeClass = function (status) {
        if (!status) return 'bg-secondary';
        var s = (status + '').toUpperCase();
        if (s === 'PENDING') return 'bg-secondary';
        if (s === 'SUBMITTED') return 'bg-warning text-dark';
        if (s === 'VERIFIED') return 'bg-success';
        if (s === 'REJECTED') return 'bg-danger';
        if (s === 'OVERDUE') return 'bg-danger';
        return 'bg-secondary';
    };
    /**
     * Payment status display text only (for payment tables/proof).
     * SUBMITTED → Awaiting Verification
     * VERIFIED  → Verified
     * REJECTED  → "Rejected - Please Resubmit" when allowResubmit=true, otherwise just "Rejected".
     */
    $scope.getPaymentStatusDisplayText = function (status, payment) {
        if (!status) return '-';
        var s = (status + '').toUpperCase();
        if (s === 'SUBMITTED') return 'Awaiting Verification';
        if (s === 'VERIFIED') return 'Verified';
        if (s === 'REJECTED') {
            var canResubmit = payment && payment.allowResubmit === true;
            return canResubmit ? 'Rejected - Please Resubmit' : 'Rejected';
        }
        return status;
    };
    /** Order status display: use order.status from DB directly. Map only for consistent labels. */
    $scope.getDisplayOrderStatus = function (orderStatus) {
        if (!orderStatus) return '-';
        var s = (orderStatus + '').trim();
        if (s === 'Rejected') return 'Payment Rejected';
        if (s === 'Cancelled') return 'Order Cancelled';
        if (s === 'Paid') return 'Paid - Waiting for Shipment';
        if (s === 'Awaiting Payment Verification') return 'Awaiting Payment Verification';
        if (s === 'Pending Approval') return 'Pending Approval';
        if (s === 'Pending Payment') return 'Pending Payment';
        if (s === 'Pending Downpayment') return 'Pending Downpayment';
        return orderStatus;
    };
    $scope.verifyPaymentFromModal = function () {
        if (!$scope.selectedPayment || !$scope.selectedPayment.paymentId) return;
        var paymentId = $scope.selectedPayment.paymentId;
        SolteiraAndBrandsService.updatePaymentStatus(paymentId, 'VERIFIED').then(function (res) {
            if (res.data && res.data.success) {
                // Reflect VERIFIED status immediately in the payments list.
                if ($scope.paymentsList && $scope.paymentsList.length) {
                    for (var i = 0; i < $scope.paymentsList.length; i++) {
                        var p = $scope.paymentsList[i];
                            if (p && p.paymentId === paymentId) {
                                p.paymentStatus = 'VERIFIED';
                                break;
                            }
                    }
                }
                $scope.loadPayments();
                $scope.paymentSuccessSwalPending = true;
                $scope.closePaymentProofModal();
                if ($scope.loadOrders) $scope.loadOrders();
                if ($scope.loadDeliveryMeetup) $scope.loadDeliveryMeetup();
                var orderStatus = (res.data && res.data.orderStatus) ? res.data.orderStatus : '';
                var msg = orderStatus === 'Ready for Meetup' ? 'Payment verified. Order is now Ready for Meetup — check Delivery - Cash through Meetup.' : 'Payment verified';
                Swal.fire({ title: 'Payment verified', text: msg, icon: 'success', confirmButtonText: 'OK' }).then(function () {
                    $scope.paymentSuccessSwalPending = false;
                    window.location.reload();
                });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };
    $scope.confirmVerifyPaymentFromModal = function () {
        if (!$scope.selectedPayment || !$scope.selectedPayment.paymentId) return;
        Swal.fire({
            title: 'Approve this payment?',
            text: 'You are approving the payment. The "Allow resubmit" option only applies when you Reject.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Yes, approve',
            cancelButtonText: 'Cancel'
        }).then(function (result) {
            if (result.isConfirmed) $scope.verifyPaymentFromModal();
        });
    };
    $scope.rejectPaymentFromModal = function () {
        if (!$scope.selectedPayment || !$scope.selectedPayment.paymentId) return;
        var reason = (($scope.paymentModalForm && $scope.paymentModalForm.rejectionReason) || '').trim();
        if (!reason) {
            Swal.fire({ icon: 'warning', title: 'Rejection reason required', text: 'Please enter a reason for rejecting this payment.' });
            return;
        }
        if (reason.length < 5) {
            Swal.fire({ icon: 'warning', title: 'Reason too short', text: 'Please enter at least 5 characters for the rejection reason.' });
            return;
        }
        var allowResubmit = !!($scope.paymentModalForm && $scope.paymentModalForm.allowResubmit);
        var paymentId = $scope.selectedPayment.paymentId;
        SolteiraAndBrandsService.updatePaymentStatus(paymentId, 'REJECTED', reason, allowResubmit).then(function (res) {
            if (res.data && res.data.success) {
                $scope.paymentModalForm.rejectionReason = '';
                $scope.paymentModalForm.allowResubmit = true;
                // Reflect REJECTED status immediately in the payments list.
                if ($scope.paymentsList && $scope.paymentsList.length) {
                    for (var i = 0; i < $scope.paymentsList.length; i++) {
                        var p = $scope.paymentsList[i];
                        if (p && p.paymentId === paymentId) {
                            p.paymentStatus = 'REJECTED';
                            p.rejectionReason = reason;
                            break;
                        }
                    }
                }
                $scope.loadPayments();
                $scope.paymentSuccessSwalPending = true;
                $scope.closePaymentProofModal();
                var msg = allowResubmit ? 'The buyer will be able to resubmit payment.' : 'The buyer cannot resubmit; they will see the rejection reason only.';

                // For final rejections (no resubmit), also ask whether to return items to stock.
                if (!allowResubmit && $scope.paymentDetail && $scope.paymentDetail.orderSummary && $scope.paymentDetail.orderSummary.id) {
                    var orderId = $scope.paymentDetail.orderSummary.id;
                    Swal.fire({
                        title: 'Return items to stock?',
                        text: 'Order will be rejected. Do you want to return these items to active inventory?',
                        icon: 'question',
                        showCancelButton: true,
                        confirmButtonText: 'Yes, return to stock',
                        cancelButtonText: 'No, keep stock as is'
                    }).then(function (choice) {
                        if (choice.isConfirmed) {
                            // Yes → cancel and restore stock.
                            $scope.updateOrderStatusFor(orderId, 'Cancelled', reason, true);
                            Swal.fire({ title: 'Payment rejected & order cancelled', text: msg, icon: 'success', confirmButtonText: 'OK' }).then(function () {
                                $scope.paymentSuccessSwalPending = false;
                                window.location.reload();
                            });
                        } else if (choice.dismiss === 'cancel') {
                            // "No, keep stock as is" → cancel order but do not touch stock.
                            $scope.updateOrderStatusFor(orderId, 'Cancelled', reason, false);
                            Swal.fire({ title: 'Payment rejected & order cancelled', text: msg, icon: 'success', confirmButtonText: 'OK' }).then(function () {
                                $scope.paymentSuccessSwalPending = false;
                                window.location.reload();
                            });
                        } else {
                            // X / backdrop / Esc → abort without cancelling the order.
                            return;
                        }
                    });
                } else {
                    Swal.fire({ title: 'Payment rejected', text: msg, icon: 'success', confirmButtonText: 'OK' }).then(function () {
                        $scope.paymentSuccessSwalPending = false;
                        window.location.reload();
                    });
                }
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };

    $scope.paymentSettingsForm = {
        gcashAccountName: '', gcashAccountNumber: '', gcashQrImageId: null, gcashQrImageUrl: null,
        bankAccountName: '', bankAccountNumber: '', bankQrImageId: null, bankQrImageUrl: null,
        downpaymentPercentage: 30, monthsAllowed: 3,
        meetupLocationName: '', meetupAddress: ''
    };
    $scope.pendingQrType = null;
    $scope.openPaymentSettingsModal = function () {
        SolteiraAndBrandsService.getPaymentSettingsForAdmin().then(function (res) {
            if (res.data && res.data.success) {
                var g = res.data.gcash, b = res.data.bankTransfer;
                $scope.paymentSettingsForm.gcashAccountName = g ? (g.accountName || '') : '';
                $scope.paymentSettingsForm.gcashAccountNumber = g ? (g.accountNumber || '') : '';
                $scope.paymentSettingsForm.gcashQrImageId = g ? g.qrImageId : null;
                $scope.paymentSettingsForm.gcashQrImageUrl = g && g.qrImageUrl ? g.qrImageUrl : null;
                $scope.paymentSettingsForm.bankAccountName = b ? (b.accountName || '') : '';
                $scope.paymentSettingsForm.bankAccountNumber = b ? (b.accountNumber || '') : '';
                $scope.paymentSettingsForm.bankQrImageId = b ? b.qrImageId : null;
                $scope.paymentSettingsForm.bankQrImageUrl = b && b.qrImageUrl ? b.qrImageUrl : null;
            }
        });
        SolteiraAndBrandsService.getLayawaySettingsForAdmin().then(function (res) {
            if (res.data && res.data.success) {
                $scope.paymentSettingsForm.downpaymentPercentage = res.data.downpaymentPercentage != null ? res.data.downpaymentPercentage : 30;
                $scope.paymentSettingsForm.monthsAllowed = res.data.monthsAllowed != null ? res.data.monthsAllowed : 3;
            }
        });
        SolteiraAndBrandsService.getMeetupSettingsForAdmin().then(function (res) {
            if (res.data && res.data.success) {
                $scope.paymentSettingsForm.meetupLocationName = res.data.locationName || '';
                $scope.paymentSettingsForm.meetupAddress = res.data.address || '';
            }
        });
    };
    $scope.triggerQrUpload = function (type) {
        $scope.pendingQrType = type;
        var id = type === 'gcash' ? 'gcashQrUpload' : 'bankQrUpload';
        var el = document.getElementById(id);
        if (el) el.click();
    };
    $scope.onQrFileSelected = function (file, type) {
        if (!file || !type) return;
        SolteiraAndBrandsService.uploadQrImage(file).then(function (res) {
            if (res.data && res.data.success && res.data.imageID) {
                if (type === 'gcash') {
                    $scope.paymentSettingsForm.gcashQrImageId = res.data.imageID;
                    $scope.paymentSettingsForm.gcashQrImageUrl = (res.data.imagePath && res.data.imagePath.indexOf('/') === 0) ? res.data.imagePath : ('/Content/images_uploaded/qr/' + (res.data.imageFileName || ''));
                } else {
                    $scope.paymentSettingsForm.bankQrImageId = res.data.imageID;
                    $scope.paymentSettingsForm.bankQrImageUrl = (res.data.imagePath && res.data.imagePath.indexOf('/') === 0) ? res.data.imagePath : ('/Content/images_uploaded/qr/' + (res.data.imageFileName || ''));
                }
                $scope.$apply();
            } else {
                Swal.fire({ icon: 'error', title: 'Upload failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Upload failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };
    $scope.savePaymentSetting = function (paymentType) {
        var isGcash = paymentType === 'GCASH';
        var accountName = isGcash ? $scope.paymentSettingsForm.gcashAccountName : $scope.paymentSettingsForm.bankAccountName;
        var accountNumber = isGcash ? $scope.paymentSettingsForm.gcashAccountNumber : $scope.paymentSettingsForm.bankAccountNumber;
        var qrImageId = isGcash ? $scope.paymentSettingsForm.gcashQrImageId : $scope.paymentSettingsForm.bankQrImageId;
        SolteiraAndBrandsService.updatePaymentSetting(paymentType, accountName, accountNumber, qrImageId).then(function (res) {
            if (res.data && res.data.success) {
                Swal.fire({ title: (res.data.message || 'Saved'), icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };
    $scope.saveMeetupSetting = function () {
        var locationName = ($scope.paymentSettingsForm.meetupLocationName || '').trim();
        var address = ($scope.paymentSettingsForm.meetupAddress || '').trim();
        if (!locationName) {
            Swal.fire({ icon: 'warning', title: 'Required', text: 'Location name is required.' });
            return;
        }
        SolteiraAndBrandsService.updateMeetupSetting(locationName, address).then(function (res) {
            if (res.data && res.data.success) {
                Swal.fire({ title: 'Meetup location saved', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };
    $scope.saveLayawaySettings = function () {
        var pct = parseFloat($scope.paymentSettingsForm.downpaymentPercentage, 10);
        var months = parseInt($scope.paymentSettingsForm.monthsAllowed, 10);
        if (isNaN(pct) || pct < 1 || pct > 100) {
            Swal.fire({ icon: 'warning', title: 'Invalid', text: 'Downpayment % must be 1-100.' });
            return;
        }
        if (isNaN(months) || months < 1 || months > 24) {
            Swal.fire({ icon: 'warning', title: 'Invalid', text: 'Months allowed must be 1-24.' });
            return;
        }
        SolteiraAndBrandsService.updateLayawaySettings(pct, months).then(function (res) {
            if (res.data && res.data.success) {
                Swal.fire({ title: 'Layaway settings saved', icon: 'success', confirmButtonText: 'OK' });
            } else {
                Swal.fire({ icon: 'error', title: 'Failed', text: (res.data && res.data.message) || 'Please try again.' });
            }
        }, function (err) {
            Swal.fire({ icon: 'error', title: 'Failed', text: (err.data && err.data.message) || 'Please try again.' });
        });
    };

    // ----- Inventory: products from tbl_products (shared with buyer storefront) -----
    $scope.inventoryProducts = [];
    $scope.showArchivedInventory = false;
    $scope.inventorySearchText = '';
    $scope.productCategories = ["Bracelet", "Ring", "Earring", "Necklace"];
    $scope.newProduct = {
        name: "",
        category: "",
        description: "",
        price: null,
        stockQuantity: null,
        stones: "",
        material: "",
        weight: "",
        length: "",
        isBestSeller: false,
        isFeatured: false
    };
    $scope.inventoryPhotoFile = null;
    $scope.inventoryEditPhotoFile = null;
    $scope.editProduct = null;
    $scope.nextProductDisplayId = 'P-001';
    $scope.inventoryTableVisible = true;

    $scope.loadInventoryProducts = function () {
        var showArchived = $scope.showArchivedInventory;
        $scope.inventorySearchText = '';
        
        // Guard against out-of-order responses when the user clicks tabs quickly.
        $scope.inventoryLoadSeq = ($scope.inventoryLoadSeq || 0) + 1;
        var seq = $scope.inventoryLoadSeq;

        // Reset immediately so DataTables never shows stale rows.
        try {
            if ($.fn.DataTable && $.fn.DataTable.isDataTable && $('#inventoryTable').length && $.fn.DataTable.isDataTable('#inventoryTable')) {
                $('#inventoryTable').DataTable().destroy();
            }
        } catch (e) { }
        $scope.inventoryProducts = [];
        $scope.inventoryTableVisible = false;

        SolteiraAndBrandsService.getInventoryProducts(showArchived).then(function (res) {
            if (seq !== $scope.inventoryLoadSeq) return; // stale response
            var list = [];
            var d = res.data;
            if (angular.isArray(d)) {
                list = d;
            } else if (d && d.data && angular.isArray(d.data)) {
                list = d.data;
            }
            list = list || [];

            $scope.inventoryProducts = list;

            if (!showArchived) {
                var maxId = 0;
                angular.forEach($scope.inventoryProducts, function (p) {
                    if (p && p.id && p.id > maxId) maxId = p.id;
                });
                $scope.nextProductDisplayId = 'P-' + ('000' + (maxId + 1)).slice(-3);
            }

            // Re-render the table (ng-if) to guarantee DataTables rebinds to current rows.
            $timeout(function () {
                if (seq !== $scope.inventoryLoadSeq) return; // stale response
                $scope.inventoryTableVisible = true;
                $timeout(function () {
                    if (typeof window.initializeStandardTable !== 'function') return;
                    window.initializeStandardTable('#inventoryTable', 1);
                }, 0);
            }, 0);
        }, function () {
            if (seq !== $scope.inventoryLoadSeq) return; // stale response
            $scope.inventoryProducts = [];
            // Ensure any existing DataTable instance is cleaned up when list becomes empty.
            $timeout(function () {
                try {
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable && $('#inventoryTable').length && $.fn.DataTable.isDataTable('#inventoryTable')) {
                        $('#inventoryTable').DataTable().destroy();
                    }
                } catch (e) { }
            }, 0);
            $timeout(function () {
                if (seq !== $scope.inventoryLoadSeq) return; // stale response
                $scope.inventoryTableVisible = true;
            }, 0);
        });
    };

    $scope.inventoryFileValidationMessage = '';
    $scope.inventoryEditFileValidationMessage = '';

    $scope.onInventoryFileSelected = function (files) {
        $scope.inventoryFileValidationMessage = '';
        if (files && files.length > 0) {
            var file = files[0];
            var fn = (file.name || '').toLowerCase();
            if (fn.indexOf('.jpg') === -1 && fn.indexOf('.jpeg') === -1 && fn.indexOf('.png') === -1) {
                $scope.$applyAsync(function () {
                    $scope.inventoryPhotoFile = null;
                    $scope.inventoryFileValidationMessage = 'Must submit JPG or PNG only. Maximum size 5MB.';
                });
                Swal.fire({ icon: 'warning', title: 'Invalid file', text: 'Must submit JPG or PNG only. Maximum size 5MB.' });
                return;
            }
            if (file.size > 5 * 1024 * 1024) {
                $scope.$applyAsync(function () {
                    $scope.inventoryPhotoFile = null;
                    $scope.inventoryFileValidationMessage = 'Must submit JPG or PNG only. Maximum size 5MB.';
                });
                Swal.fire({ icon: 'warning', title: 'File too large', text: 'File must be under 5MB.' });
                return;
            }
            $scope.$applyAsync(function () {
                $scope.inventoryPhotoFile = file;
            });
        } else {
            $scope.$applyAsync(function () {
                $scope.inventoryPhotoFile = null;
            });
        }
    };

    $scope.onInventoryEditFileSelected = function (files) {
        $scope.inventoryEditFileValidationMessage = '';
        if (files && files.length > 0) {
            var file = files[0];
            var fn = (file.name || '').toLowerCase();
            if (fn.indexOf('.jpg') === -1 && fn.indexOf('.jpeg') === -1 && fn.indexOf('.png') === -1) {
                $scope.$applyAsync(function () {
                    $scope.inventoryEditPhotoFile = null;
                    $scope.inventoryEditFileValidationMessage = 'Must submit JPG or PNG only. Maximum size 5MB.';
                });
                Swal.fire({ icon: 'warning', title: 'Invalid file', text: 'Must submit JPG or PNG only. Maximum size 5MB.' });
                return;
            }
            if (file.size > 5 * 1024 * 1024) {
                $scope.$applyAsync(function () {
                    $scope.inventoryEditPhotoFile = null;
                    $scope.inventoryEditFileValidationMessage = 'Must submit JPG or PNG only. Maximum size 5MB.';
                });
                Swal.fire({ icon: 'warning', title: 'File too large', text: 'File must be under 5MB.' });
                return;
            }
            $scope.$applyAsync(function () {
                $scope.inventoryEditPhotoFile = file;
            });
        } else {
            $scope.$applyAsync(function () {
                $scope.inventoryEditPhotoFile = null;
            });
        }
    };

    $scope.createInventoryProduct = function () {
        $scope.inventoryFileValidationMessage = '';
        if (!$scope.newProduct.name || !$scope.newProduct.category || !$scope.newProduct.price || !$scope.newProduct.stockQuantity) {
            Swal.fire({
                title: 'Missing required fields',
                text: 'Please fill in necessary fields *',
                icon: 'warning',
                confirmButtonText: 'OK'
            });
            return;
        }
        var priceNum = parseFloat($scope.newProduct.price);
        if (isNaN(priceNum) || priceNum < 0) {
            Swal.fire({ title: 'Invalid price', text: 'Selling price cannot be negative.', icon: 'warning', confirmButtonText: 'OK' });
            return;
        }
        var stockNum = parseInt($scope.newProduct.stockQuantity, 10);
        if (isNaN(stockNum) || stockNum < 0) {
            Swal.fire({ title: 'Invalid stock', text: 'Stock quantity cannot be negative.', icon: 'warning', confirmButtonText: 'OK' });
            return;
        }
        if (!$scope.newProduct.stones || !$scope.newProduct.material) {
            Swal.fire({
                title: 'Missing required fields',
                text: 'Please fill in necessary fields *',
                icon: 'warning',
                confirmButtonText: 'OK'
            });
            return;
        }
        if (!$scope.inventoryPhotoFile) {
            Swal.fire({
                title: 'Photo required',
                text: 'Please upload a product photo (PNG or JPG, less than 5MB).',
                icon: 'warning',
                confirmButtonText: 'OK'
            });
            return;
        }

        var doCreate = function (imageId) {
            var payload = {
                Name: $scope.newProduct.name,
                Category: $scope.newProduct.category,
                Description: $scope.newProduct.description,
                Price: parseFloat($scope.newProduct.price) || 0,
                Material: $scope.newProduct.material,
                Stones: $scope.newProduct.stones,
                Weight: $scope.newProduct.weight,
                Length: $scope.newProduct.length,
                StockQuantity: parseInt($scope.newProduct.stockQuantity) || 0,
                ImageId: imageId,
                IsBestSeller: !!$scope.newProduct.isBestSeller,
                IsFeatured: !!$scope.newProduct.isFeatured
            };

            SolteiraAndBrandsService.createProduct(payload).then(function (res) {
                if (res.data && res.data.success) {
                    $('#inventoryAddItemModal').modal('hide');
                    $scope.newProduct = { name: '', category: '', description: '', price: null, stockQuantity: null, stones: '', material: '', weight: '', length: '', isBestSeller: false, isFeatured: false };
                    $scope.inventoryPhotoFile = null;
                    $scope.inventoryFileValidationMessage = '';
                    Swal.fire({
                        title: 'Inventory item added!',
                        text: 'Click OK to update the inventory list.',
                        icon: 'success',
                        iconColor: '#FFFBE8',
                        background: '#8eb078',
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#FFFBE8',
                        buttonsStyling: true,
                        customClass: { title: 'status-title', confirmButton: 'status-button' }
                    }).then(function () {
                        $timeout(function () { $scope.loadInventoryProducts(); }, 0);
                    });
                } else {
                    Swal.fire({
                        title: 'Unable to add item.',
                        text: (res.data && res.data.message) || 'Please try again.',
                        icon: 'error',
                        confirmButtonText: 'OK'
                    });
                }
            }, function () {
                Swal.fire({
                    title: 'Unable to add item.',
                    text: 'Please try again.',
                    icon: 'error',
                    confirmButtonText: 'OK'
                });
            });
        };

        if ($scope.inventoryPhotoFile) {
            SolteiraAndBrandsService.uploadProductImage($scope.inventoryPhotoFile).then(function (res) {
                console.log('Create uploadProductImage response:', res.data);
                if (res.data && res.data.success && res.data.imageID) {
                    doCreate(res.data.imageID);
                } else {
                    $scope.inventoryFileValidationMessage = (res.data && res.data.message) ? res.data.message : 'Must be PNG/JPG, less than 5MB.';
                    Swal.fire({
                        title: 'Image upload failed',
                        text: 'Must be PNG/JPG, less than 5MB.',
                        icon: 'error',
                        confirmButtonText: 'OK'
                    });
                }
            }, function () {
                $scope.inventoryFileValidationMessage = 'Must be PNG/JPG, less than 5MB.';
                Swal.fire({
                    title: 'Image upload failed',
                    text: 'Must be PNG/JPG, less than 5MB.',
                    icon: 'error',
                    confirmButtonText: 'OK'
                });
            });
        } else {
            doCreate(null);
        }
    };

    $scope.viewPhotoItem = null;
    /** Full product details modal (inventory): image + specs — opened from Action eye icon. */
    $scope.openViewPhotoModal = function (item) {
        if (!item) return;
        $scope.viewPhotoItem = item;
        $('#inventoryViewPhotoModal').modal('show');
    };
    $scope.closeViewPhotoAndEdit = function (item) {
        $('#inventoryViewPhotoModal').modal('hide');
        $scope.viewPhotoItem = null;
    };
    /** Photo column only: same-page fullscreen overlay, image only (no details modal). */
    $scope.openInventoryPhotoOnly = function (item) {
        if (!item) return;
        var url = item.imageUrl;
        if (url == null || String(url).trim() === '') {
            url = $scope.inventoryDefaultPhotoUrl || '/Content/img/shared/default-product.svg';
        }
        $scope.openImagePreview(url);
    };
    $scope.openImagePreview = function (src) {
        if (!src) return;
        var overlay = document.createElement('div');
        overlay.className = 'image-overlay';
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        overlay.setAttribute('aria-label', 'Photo preview');
        var img = document.createElement('img');
        img.src = src;
        img.alt = '';
        overlay.appendChild(img);
        function close() {
            overlay.remove();
            document.removeEventListener('keydown', onKey);
        }
        function onKey(e) {
            if (e.key === 'Escape') close();
        }
        overlay.onclick = function (e) {
            if (e.target === overlay) close();
        };
        document.addEventListener('keydown', onKey);
        document.body.appendChild(overlay);
    };
    $scope.formatProductPrice = function (price) {
        if (price == null || price === undefined) return '';
        return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(price);
    };
    /** Safe display for inventory text fields: empty string for null/blank/dash-only/mojibake (no placeholder glyphs). */
    $scope.displayInventoryField = function (v) {
        if (v == null || v === '') return '';
        var s = String(v).trim();
        if (!s) return '';
        if (/^[\u002D\u2010\u2011\u2012\u2013\u2014\u2015\u2212\uFE58\uFE63\uFF0D\u00AD\s]+$/.test(s)) return '';
        if (/^â€/.test(s)) return '';
        return s;
    };
    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    $scope.getViewProductDescriptionHtml = function (item) {
        if (!item || item.description == null) return $sce.trustAsHtml('');
        var safe = escapeHtml(String(item.description)).replace(/\n/g, '<br>');
        return $sce.trustAsHtml(safe);
    };
    $scope.openEditProduct = function (item) {
        if (!item) return;
        $scope.editProduct = angular.copy(item);
        $scope.inventoryEditPhotoFile = null;
        $('#inventoryEditItemModal').modal('show');
    };

    $scope.updateInventoryProduct = function () {
        if (!$scope.editProduct || !$scope.editProduct.id) return;
        $scope.inventoryEditFileValidationMessage = '';
        if (!$scope.editProduct.name || !$scope.editProduct.category || !$scope.editProduct.price || !$scope.editProduct.stockQuantity) {
            Swal.fire({
                title: 'Missing required fields',
                text: 'Please fill in necessary fields *',
                icon: 'warning',
                confirmButtonText: 'OK'
            });
            return;
        }
        if (!$scope.editProduct.stones || !$scope.editProduct.material) {
            Swal.fire({
                title: 'Missing required fields',
                text: 'Please fill in necessary fields *',
                icon: 'warning',
                confirmButtonText: 'OK'
            });
            return;
        }

        var doUpdate = function (imageId) {
            var payload = {
                Id: $scope.editProduct.id,
                Name: $scope.editProduct.name,
                Category: $scope.editProduct.category,
                Description: $scope.editProduct.description,
                Price: parseFloat($scope.editProduct.price) || 0,
                Material: $scope.editProduct.material,
                Stones: $scope.editProduct.stones,
                Weight: $scope.editProduct.weight,
                Length: $scope.editProduct.length,
                StockQuantity: parseInt($scope.editProduct.stockQuantity) || 0,
                ImageId: imageId,
                IsBestSeller: !!$scope.editProduct.isBestSeller,
                IsFeatured: !!$scope.editProduct.isFeatured
            };

            SolteiraAndBrandsService.updateProduct(payload).then(function (res) {
                if (res.data && res.data.success) {
                    $('#inventoryEditItemModal').modal('hide');
                    $scope.editProduct = null;
                    $scope.inventoryEditPhotoFile = null;
                    Swal.fire({
                        title: 'Inventory item updated!',
                        text: 'Inventory has been updated.',
                        icon: 'success',
                        iconColor: '#FFFBE8',
                        background: '#8eb078',
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#FFFBE8',
                        buttonsStyling: true,
                        customClass: { title: 'status-title', confirmButton: 'status-button' }
                    }).then(function () {
                        // Refresh inventory list so the edited stockQuantity is visible immediately.
                        $scope.loadInventoryProducts();
                    });
                } else {
                    Swal.fire({
                        title: 'Unable to update item.',
                        text: (res.data && res.data.message) || 'Please try again.',
                        icon: 'error',
                        confirmButtonText: 'OK'
                    });
                }
            }, function () {
                Swal.fire({
                    title: 'Unable to update item.',
                    text: 'Please try again.',
                    icon: 'error',
                    confirmButtonText: 'OK'
                });
            });
        };

        if ($scope.inventoryEditPhotoFile) {
            SolteiraAndBrandsService.uploadProductImage($scope.inventoryEditPhotoFile).then(function (res) {
                console.log('Edit uploadProductImage response:', res.data);
                if (res.data && res.data.success && res.data.imageID) {
                    doUpdate(res.data.imageID);
                } else {
                    $scope.inventoryEditFileValidationMessage = (res.data && res.data.message) ? res.data.message : 'Must be PNG/JPG, less than 5MB.';
                    Swal.fire({
                        title: 'Image upload failed',
                        text: 'Must be PNG/JPG, less than 5MB.',
                        icon: 'error',
                        confirmButtonText: 'OK'
                    });
                }
            }, function () {
                $scope.inventoryEditFileValidationMessage = 'Must be PNG/JPG, less than 5MB.';
                Swal.fire({
                    title: 'Image upload failed',
                    text: 'Must be PNG/JPG, less than 5MB.',
                    icon: 'error',
                    confirmButtonText: 'OK'
                });
            });
        } else {
            doUpdate(null);
        }
    };

    // Initialize when inventory table is present
    $timeout(function () {
        if ($('#inventoryTable').length) {
            // If the URL includes `?archived=true`, open the inventory page in the archived tab.
            // This is used by the system admin sidebar navigation for "Archive".
            try {
                var params = new URLSearchParams(window.location.search || '');
                var archivedParam = params.get('archived');
                if (archivedParam != null) {
                    $scope.showArchivedInventory = archivedParam === 'true' || archivedParam === '1';
                }
            } catch (e) { }
            $scope.loadInventoryProducts();
        }
    }, 0);

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


    // Restore order from Archive page (order = { id, orderNumber, ... })
    $scope.restoreAlert = function (order) {
        if (!order || !order.id) return;
        Swal.fire({
            title: 'Restore Confirmation',
            text: 'Please confirm you wish to restore order ' + (order.orderNumber || order.id) + '. It will reappear on the Orders page.',
            icon: 'info',
            iconColor: '#8eb078',
            showCancelButton: true,
            confirmButtonText: 'Restore',
            confirmButtonColor: '#8eb078',
            cancelButtonText: 'Cancel',
            background: '#FFFBE8',
        }).then(function (result) {
            if (result.isConfirmed) {
                SolteiraAndBrandsService.restoreOrder(order.id).then(function (res) {
                    if (res.data && res.data.success) {
                        Swal.fire({ title: 'Order restored successfully!', icon: 'success', confirmButtonColor: '#8eb078' });
                        $scope.loadArchiveOrders();
                    } else {
                        Swal.fire({ icon: 'error', title: 'Restore failed', text: (res.data && res.data.message) || 'Please try again.' });
                    }
                }, function (err) {
                    Swal.fire({ icon: 'error', title: 'Restore failed', text: (err.data && err.data.message) || 'Please try again.' });
                });
            }
        });
    };

    // Archive order from Orders page (ADMIN only). orderId = order.id
    $scope.archiveOrderConfirm = function (order) {
        if (!order || !order.id) return;
        Swal.fire({
            title: 'Archive Confirmation',
            text: 'This order will be moved to Archive and hidden from Orders, Payments, and Delivery pages. Buyer will still see it in My Orders (view-only).',
            icon: 'info',
            iconColor: '#8eb078',
            showCancelButton: true,
            confirmButtonText: 'Archive',
            confirmButtonColor: '#8eb078',
            cancelButtonText: 'Cancel',
            background: '#FFFBE8',
        }).then(function (result) {
            if (result.isConfirmed) {
                SolteiraAndBrandsService.archiveOrder(order.id).then(function (res) {
                    if (res.data && res.data.success) {
                        Swal.fire({ title: 'Order archived successfully!', icon: 'success', confirmButtonColor: '#8eb078' });
                        $scope.loadOrders();
                    } else {
                        Swal.fire({ icon: 'error', title: 'Archive failed', text: (res.data && res.data.message) || 'Please try again.' });
                    }
                }, function (err) {
                    Swal.fire({ icon: 'error', title: 'Archive failed', text: (err.data && err.data.message) || 'Please try again.' });
                });
            }
        });
    };

    //for when archive/restore is clicked in INVENTORY Page
    $scope.onFeaturedCheckboxChange = function (item) {
        if (!item || !item.id) return;
        SolteiraAndBrandsService.toggleFeatured(item.id).then(function (res) {
            if (res.data && res.data.success) {
                item.isFeatured = res.data.isFeatured;
                if ($scope.viewPhotoItem && $scope.viewPhotoItem.id === item.id) {
                    $scope.viewPhotoItem.isFeatured = res.data.isFeatured;
                }
            } else {
                item.isFeatured = !item.isFeatured;
                Swal.fire({
                    title: 'Unable to update featured status.',
                    text: (res.data && res.data.message) || 'Please try again.',
                    icon: 'error',
                    confirmButtonText: 'OK'
                });
            }
        }, function () {
            item.isFeatured = !item.isFeatured;
            Swal.fire({
                title: 'Unable to update featured status.',
                text: 'Please try again.',
                icon: 'error',
                confirmButtonText: 'OK'
            });
        });
    };

    $scope.inventoryArchiveAlert = function (item) {
        if (!item || !item.id) return;

        var isArchiving = !$scope.showArchivedInventory;
        var title = isArchiving ? 'Archive Confirmation' : 'Restore Confirmation';
        var text = isArchiving ? 'Please confirm you wish to archive this inventory item'
                               : 'Please confirm you wish to restore this inventory item';
        var confirmLabel = isArchiving ? 'Archive' : 'Restore';
        var successTitle = isArchiving ? 'Inventory item archived successfully!'
                                       : 'Inventory item restored successfully!';

        Swal.fire({
            title: title,
            text: text,
            icon: 'info',
            iconColor: '#8eb078',
            showCancelButton: true,
            confirmButtonText: confirmLabel,
            confirmButtonColor: '#8eb078',
            cancelButtonText: 'Cancel',
            background: '#FFFBE8',
        }).then((result) => {
            if (result.isConfirmed) {
                SolteiraAndBrandsService.toggleProductArchive(item.id).then(function (res) {
                    if (res.data && res.data.success) {
                        Swal.fire({
                            title: successTitle,
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
                        }).then(function () {
                            // Refresh after user confirms, so DataTable re-init doesn't race with SweetAlert overlay.
                            $timeout(function () { $scope.loadInventoryProducts(); }, 0);
                        });
                    } else {
                        Swal.fire({
                            title: 'Unable to update item.',
                            text: (res.data && res.data.message) || 'Please try again.',
                            icon: 'error',
                            confirmButtonText: 'OK'
                        });
                    }
                }, function () {
                    Swal.fire({
                        title: 'Unable to update item.',
                        text: 'Please try again.',
                        icon: 'error',
                        confirmButtonText: 'OK'
                    });
                });
            }
        });
    }
    //---------Sweet Alert Dialogue Box end---------

    //----------Charts (initialized empty; filled by loadDashboardData) --------- 
    $scope.lineLabels = [];
    $scope.series = ['Total Sales (\u20B1)'];
    $scope.lineData = [[]];
    $scope.onClick = function (points, evt) {
        console.log(points, evt);
    };
    $scope.datasetOverride = [{ yAxisID: 'y-axis-1' }];
    $scope.lineOptions = {
        responsive: true,
        maintainAspectRatio: true,
        aspectRatio: 2,
        scales: {
            yAxes: [{
                id: 'y-axis-1',
                type: 'linear',
                display: true,
                position: 'left'
            }]
        }
    };
    // Green theme #87AE73 (rgb 135, 174, 115) for charts
    $scope.lineChartColors = [{
        backgroundColor: 'rgba(135, 174, 115, 0.25)',
        borderColor: 'rgb(135, 174, 115)',
        pointBackgroundColor: 'rgb(135, 174, 115)',
        pointBorderColor: '#fff',
        pointHoverBackgroundColor: 'rgb(135, 174, 115)',
        pointHoverBorderColor: '#fff'
    }];
    $scope.pieLabels = [];
    $scope.pieData = [];
    // Set Chart.js default pie segment colors so pie uses blue, green, orange, red (Ring, Earring, Necklace, Bracelet)
    if (typeof Chart !== 'undefined' && Chart.defaults && Chart.defaults.global) {
        Chart.defaults.global.datasets = Chart.defaults.global.datasets || {};
        Chart.defaults.global.datasets.pie = Chart.defaults.global.datasets.pie || {};
        Chart.defaults.global.datasets.pie.backgroundColor = [
            'rgba(54, 162, 235, 0.85)',   // blue - Ring
            'rgba(135, 174, 115, 0.85)',  // green - Earring
            'rgba(255, 159, 64, 0.85)',   // orange - Necklace
            'rgba(255, 99, 132, 0.85)'    // red - Bracelet
        ];
        // Ensure datalabels plugin shows on pie/doughnut (angular-chart may not pass plugins through)
        Chart.defaults.global.plugins = Chart.defaults.global.plugins || {};
        Chart.defaults.global.plugins.datalabels = Chart.defaults.global.plugins.datalabels || {};
        Chart.defaults.global.plugins.datalabels.display = function (context) {
            var value = context.dataset && context.dataset.data && context.dataset.data[context.dataIndex];
            return value != null && Number(value) > 0;
        };
        Chart.defaults.global.plugins.datalabels.color = '#fff';
        Chart.defaults.global.plugins.datalabels.font = { size: 13, weight: 'bold' };
        Chart.defaults.global.plugins.datalabels.formatter = function (value, ctx) {
            var data = ctx.dataset && ctx.dataset.data;
            if (!data || data.length === 0) return value;
            var total = data.reduce(function (a, b) { return a + b; }, 0);
            var pct = total === 0 ? 0 : Math.round((value / total) * 100);
            var numStr = typeof value === 'number' && value % 1 !== 0 ? value.toFixed(2) : String(value);
            return pct + '%\n(' + numStr + ')';
        };
    }
    $scope.pieOptions = {
        responsive: true,
        maintainAspectRatio: true,
        aspectRatio: 1,
        legend: { display: true },
        tooltips: { enabled: true },
        plugins: {
            datalabels: {
                display: function (context) {
                    var value = context.dataset && context.dataset.data && context.dataset.data[context.dataIndex];
                    return value != null && Number(value) > 0;
                },
                color: '#fff',
                font: { size: 13, weight: 'bold' },
                formatter: function (value, ctx) {
                    var data = ctx.dataset && ctx.dataset.data;
                    if (!data || data.length === 0) return value;
                    var total = data.reduce(function (a, b) { return a + b; }, 0);
                    var pct = total === 0 ? 0 : Math.round((value / total) * 100);
                    var numStr = typeof value === 'number' && value % 1 !== 0 ? value.toFixed(2) : String(value);
                    return pct + '%\n(' + numStr + ')';
                }
            }
        }
    };

    // Dashboard stats (cards)
    $scope.dashboardStats = {
        totalSales: 0,
        productSold: 0,
        pendingOrders: 0,
        approvedOrders: 0,
        shippedOrders: 0
    };
    $scope.dashboardDate = '';
    $scope.dashboardRange = 'today';
    $scope.customStartDate = '';
    $scope.customEndDate = '';

    $scope.loadDashboardData = function () {
        var range = $scope.dashboardRange || 'lastmonth';
        var startDate = range === 'custom' ? $scope.customStartDate : null;
        var endDate = range === 'custom' ? $scope.customEndDate : null;
        SolteiraAndBrandsService.getDashboardData(range, startDate, endDate).then(function (res) {
            if (res.data && res.data.success) {
                var stats = res.data.stats || {};
                $scope.dashboardStats = {
                    totalSales: stats.totalSales || 0,
                    productSold: stats.productSold || 0,
                    pendingOrders: stats.pendingOrders || 0,
                    approvedOrders: stats.approvedOrders || 0,
                    shippedOrders: stats.shippedOrders || 0
                };
                var daily = res.data.dailySales || {};
                var labels = daily.labels || [];
                var data = daily.data || [];
                // Line chart needs at least 2 points to draw; duplicate single point so "Today" and single-day ranges show
                if (labels.length === 1 && data.length === 1) {
                    labels = [labels[0], labels[0]];
                    data = [data[0], data[0]];
                }
                $scope.lineLabels = labels;
                $scope.lineData = [data];
                var byCat = res.data.salesByCategory || {};
                $scope.pieLabels = byCat.labels || [];
                $scope.pieData = byCat.data || [];
                $scope.dashboardInventoryStock = res.data.inventoryStock || [];
                $scope.dashboardTopSelling = res.data.topSellingProducts || [];
                var now = new Date();
                $scope.dashboardDate = now.toLocaleDateString('en-US', { month: '2-digit', day: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
            }
        }, function () {
            $scope.lineLabels = [];
            $scope.lineData = [[]];
            $scope.pieLabels = [];
            $scope.pieData = [];
            $scope.dashboardInventoryStock = [];
            $scope.dashboardTopSelling = [];
        });
    };

    $scope.onDashboardRangeChange = function () {
        if ($scope.dashboardRange === 'custom') {
            var today = new Date();
            var lastMonth = new Date(today);
            lastMonth.setDate(lastMonth.getDate() - 30);
            $scope.customEndDate = today.toISOString().slice(0, 10);
            $scope.customStartDate = lastMonth.toISOString().slice(0, 10);
        } else {
            $scope.loadDashboardData();
        }
    };

    $scope.applyCustomRange = function () {
        if ($scope.dashboardRange === 'custom' && $scope.customStartDate && $scope.customEndDate) {
            $scope.loadDashboardData();
        }
    };

    $scope.dashboardInventoryStock = [];
    $scope.dashboardTopSelling = [];
    $scope.getStockStatus = function (quantity) {
        if (quantity == null || quantity === undefined) return 'In Stock';
        return quantity < 3 ? 'Low Stock' : 'In Stock';
    };
    $scope.dashboardDefaultProductImage = '/Content/img/shared/default-product.svg';
    //----------Charts end---------

    //----------Dashboard Page----------

    //----------Dashboard Page End----------

    //----------Activity Logs Page----------
    $scope.activityLogsList = [];
    $scope.logsFilterUser = '';
    $scope.logsFilterRole = '';
    $scope.logsFilterAction = '';
    $scope.logsFilterDateFrom = '';
    $scope.logsFilterDateTo = '';

    function toDateOnlyString(val) {
        if (!val) return '';
        if (typeof val === 'string') return val.substring(0, 10);
        if (val instanceof Date && !isNaN(val.getTime())) {
            var y = val.getFullYear(), m = val.getMonth() + 1, d = val.getDate();
            return y + '-' + (m < 10 ? '0' : '') + m + '-' + (d < 10 ? '0' : '') + d;
        }
        return '';
    }

    $scope.getFilteredActivityLogs = function () {
        var list = $scope.activityLogsList || [];
        var user = ($scope.logsFilterUser || '').toLowerCase().trim();
        var role = ($scope.logsFilterRole || '').trim();
        var action = ($scope.logsFilterAction || '').toLowerCase().trim();
        var dateFrom = toDateOnlyString($scope.logsFilterDateFrom);
        var dateTo = toDateOnlyString($scope.logsFilterDateTo);

        return list.filter(function (log) {
            if (user && (log.username || '').toLowerCase().indexOf(user) === -1) return false;
            if (role && (log.roleName || '').trim() !== role) return false;
            if (action && (log.action || '').toLowerCase().indexOf(action) === -1) return false;
            if (dateFrom || dateTo) {
                var logDate = (log.createdAtString || '').substring(0, 10);
                if (dateFrom && logDate < dateFrom) return false;
                if (dateTo && logDate > dateTo) return false;
            }
            return true;
        });
    };

    $scope.loadActivityLogs = function () {
        SolteiraAndBrandsService.getActivityLogs().then(function (res) {
            if (res.data && res.data.success && angular.isArray(res.data.data)) {
                var raw = res.data.data;
                $scope.activityLogsList = raw.map(function (l) {
                    return {
                        username: l.username != null ? l.username : (l.Username != null ? l.Username : ''),
                        roleName: l.roleName != null ? l.roleName : (l.RoleName != null ? l.RoleName : ''),
                        action: l.action != null ? l.action : (l.Action != null ? l.Action : ''),
                        module: l.module != null ? l.module : (l.Module != null ? l.Module : ''),
                        entityId: l.entityId != null ? l.entityId : (l.EntityId != null ? l.EntityId : null),
                        details: l.details != null ? l.details : (l.Details != null ? l.Details : null),
                        createdAtString: l.createdAtString != null ? l.createdAtString : (l.CreatedAtString != null ? l.CreatedAtString : '')
                    };
                });
            } else {
                $scope.activityLogsList = [];
            }
        }, function () {
            $scope.activityLogsList = [];
        });
    };
    $scope.getActivityLogRoleClass = function (roleName) {
        if (!roleName) return 'bg-secondary';
        var r = (roleName || '').toUpperCase();
        if (r === 'ADMIN') return 'bg-danger';
        if (r === 'MANAGER') return 'bg-warning text-dark';
        if (r === 'STAFF') return 'bg-info text-dark';
        return 'bg-secondary';
    };
    $scope.getActivityLogActionClass = function (action) {
        if (!action) return '';
        var a = (action || '').toLowerCase();
        if (a.indexOf('archive') !== -1 && a.indexOf('unarchive') === -1) return 'text-danger';
        if (a.indexOf('unarchive') !== -1 || a.indexOf('add') !== -1) return 'text-success';
        return 'text-primary';
    };
    //----------Activity Logs Page End----------

    //----------Sales Reports Page----------
    $scope.salesReportRows = [];
    $scope.salesReportGrandTotal = 0;
    $scope.reportRange = 'all';
    $scope.customReportStart = '';
    $scope.customReportEnd = '';
    $scope.reportCategory = 'All Categories';
    $scope.reportFiltersApplied = false;

    $scope.onReportsPageInit = function () {
        $scope.reportFiltersApplied = false;
        $scope.loadSalesReport();
    };

    $scope.loadSalesReport = function () {
        var range = $scope.reportRange || 'all';
        var startDate = range === 'custom' ? ($scope.customReportStart || '') : null;
        var endDate = range === 'custom' ? ($scope.customReportEnd || '') : null;
        var cat = $scope.reportCategory || 'All Categories';
        if (range === 'custom' && (!startDate || !endDate)) {
            $scope.salesReportRows = [];
            $scope.salesReportGrandTotal = 0;
            return;
        }
        SolteiraAndBrandsService.getSalesReport(range, startDate || null, endDate || null, cat === 'All Categories' ? null : cat).then(function (res) {
            if (res.data && res.data.success) {
                var raw = res.data.rows || [];
                $scope.salesReportRows = raw.map(function (r) {
                    return {
                        date: r.date != null ? r.date : (r.Date != null ? r.Date : ''),
                        orderNumber: r.orderNumber != null ? r.orderNumber : (r.OrderNumber != null ? r.OrderNumber : ''),
                        itemName: r.itemName != null ? r.itemName : (r.ItemName != null ? r.ItemName : ''),
                        category: r.category != null ? r.category : (r.Category != null ? r.Category : ''),
                        qty: r.qty != null ? r.qty : (r.Qty != null ? r.Qty : 0),
                        unitPrice: r.unitPrice != null ? r.unitPrice : (r.UnitPrice != null ? r.UnitPrice : 0),
                        total: r.total != null ? r.total : (r.Total != null ? r.Total : 0)
                    };
                });
                $scope.salesReportGrandTotal = res.data.grandTotal != null ? res.data.grandTotal : 0;
                $scope.reportFiltersApplied = true;
            }
        }, function () {
            $scope.salesReportRows = [];
            $scope.salesReportGrandTotal = 0;
        });
    };

    $scope.applyReportFilters = function () {
        if ($scope.reportRange === 'custom') {
            if (!$scope.customReportStart || !$scope.customReportEnd) {
                Swal.fire({ icon: 'warning', title: 'Select dates', text: 'Please choose both start and end date for custom range.' });
                return;
            }
            if ($scope.customReportStart > $scope.customReportEnd) {
                Swal.fire({ icon: 'warning', title: 'Invalid range', text: 'Start date must be before or equal to end date.' });
                return;
            }
        }
        $scope.loadSalesReport();
    };

    $scope.canExportSalesReport = function () {
        if ($scope.reportRange !== 'custom') return true;
        return !!($scope.customReportStart && $scope.customReportEnd && $scope.reportFiltersApplied);
    };

    $scope.onReportRangeChange = function () {
        if ($scope.reportRange === 'custom') {
            var today = new Date();
            var lastMonth = new Date(today);
            lastMonth.setDate(lastMonth.getDate() - 30);
            $scope.customReportEnd = today.toISOString().slice(0, 10);
            $scope.customReportStart = lastMonth.toISOString().slice(0, 10);
            $scope.reportFiltersApplied = false;
        } else {
            $scope.reportFiltersApplied = false;
            $scope.loadSalesReport();
        }
    };

    $scope.formatReportCurrency = function (val) {
        if (val == null) return 'P 0.00';
        var n = parseFloat(val);
        return 'P ' + (isNaN(n) ? '0.00' : n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
    };

    function formatPdfCurrency(val) {
        if (val == null) return 'P 0.00';
        var n = parseFloat(val);
        return 'P ' + (isNaN(n) ? '0.00' : n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
    }

    function getReportPeriodLabel() {
        var range = ($scope.reportRange || 'all').toLowerCase();
        if (range === 'all') return 'All time';
        if (range === 'today') return 'Today';
        if (range === 'lastweek') return 'Last week';
        if (range === 'lastmonth') return 'Last month';
        if (range === 'custom' && $scope.customReportStart && $scope.customReportEnd)
            return $scope.customReportStart + ' to ' + $scope.customReportEnd;
        return range === 'custom' ? 'Custom range' : 'All time';
    }

    $scope.exportSalesReportPdf = function () {
        if (!$scope.canExportSalesReport()) {
            Swal.fire({ icon: 'warning', title: 'Apply filters first', text: 'For custom range, set both dates and click Apply before exporting.' });
            return;
        }
        if (typeof pdfMake === 'undefined') {
            alert('PDF library not loaded.');
            return;
        }
        var rows = $scope.salesReportRows || [];
        var grandTotal = $scope.salesReportGrandTotal != null ? $scope.salesReportGrandTotal : 0;
        var generatedAt = new Date().toLocaleString('en-PH', { dateStyle: 'medium', timeStyle: 'short' });
        var periodLabel = getReportPeriodLabel();
        var categoryLabel = ($scope.reportCategory || 'All Categories') === 'All Categories' ? 'All Categories' : ($scope.reportCategory || 'All Categories');

        var tableBody = [
            [
                { text: 'Date', style: 'tableHeader', alignment: 'center' },
                { text: 'Order ID', style: 'tableHeader', alignment: 'center' },
                { text: 'Item Name', style: 'tableHeader', alignment: 'left' },
                { text: 'Category', style: 'tableHeader', alignment: 'center' },
                { text: 'Qty', style: 'tableHeader', alignment: 'center' },
                { text: 'Unit Price', style: 'tableHeader', alignment: 'right' },
                { text: 'Total', style: 'tableHeader', alignment: 'right' }
            ]
        ];
        rows.forEach(function (r) {
            tableBody.push([
                { text: r.date || '', alignment: 'center' },
                { text: r.orderNumber || '', alignment: 'center' },
                { text: (r.itemName || '').substring(0, 40), alignment: 'left' },
                { text: r.category || '', alignment: 'center' },
                { text: String(r.qty != null ? r.qty : ''), alignment: 'center' },
                { text: formatPdfCurrency(r.unitPrice), alignment: 'right' },
                { text: formatPdfCurrency(r.total), alignment: 'right' }
            ]);
        });

        var docDefinition = {
            pageSize: 'A4',
            pageMargins: [40, 90, 40, 60],

            // CUSTOM CORPORATE HEADER 
            header: function (currentPage, pageCount, pageSize) {
                return {
                    margin: [40, 25, 40, 0],
                    stack: [
                        {
                            columns: [
                                {
                                    image: 'companyLogo',
                                    width: 60
                                },
                                {
                                    stack: [
                                        { text: 'SOLTEIRA & BRANDS', style: 'companyNameMain' },
                                        { text: 'SALES PERFORMANCE REPORT', fontSize: 9, color: '#999999', letterSpacing: 1 }
                                    ],
                                    alignment: 'center',
                                    width: '*'
                                },
                                {
                                    text: 'CONFIDENTIAL',
                                    color: '#2D5A27',
                                    fontSize: 9,
                                    bold: true,
                                    alignment: 'right',
                                    width: 80,
                                    margin: [0, 10, 0, 0]
                                }
                            ]
                        },
                        // Beige Divider Line
                        {
                            canvas: [{ type: 'line', x1: 0, y1: 10, x2: pageSize.width - 80, y2: 10, lineWidth: 2, lineColor: '#F5F5DC' }]
                        }
                    ]
                };
            },

            // CUSTOM CORPORATE FOOTER 
            footer: function (currentPage, pageCount) {
                return {
                    margin: [40, 10, 40, 0],
                    stack: [
                        // Beige Divider Line
                        { canvas: [{ type: 'line', x1: 0, y1: 0, x2: 515, y2: 0, lineWidth: 1.5, lineColor: '#F5F5DC' }] },
                        {
                            columns: [
                                { text: 'Generated: ' + generatedAt, fontSize: 8, color: '#666', margin: [0, 8, 0, 0] },
                                { text: 'Page ' + currentPage + ' of ' + pageCount, alignment: 'right', fontSize: 8, color: '#2D5A27', bold: true, margin: [0, 8, 0, 0] }
                            ]
                        }
                    ]
                };
            },

            content: [
                // REPORT METADATA BOX 
                {
                    columns: [
                        {
                            width: '*',
                            text: 'SALES SUMMARY',
                            style: 'title'
                        },
                        {
                            width: 220,
                            style: 'metaBox',
                            table: {
                                widths: [60, '*'],
                                body: [
                                    [{ text: 'PERIOD:', style: 'metaLabel' }, { text: periodLabel, style: 'metaValue' }],
                                    [{ text: 'CATEGORY:', style: 'metaLabel' }, { text: categoryLabel, style: 'metaValue' }]
                                ]
                            },
                            layout: 'noBorders'
                        }
                    ],
                    margin: [0, 0, 0, 20]
                },

                // MAIN DATA TABLE 
                {
                    style: 'salesTable',
                    table: {
                        headerRows: 1,
                        widths: [55, 65, '*', 52, 28, 65, 65],
                        body: tableBody
                    },
                    layout: {
                        hLineWidth: function (i, node) { return (i === 0 || i === node.table.body.length) ? 0 : 0.5; },
                        vLineWidth: function (i, node) { return 0; },
                        hLineColor: function (i, node) { return '#E0E0E0'; },
                        fillColor: function (rowIndex, node, columnIndex) {
                            return (rowIndex === 0) ? '#F5F5DC' : null;
                        },
                        paddingTop: function (i) { return 6; },
                        paddingBottom: function (i) { return 6; }
                    }
                },

                // GRAND TOTAL BLOCK 
                {
                    columns: [
                        { width: '*', text: '' },
                        {
                            width: 250,
                            table: {
                                widths: ['*', 'auto'],
                                body: [
                                    [
                                        { text: 'GRAND TOTAL DUE', style: 'totalLabel' },
                                        { text: formatPdfCurrency(grandTotal), style: 'totalValue' }
                                    ]
                                ]
                            },
                            layout: 'noBorders',
                            fillColor: '#2D5A27',
                            margin: [0, 15, 0, 0]
                        }
                    ]
                }
            ],

            // TYPOGRAPHY & DESIGN SYSTEM 
            styles: {
                companyNameMain: {
                    fontSize: 16,
                    bold: true,
                    color: '#2D5A27',
                    letterSpacing: 1.5
                },
                title: {
                    fontSize: 18,
                    bold: true,
                    color: '#2D5A27',
                    margin: [0, 5, 0, 0]
                },
                metaBox: {
                    fillColor: '#FAFAFA',
                    margin: [0, 0, 0, 0]
                },
                metaLabel: {
                    fontSize: 8,
                    color: '#999999',
                    bold: true,
                    alignment: 'right',
                    margin: [0, 2, 0, 2]
                },
                metaValue: {
                    fontSize: 9,
                    color: '#333333',
                    bold: true,
                    margin: [0, 2, 0, 2]
                },
                salesTable: {
                    margin: [0, 0, 0, 10]
                },
                tableHeader: {
                    bold: true,
                    fontSize: 9,
                    color: '#2D5A27'
                },
                totalLabel: {
                    color: 'white',
                    fontSize: 11,
                    bold: true,
                    margin: [10, 8, 0, 8]
                },
                totalValue: {
                    color: 'white',
                    fontSize: 12,
                    bold: true,
                    alignment: 'right',
                    margin: [0, 8, 10, 8]
                }
            },
            images: {
                companyLogo: window.location.origin + '/Content/img/solteira-logo.png'
            }
        };

        var filename = 'SalesReport_' + (new Date().toISOString().slice(0, 10)) + '.pdf';
        pdfMake.createPdf(docDefinition).download(filename);
    };

    function escapeCsvCell(val) {
        if (val == null) return '';
        var s = String(val);
        if (/[",\r\n]/.test(s)) {
            return '"' + s.replace(/"/g, '""') + '"';
        }
        return s;
    }

    $scope.exportSalesReportCsv = function () {
        if (!$scope.canExportSalesReport()) {
            Swal.fire({ icon: 'warning', title: 'Apply filters first', text: 'For custom range, set both dates and click Apply before exporting.' });
            return;
        }
        var rows = $scope.salesReportRows || [];
        var grandTotal = $scope.salesReportGrandTotal != null ? $scope.salesReportGrandTotal : 0;
        var header = ['Date', 'Order ID', 'Item Name', 'Category', 'Qty', 'Unit Price', 'Total'];
        var lines = [header.map(escapeCsvCell).join(',')];
        rows.forEach(function (r) {
            lines.push([
                escapeCsvCell(r.date),
                escapeCsvCell(r.orderNumber),
                escapeCsvCell(r.itemName),
                escapeCsvCell(r.category),
                escapeCsvCell(r.qty),
                escapeCsvCell(r.unitPrice),
                escapeCsvCell(r.total)
            ].join(','));
        });
        lines.push('');
        lines.push(escapeCsvCell('Grand Total') + ',,,,,,' + escapeCsvCell($scope.formatReportCurrency(grandTotal)));
        var csv = lines.join('\r\n');
        var blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
        var link = document.createElement('a');
        var url = URL.createObjectURL(blob);
        var filename = 'SalesReport_' + (new Date().toISOString().slice(0, 10)) + '.csv';
        link.setAttribute('href', url);
        link.setAttribute('download', filename);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    };
    //----------Sales Reports Page End----------

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
        window.location.href = '/SolteiraAndBrands/SolteiraBrandsOTP?mode=forgot&email=' + $scope.userEmail;
    };

    // This function will be called by the OTP submit logic
    $scope.handleOTPSuccess = function () {
        if ($scope.currentMode === 'forgot') {
            window.location.href = '/SolteiraAndBrands/SoliteiraBrandsNewPassword';
        } else {
            window.location.href = '/SolteiraAndBrands/SolteiraBrandsLogin';
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

}]);