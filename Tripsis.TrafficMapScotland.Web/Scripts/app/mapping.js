(function (window, $, L, undefined) {

    var map;
    
    function resolvePoint(point, callback) {
        //var selectedMode = getTransportModeFunction();
        //routing.resolvePoint(selectedMode, point, callback);
    }

    function setUpClickEvents() {
        map.on('contextmenu', function(e) {
            var clickedPosition = e.latlng;
            //resolvePoint({ latitude: clickedPosition.lat, longitude: clickedPosition.lng }, function (point) {

            //    geolocation.reverseGeocode({ latitude: point.Latitude, longitude: point.Longitude }, function (pointName) {
            //        var position = { lat: point.Latitude, lng: point.Longitude };
            //        addRoutingPoint(position, pointName);
            //    });

            //});
        });
    }
    
    function init(options, readyCallback) {
        var div = options.container;
        var lat = options.latitude;
        var lng = options.longitude;
        var zoom = options.zoom;

        map = L.map(div).setView([lat, lng], zoom);

        // add an OpenStreetMap tile layer
        L.tileLayer('http://{s}.tile.osm.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors.'
        }).addTo(map);

        setUpClickEvents();

        if (readyCallback) {
            readyCallback();
        }
    }

    var publicApi = {
        init: init
    };

    window.mapping = publicApi;

})(window, $, L);