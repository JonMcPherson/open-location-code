using System;
namespace Google.OpenLocationCode {
    public struct GeoPoint : IEquatable<GeoPoint> {

        public GeoPoint(double latitude, double longitude) {
            if (latitude < -90 || latitude > 90) throw new ArgumentException("latitude is out of range -90 to 90");
            if (longitude < -180 || longitude > 180) throw new ArgumentException("longitude is out of range -180 to 180");

            Latitude = latitude;
            Longitude = longitude;
        }

        public double Latitude { get; }

        public double Longitude { get; }


        public override string ToString() => $"[Longitude:{Longitude},Latitude:{Latitude}]";

        public override int GetHashCode() => Longitude.GetHashCode() ^ Latitude.GetHashCode();

        public override bool Equals(object obj) => obj is GeoPoint coord && Equals(coord);

        public bool Equals(GeoPoint other) => this == other;

        public static bool operator ==(GeoPoint a, GeoPoint b) => a.Longitude == b.Longitude && a.Latitude == b.Latitude;

        public static bool operator !=(GeoPoint a, GeoPoint b) => !(a == b);

    }
}
