using System;

namespace Google.OpenLocationCode {
    /// <summary>
    /// Coordinates of a decoded Open Location Code area.
    /// The coordinates include the latitude and longitude of the lower left and upper right corners
    /// and the center of the bounding box of the code area.
    /// </summary>
    public class CodeArea {

        public CodeArea(double southLatitude, double westLongitude, double northLatitude, double eastLongitude) :
            this(new GeoPoint(southLatitude, westLongitude), new GeoPoint(northLatitude, eastLongitude)) { }

        public CodeArea(GeoPoint min, GeoPoint max) {
            if (min.Longitude >= max.Longitude || min.Latitude >= max.Latitude) {
                throw new ArgumentException("min must be less than max");
            }

            Min = min;
            Max = max;
        }


        public GeoPoint Min { get; }

        public GeoPoint Max { get; }

        public GeoPoint Center => new GeoPoint(CenterLatitude, CenterLongitude);

        public double LongitudeWidth => (double) ((decimal) Max.Longitude - (decimal) Min.Longitude);

        public double LatitudeHeight => (double) ((decimal) Max.Latitude - (decimal) Min.Latitude);


        public double SouthLatitude => Min.Latitude;

        public double WestLongitude => Min.Longitude;

        public double NorthLatitude => Max.Latitude;

        public double EastLongitude => Max.Longitude;

        public double CenterLatitude => (Min.Latitude + Max.Latitude) / 2;

        public double CenterLongitude => (Min.Longitude + Max.Longitude) / 2;


        public bool Contains(GeoPoint coordinates) {
            return Contains(coordinates.Longitude, coordinates.Latitude);
        }

        public bool Contains(double longitude, double latitude) {
            return Min.Longitude <= longitude && longitude < Max.Longitude
                && Min.Latitude <= latitude && latitude < Max.Latitude;
        }

    }
}
