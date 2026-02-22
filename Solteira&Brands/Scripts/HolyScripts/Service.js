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

    this.getOrderDetails = function (orderId) {
        return $http({
            method: "GET",
            url: "/SolteiraAndBrands/GetOrderDetails",
            params: { id: orderId }
        });
    };

    this.updateOrderStatus = function (orderId, status, rejectionReason) {
        var params = { id: orderId, status: status };
        if (rejectionReason != null && rejectionReason !== '') params.rejectionReason = rejectionReason;
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UpdateOrderStatus",
            params: params
        });
    };

    this.updatePaymentStatus = function (paymentId, status, rejectionReason) {
        var params = { paymentId: paymentId, status: status };
        if (rejectionReason != null && rejectionReason !== '') params.rejectionReason = rejectionReason;
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UpdatePaymentStatus",
            params: params
        });
    };

    this.getPaymentsForAdmin = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetPaymentsForAdmin" });
    };

    this.revokeApproval = function (orderId) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/RevokeApproval", params: { orderId: orderId } });
    };

    this.createDelivery = function (orderId, deliveryType, trackingNumber) {
        var params = { orderId: orderId, deliveryType: deliveryType };
        if (trackingNumber) params.trackingNumber = trackingNumber;
        return $http({ method: "POST", url: "/SolteiraAndBrands/CreateDelivery", params: params });
    };

    this.updateDeliveryStatus = function (deliveryId, status) {
        return $http({ method: "POST", url: "/SolteiraAndBrands/UpdateDeliveryStatus", params: { deliveryId: deliveryId, status: status } });
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