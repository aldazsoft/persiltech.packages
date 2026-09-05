const getPosition = () => {
    return new Promise((resolve, reject) => {
        if ("geolocation" in navigator) {
            navigator.geolocation.getCurrentPosition(returnPosition, returnError);
        }

        function returnPosition(position) {
            resolve({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            });
        }

        function returnError(error) {
            let errorMessage;
            switch (error.code) {
                case error.PERMISSION_DENIED:
                    errorMessage = "User denied the request for Geolocation.";
                    break;
                case error.POSITION_UNAVAILABLE:
                    errorMessage = "Location information is unavailable.";
                    break;
                case error.TIMEOUT:
                    errorMessage = "The request to get user location time out.";
                    break;
                case error.UNKNOWN_ERROR:
                    errorMessage = "An unknown error ocurred.";
                    break;
                default:
                    errorMessage = "An error has ocurred.";
            }

            reject(errorMessage);
        }
    });
}

export { getPosition };
