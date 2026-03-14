app.service("SolteiraAndBrandsService", function ($http) {

    // Inventory / products (admin)
    this.getInventoryProducts = function (archived) {
        return $http({
            method: "GET",
            url: "/SolteiraAndBrands/GetInventoryProducts",
            params: { archived: !!archived }
        });
    };

    this.createProduct = function (productData) {
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/CreateProduct",
            data: productData
        });
    };

    this.updateProduct = function (productData) {
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UpdateProduct",
            data: productData
        });
    };

    this.toggleProductArchive = function (id) {
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/ToggleProductArchive",
            data: { id: id }
        });
    };

    this.toggleFeatured = function (productId) {
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/ToggleFeatured",
            data: { productId: productId }
        });
    };

    this.uploadProductImage = function (file) {
        var formData = new FormData();
        formData.append("file", file);
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UploadProductImage",
            data: formData,
            headers: { "Content-Type": undefined },
            transformRequest: angular.identity
        });
    };

    this.getOrders = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetOrders" });
    };

    this.getArchiveOrders = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetArchiveOrders" });
    };

    this.archiveOrder = function (orderId) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/ArchiveOrder", params: { orderId: orderId } });
    };

    this.restoreOrder = function (orderId) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/RestoreOrder", params: { orderId: orderId } });
    };

    this.getOrderDetails = function (orderId) {
        return $http({
            method: "GET",
            url: "/SolteiraAndBrands/GetOrderDetails",
            params: { id: orderId }
        });
    };

    this.updateOrderStatus = function (orderId, status, rejectionReason, restoreStock) {
        var params = { id: orderId, status: status };
        if (rejectionReason != null && rejectionReason !== '') params.rejectionReason = rejectionReason;
        if (typeof restoreStock === 'boolean') params.restoreStock = restoreStock;
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UpdateOrderStatus",
            params: params
        });
    };

    this.updatePaymentStatus = function (paymentId, status, rejectionReason, allowResubmit) {
        var form = new URLSearchParams();
        form.append("paymentId", paymentId);
        form.append("status", status);
        if (rejectionReason != null && rejectionReason !== '') form.append("rejectionReason", rejectionReason);
        if (allowResubmit != null) form.append("allowResubmit", allowResubmit ? "true" : "false");
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UpdatePaymentStatus",
            data: form.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.getPaymentsForAdmin = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetPaymentsForAdmin" });
    };

    this.getPaymentDetailForAdmin = function (paymentId) {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetPaymentDetailForAdmin", params: { paymentId: paymentId } });
    };

    this.revokeApproval = function (orderId) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/RevokeApproval", params: { orderId: orderId } });
    };

    this.createDelivery = function (orderId, deliveryType, trackingNumber, courier, dispatchDate, eta) {
        var params = { orderId: orderId, deliveryType: deliveryType || 'COURIER' };
        if (trackingNumber) params.trackingNumber = trackingNumber;
        if (courier) params.courier = courier;
        if (dispatchDate) params.dispatchDate = dispatchDate;
        if (eta) params.eta = eta;
        return $http({ method: "POST", url: "/SolteiraAndBrands/CreateDelivery", params: params });
    };

    this.updateDeliveryStatus = function (deliveryId, status) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/UpdateDeliveryStatus", params: { deliveryId: deliveryId, status: status } });
    };

    this.getDeliveryDetailForAdmin = function (deliveryId) {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetDeliveryDetailForAdmin", params: { deliveryId: deliveryId } });
    };

    this.updateDelivery = function (deliveryId, courier, trackingNumber, dispatchDate, eta, deliveryStatus) {
        var params = { deliveryId: deliveryId };
        if (courier != null) params.courier = courier;
        if (trackingNumber != null) params.trackingNumber = trackingNumber;
        if (dispatchDate != null) params.dispatchDate = dispatchDate;
        if (eta != null) params.eta = eta;
        if (deliveryStatus != null) params.deliveryStatus = deliveryStatus;
        return $http({ method: "POST", url: "/SolteiraAndBrands/UpdateDelivery", params: params });
    };

    this.getOrdersForDeliveryShipments = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetOrdersForDeliveryShipments" });
    };
    this.getDeliveryShipments = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetDeliveryShipments" });
    };
    this.getOrdersForDeliveryMeetup = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetOrdersForDeliveryMeetup" });
    };

    this.getMeetupOrderDetail = function (orderId) {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetMeetupOrderDetail", params: { orderId: orderId } });
    };
    this.uploadMeetupReceipt = function (file) {
        var formData = new FormData();
        formData.append("file", file);
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UploadMeetupReceipt",
            data: formData,
            headers: { "Content-Type": undefined },
            transformRequest: angular.identity
        });
    };
    this.updateMeetupOutcome = function (orderId, status, receiptImageId, cancellationReason, restoreStock) {
        var params = { orderId: orderId, status: status };
        if (receiptImageId) params.receiptImageId = receiptImageId;
        if (cancellationReason) params.cancellationReason = cancellationReason;
        if (typeof restoreStock !== "undefined" && restoreStock !== null) params.restoreStock = restoreStock;
        return $http({ method: "POST", url: "/SolteiraAndBrands/UpdateMeetupOutcome", params: params });
    };

    this.rescheduleMeetup = function (orderId, meetupDate, meetupTime) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/RescheduleMeetup", params: { orderId: orderId, meetupDate: meetupDate, meetupTime: meetupTime } });
    };

    this.markReadyForMeetup = function (orderId) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/MarkReadyForMeetup", params: { orderId: orderId } });
    };

    this.completeMeetup = function (orderId) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/CompleteMeetup", params: { orderId: orderId } });
    };

    this.checkAndCancelOverdueOrders = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/CheckAndCancelOverdueOrders" });
    };

    this.getDashboardData = function (range, startDate, endDate) {
        var params = { range: range || "lastmonth" };
        if (range === "custom" && startDate) params.startDate = startDate;
        if (range === "custom" && endDate) params.endDate = endDate;
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetDashboardData", params: params });
    };

    this.getSalesReport = function (range, startDate, endDate, category) {
        var params = { range: range || "all" };
        if (range === "custom" && startDate) params.startDate = startDate;
        if (range === "custom" && endDate) params.endDate = endDate;
        if (category) params.category = category;
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetSalesReport", params: params });
    };

    this.getLowStockNotifications = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetLowStockNotifications" });
    };

    this.getActivityLogs = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetActivityLogs" });
    };

    this.getPaymentSettingsForAdmin = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetPaymentSettingsForAdmin" });
    };
    this.uploadQrImage = function (file) {
        var formData = new FormData();
        formData.append("file", file);
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UploadQrImage",
            data: formData,
            headers: { "Content-Type": undefined },
            transformRequest: angular.identity
        });
    };
    this.updatePaymentSetting = function (paymentType, accountName, accountNumber, qrImageId) {
        var params = { paymentType: paymentType, accountName: accountName || "", accountNumber: accountNumber || "" };
        if (qrImageId) params.qrImageId = qrImageId;
        return $http({ method: "POST", url: "/SolteiraAndBrands/UpdatePaymentSetting", params: params });
    };
    this.getMeetupSettingsForAdmin = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetMeetupSettingsForAdmin" });
    };
    this.updateMeetupSetting = function (locationName, address) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/UpdateMeetupSetting", params: { locationName: locationName || "", address: address || "" } });
    };
    this.getLayawaySettingsForAdmin = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetLayawaySettingsForAdmin" });
    };
    this.updateLayawaySettings = function (downpaymentPercentage, monthsAllowed) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/UpdateLayawaySettings", params: { downpaymentPercentage: downpaymentPercentage, monthsAllowed: monthsAllowed } });
    };
});