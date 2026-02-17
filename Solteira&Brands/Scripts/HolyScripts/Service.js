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
});