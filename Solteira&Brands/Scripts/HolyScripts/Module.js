var app = angular.module("SolteiraAndBrands", ["chart.js"]);

app.filter("numberPad", function () {
    return function (num, length) {
        if (num == null || num === undefined) return "";
        var n = parseInt(num, 10);
        if (isNaN(n)) return num;
        var len = parseInt(length, 10) || 3;
        var s = n.toString();
        while (s.length < len) s = "0" + s;
        return s;
    };
});