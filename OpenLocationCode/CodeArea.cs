namespace Google.OpenLocationCode {
    /// <summary>
    /// Coordinates of a decoded Open Location Code.
    /// The coordinates include the latitude and longitude of the lower left and upper right corners
    /// and the center of the bounding box for the area the code represents.
    /// </summary>
    public class CodeArea {
        
        public CodeArea(decimal southLatitude, decimal westLongitude, decimal northLatitude, decimal eastLongitude) {
            SouthLatitude = (double) southLatitude;
            WestLongitude = (double) westLongitude;
            NorthLatitude = (double) northLatitude;
            EastLongitude = (double) eastLongitude;
            LatitudeHeight = (double) (northLatitude - southLatitude);
            LongitudeWidth = (double) (eastLongitude - westLongitude);
            CenterLatitude = (double) ((southLatitude + northLatitude) / 2);
            CenterLongitude = (double) ((westLongitude + eastLongitude) / 2);
        }

        public double SouthLatitude { get; }

        public double WestLongitude { get; }

        public double NorthLatitude { get; }

        public double EastLongitude { get; }

        public double LatitudeHeight { get; }

        public double LongitudeWidth { get; }

        public double CenterLatitude { get; }

        public double CenterLongitude { get; }

    }
}
