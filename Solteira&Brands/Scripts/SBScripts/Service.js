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

});
