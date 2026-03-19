app.service("SolteiraBrandsService", function ($http) {

    this.defaultProductImageUrl = "/Content/img/shared/default-product.svg";
    this.defaultAvatarUrl = "/Content/img/shared/default-avatar.svg";

    this.getMyProfile = function () {
        return $http({ method: "GET", url: "/Account/GetMyProfile" });
    };

    this.updateProfile = function (avatarImageId, username) {
        var params = new URLSearchParams();
        if (avatarImageId != null) params.append("avatarImageId", avatarImageId);
        if (username != null) params.append("username", username);
        return $http({
            method: "POST",
            url: "/Account/UpdateProfile",
            data: params.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.uploadProfileImage = function (file) {
        var formData = new FormData();
        formData.append("file", file);
        return $http({
            method: "POST",
            url: "/Account/UploadProfileImage",
            data: formData,
            headers: { "Content-Type": undefined },
            transformRequest: angular.identity
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

    this.getProductImage = function (imageId) {
        return $http({
            method: "GET",
            url: "/SolteiraAndBrands/GetProductImage",
            params: { imageId: imageId }
        });
    };

    this.getStorefrontProducts = function () {
        return $http({
            method: "GET",
            url: "/SolteiraAndBrands/GetStorefrontProducts"
        });
    };

    this.getProductStock = function (productId) {
        return $http({
            method: "GET",
            url: "/SolteiraAndBrands/GetProductStock",
            params: { productId: productId }
        });
    };

    this.getProductStocks = function (productIds) {
        if (!productIds || productIds.length === 0) return Promise.resolve({ data: { success: true, stocks: [] } });
        return $http({
            method: "GET",
            url: "/SolteiraAndBrands/GetProductStocks",
            params: { productIds: productIds }
        });
    };

    // ----- CART: server-side per account (BUYER only) -----
    this.getMyCart = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetMyCart" });
    };

    this.getCartCount = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetCartCount" });
    };

    this.addToCart = function (productId, quantity) {
        var form = new URLSearchParams();
        form.append("productId", productId);
        form.append("quantity", quantity);
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/AddToCart",
            data: form.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.setCartItemQuantity = function (productId, quantity) {
        var form = new URLSearchParams();
        form.append("productId", productId);
        form.append("quantity", quantity);
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/SetCartItemQuantity",
            data: form.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.removeCartItem = function (productId) {
        var form = new URLSearchParams();
        form.append("productId", productId);
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/RemoveCartItem",
            data: form.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.login = function (username, password) {
        var params = new URLSearchParams();
        params.append("username", username || "");
        params.append("password", password || "");
        return $http({
            method: "POST",
            url: "/Account/Login",
            data: params.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.register = function (data, requestVerificationToken) {
        var params = new URLSearchParams();
        params.append("email", data.email || "");
        params.append("username", data.username || "");
        params.append("password", data.password || "");
        params.append("verifyPassword", data.verifyPassword || "");
        params.append("__RequestVerificationToken", requestVerificationToken || "");
        return $http({
            method: "POST",
            url: "/Account/Register",
            data: params.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.verifyAccount = function (email, otp, requestVerificationToken) {
        var params = new URLSearchParams();
        params.append("email", email || "");
        params.append("otp", otp || "");
        params.append("__RequestVerificationToken", requestVerificationToken || "");
        return $http({
            method: "POST",
            url: "/Account/VerifyAccount",
            data: params.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.forgotPassword = function (email, requestVerificationToken) {
        var params = new URLSearchParams();
        params.append("email", email || "");
        params.append("__RequestVerificationToken", requestVerificationToken || "");
        return $http({
            method: "POST",
            url: "/Account/ForgotPassword",
            data: params.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.resendOtp = function (email, mode, requestVerificationToken) {
        var params = new URLSearchParams();
        params.append("email", email || "");
        params.append("mode", mode || "");
        params.append("__RequestVerificationToken", requestVerificationToken || "");
        return $http({
            method: "POST",
            url: "/Account/ResendOtp",
            data: params.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.validatePasswordResetOtp = function (email, otp, requestVerificationToken) {
        var params = new URLSearchParams();
        params.append("email", email || "");
        params.append("otp", otp || "");
        params.append("__RequestVerificationToken", requestVerificationToken || "");
        return $http({
            method: "POST",
            url: "/Account/ValidatePasswordResetOtp",
            data: params.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.resetPassword = function (email, newPassword, confirmPassword, requestVerificationToken) {
        var params = new URLSearchParams();
        params.append("email", email || "");
        params.append("newPassword", newPassword || "");
        params.append("confirmPassword", confirmPassword || "");
        params.append("__RequestVerificationToken", requestVerificationToken || "");
        return $http({
            method: "POST",
            url: "/Account/ResetPassword",
            data: params.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

    this.isEmailAvailable = function (email) {
        return $http({
            method: "GET",
            url: "/Account/IsEmailAvailable",
            params: { email: email || "" }
        });
    };

    this.isUsernameAvailable = function (username, ignoreCurrent) {
        return $http({
            method: "GET",
            url: "/Account/IsUsernameAvailable",
            params: { username: username || "", ignoreCurrent: !!ignoreCurrent }
        });
    };

    this.createOrder = function (payload) {
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/CreateOrder",
            data: payload,
            headers: { "Content-Type": "application/json" }
        });
    };

    this.getMyOrders = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetMyOrders" });
    };

    this.getOrderForPayment = function (orderId) {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetOrderForPayment", params: { orderId: orderId } });
    };

    this.uploadPaymentProof = function (file) {
        var formData = new FormData();
        formData.append("file", file);
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/UploadPaymentProof",
            data: formData,
            headers: { "Content-Type": undefined },
            transformRequest: angular.identity
        });
    };

    this.getPaymentSettings = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetPaymentSettings" });
    };
    this.getMeetupSettings = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetMeetupSettings" });
    };
    this.getLayawaySettings = function () {
        return $http({ method: "GET", url: "/SolteiraAndBrands/GetLayawaySettings" });
    };
    // Same pattern as avatar/QR: upload proof first (UploadPaymentProof), then submit with proofImageId in body.
    this.submitPayment = function (orderId, amount, proofImageId, referenceNumber, meetupDate, meetupTime, paymentChannel) {
        var form = new URLSearchParams();
        form.append("OrderId", orderId);
        form.append("Amount", amount);
        if (proofImageId != null && proofImageId !== '') form.append("ProofImageId", proofImageId);
        if (referenceNumber != null && referenceNumber !== '') form.append("ReferenceNumber", referenceNumber);
        if (meetupDate != null && meetupDate !== '') form.append("MeetupDate", meetupDate);
        if (meetupTime != null && meetupTime !== '') form.append("MeetupTime", meetupTime);
        if (paymentChannel != null && paymentChannel !== '') form.append("PaymentChannel", paymentChannel);
        return $http({
            method: "POST",
            url: "/SolteiraAndBrands/SubmitPayment",
            data: form.toString(),
            headers: { "Content-Type": "application/x-www-form-urlencoded" }
        });
    };

});
