using System;

namespace Google.OpenLocationCode {
    /// <summary>
    /// Coordinates of a decoded Open Location Code area.
    /// The coordinates include the latitude and longitude of the lower left (south west) and upper right (north east) corners
    /// and the center of the bounding box of the code area.
    /// </summary>
    public class CodeArea {

        internal CodeArea(double southLatitude, double westLongitude, double northLatitude, double eastLongitude) {
            if (southLatitude >= northLatitude || westLongitude >= eastLongitude) {
                throw new ArgumentException("min must be less than max");
            }

            Min = new GeoPoint(southLatitude, westLongitude);
            Max = new GeoPoint(northLatitude, eastLongitude);
        }

        /// <summary>
        /// The min (south west) point coordinates of the area bounds.
        /// </summary>
        public GeoPoint Min { get; }

        /// <summary>
        /// The max (north east) point coordinates of the area bounds.
        /// </summary>
        public GeoPoint Max { get; }

        /// <summary>
        /// The center point of the area which is equidistant between <see cref="Min"/> and <see cref="Max"/>.
        /// </summary>
        public GeoPoint Center => new GeoPoint(CenterLatitude, CenterLongitude);


        /// <summary>
        /// The width of the area in longitude degrees.
        /// </summary>
        public double LongitudeWidth => (double) ((decimal) Max.Longitude - (decimal) Min.Longitude);

        /// <summary>
        /// The height of the area in latitude degrees.
        /// </summary>
        public double LatitudeHeight => (double) ((decimal) Max.Latitude - (decimal) Min.Latitude);


        /// <summary>The south (min) latitude coordinate in decimal degrees.</summary>
        /// <remarks>Alias to <see cref="Min"/>.<see cref="GeoPoint.Latitude">Latitude</see></remarks>
        public double SouthLatitude => Min.Latitude;

        /// <summary>The west (min) longitude coordinate in decimal degrees.</summary>
        /// <remarks>Alias to <see cref="Min"/>.<see cref="GeoPoint.Longitude">Longitude</see></remarks>
        public double WestLongitude => Min.Longitude;

        /// <summary>The north (max) latitude coordinate in decimal degrees.</summary>
        /// <remarks>Alias to <see cref="Max"/>.<see cref="GeoPoint.Latitude">Latitude</see></remarks>
        public double NorthLatitude => Max.Latitude;

        /// <summary>The east (max) longitude coordinate in decimal degrees.</summary>
        /// <remarks>Alias to <see cref="Max"/>.<see cref="GeoPoint.Longitude">Longitude</see></remarks>
        public double EastLongitude => Max.Longitude;

        /// <summary>The center latitude coordinate in decimal degrees.</summary>
        /// <remarks>Alias to <see cref="Center"/>.<see cref="GeoPoint.Latitude">Latitude</see></remarks>
        public double CenterLatitude => (Min.Latitude + Max.Latitude) / 2;

        /// <summary>The center longitude coordinate in decimal degrees.</summary>
        /// <remarks>Alias to <see cref="Center"/>.<see cref="GeoPoint.Longitude">Longitude</see></remarks>
        public double CenterLongitude => (Min.Longitude + Max.Longitude) / 2;


        /// <returns><c>true</c> if this code area contains the provided point, <c>false</c> otherwise.</returns>
        /// <param name="point">The point coordinates to check.</param>
        public bool Contains(GeoPoint point) {
            return Contains(point.Latitude, point.Longitude);
        }

        /// <returns><c>true</c> if this code area contains the provided point, <c>false</c> otherwise.</returns>
        /// <param name="latitude">The latitude coordinate of the point to check.</param>
        /// <param name="longitude">The longitude coordinate of the point to check.</param>
        public bool Contains(double latitude, double longitude) {
            return Min.Latitude <= latitude && latitude < Max.Latitude
                && Min.Longitude <= longitude && longitude < Max.Longitude;
        }

    }
}
