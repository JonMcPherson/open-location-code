using System;

namespace Google.OpenLocationCode {
    /// <summary>
    /// A point on the three-dimensional geographic coordinate system specified by latitude and longitude coordinates in degrees.
    /// </summary>
    public struct GeoPoint : IEquatable<GeoPoint> {

        /// <param name="latitude">The latitude coordinate in decimal degrees</param>
        /// <param name="longitude">The longitude coordinate in decimal degrees</param>
        /// <exception cref="ArgumentException">If latitude is out of range -90 to 90</exception>
        /// <exception cref="ArgumentException">If longitude is out of range -180 to 180</exception>
        public GeoPoint(double latitude, double longitude) {
            if (latitude < -90 || latitude > 90) throw new ArgumentException("latitude is out of range -90 to 90");
            if (longitude < -180 || longitude > 180) throw new ArgumentException("longitude is out of range -180 to 180");

            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// The latitude coordinate in decimal degrees (y axis)
        /// </summary>
        public double Latitude { get; }

        /// <summary>
        /// The longitude coordinate in decimal degrees (x axis)
        /// </summary>
        public double Longitude { get; }


        /// <returns>A human readable representation of this GeoPoint</returns>
        public override string ToString() => $"[Longitude:{Longitude},Latitude:{Latitude}]";

        public override int GetHashCode() => Longitude.GetHashCode() ^ Latitude.GetHashCode();

        public override bool Equals(object obj) => obj is GeoPoint coord && Equals(coord);

        public bool Equals(GeoPoint other) => this == other;

        public static bool operator ==(GeoPoint a, GeoPoint b) => a.Longitude == b.Longitude && a.Latitude == b.Latitude;

        public static bool operator !=(GeoPoint a, GeoPoint b) => !(a == b);

    }
}
