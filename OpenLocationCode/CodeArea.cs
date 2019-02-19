using System;

namespace Google.OpenLocationCode {
    /// <summary>
    /// Coordinates of a decoded Open Location Code.
    /// The coordinates include the latitude and longitude of the lower left and upper right corners
    /// and the center of the bounding box for the area the code represents.
    /// </summary>
    public class CodeArea {

        public CodeArea(double southLatitude, double westLongitude, double northLatitude, double eastLongitude) :
            this(new GeoCoord(southLatitude, westLongitude), new GeoCoord(northLatitude, eastLongitude)) { }

        public CodeArea(GeoCoord min, GeoCoord max) {
            if (min.Longitude >= max.Longitude || min.Latitude >= max.Latitude) {
                throw new ArgumentException("min must be less than max");
            }

            Min = min;
            Max = max;
        }


        public GeoCoord Min { get; }

        public GeoCoord Max { get; }

        public GeoCoord Center => new GeoCoord(
            (Min.Latitude + Max.Latitude) / 2,
            (Min.Longitude + Max.Longitude) / 2
        );

        public double LongitudeWidth => (double) ((decimal) Max.Longitude - (decimal) Min.Longitude);

        public double LatitudeHeight => (double) ((decimal) Max.Latitude - (decimal) Min.Latitude);


        public double SouthLatitude => Min.Latitude;

        public double WestLongitude => Min.Longitude;

        public double NorthLatitude => Max.Latitude;

        public double EastLongitude => Max.Longitude;

        public double CenterLatitude => Center.Latitude;

        public double CenterLongitude => Center.Longitude;


        public bool Contains(GeoCoord coordinates) {
            return Contains(coordinates.Longitude, coordinates.Latitude);
        }

        public bool Contains(double longitude, double latitude) {
            return Min.Longitude <= longitude && longitude < Max.Longitude
                && Min.Latitude <= latitude && latitude < Max.Latitude;
        }

    }
}
