using System;
using System.Collections.Generic;
using Google.OpenLocationCode;
using Xunit;

public class EncodingTest {

    private static readonly List<TestData> testDataList = new List<TestData> {
        new TestData("7FG49Q00+", 20.375, 2.775, 20.35, 2.75, 20.4, 2.8),
        new TestData("7FG49QCJ+2V", 20.3700625, 2.7821875, 20.37, 2.782125, 20.370125, 2.78225),
        new TestData("7FG49QCJ+2VX", 20.3701125, 2.782234375, 20.3701, 2.78221875, 20.370125, 2.78225),
        new TestData("7FG49QCJ+2VXGJ", 20.3701135, 2.78223535156, 20.370113, 2.782234375, 20.370114, 2.78223632813),
        new TestData("8FVC2222+22", 47.0000625, 8.0000625, 47.0, 8.0, 47.000125, 8.000125),
        new TestData("4VCPPQGP+Q9", -41.2730625, 174.7859375, -41.273125, 174.785875, -41.273, 174.786),
        new TestData("62G20000+", 0.5, -179.5, 0.0, -180.0, 1, -179),
        new TestData("22220000+", -89.5, -179.5, -90, -180, -89, -179),
        new TestData("7FG40000+", 20.5, 2.5, 20.0, 2.0, 21.0, 3.0),
        new TestData("22222222+22", -89.9999375, -179.9999375, -90.0, -180.0, -89.999875, -179.999875),
        new TestData("6VGX0000+", 0.5, 179.5, 0, 179, 1, 180),
        new TestData("6FH32222+222", 1, 1, 1, 1, 1.000025, 1.00003125),
        // Special cases over 90 latitude and 180 longitude),
        new TestData("CFX30000+", 90, 1, 89, 1, 90, 2),
        new TestData("CFX30000+", 92, 1, 89, 1, 90, 2),
        new TestData("62H20000+", 1, 180, 1, -180, 2, -179),
        new TestData("62H30000+", 1, 181, 1, -179, 2, -178),
        new TestData("CFX3X2X2+X2", 90, 1, 89.9998750, 1, 90, 1.0001250)
    };

    public class TheEncodeMethod {
        [Fact]
        public void ShouldEncodePointToLocationCode() {
            foreach (var testData in testDataList) {
                int codeLength = testData.Code.Length - 1;
                if (testData.Code.Contains("0")) {
                    codeLength = testData.Code.IndexOf("0");
                }
                Assert.True(testData.Code == OpenLocationCode.Encode(testData.Lat, testData.Lon, codeLength),
                    $"Latitude {testData.Lat} and longitude {testData.Lon} were wrongly encoded.");
            }
        }

        [Fact]
        public void ShouldClipCoordinatesWhenExceedingMaximum() {
            Assert.True(OpenLocationCode.Encode(-90, 5) == OpenLocationCode.Encode(-91, 5),
                "Clipping of negative latitude doesn't work.");
            Assert.True(OpenLocationCode.Encode(90, 5) == OpenLocationCode.Encode(91, 5),
                "Clipping of positive latitude doesn't work.");
            Assert.True(OpenLocationCode.Encode(5, 175) == OpenLocationCode.Encode(5, -185),
                "Clipping of negative longitude doesn't work.");
            Assert.True(OpenLocationCode.Encode(5, 175) == OpenLocationCode.Encode(5, -905),
                "Clipping of very long negative longitude doesn't work.");
            Assert.True(OpenLocationCode.Encode(5, -175) == OpenLocationCode.Encode(5, 905),
                "Clipping of very long positive longitude doesn't work.");
        }

        [Fact]
        public void ShouldLimitCodeLengthWhenExceedingMaximum() {
            string code = OpenLocationCode.Encode(51.3701125, -10.202665625, 1000000);

            Assert.True(code.Length == OpenLocationCode.MaxCodeLength + 1,
                "Encoded code should have a length of MaxCodeLength + 1 for the plus symbol");
        }
    }

    public class TheDecodeMethod {
        [Fact]
        public void ShouldDecodeLocationCodeToExpectedCodeArea() {
            foreach (var testData in testDataList) {
                var decoded = OpenLocationCode.Decode(testData.Code);

                Assert.True(IsNear(testData.DecodedLatLo, decoded.SouthLatitude),
                    $"Wrong low latitude for code {testData.Code}");
                Assert.True(IsNear(testData.DecodedLatHi, decoded.NorthLatitude),
                    $"Wrong high latitude for code {testData.Code}");
                Assert.True(IsNear(testData.DecodedLonLo, decoded.WestLongitude),
                    $"Wrong low longitude for code {testData.Code}");
                Assert.True(IsNear(testData.DecodedLonHi, decoded.EastLongitude),
                    $"Wrong high longitude for code {testData.Code}");
            }
        }

        [Fact]
        public void ShouldDecodeToCodeAreaWithValidContainmentRelation() {
            foreach (var testData in testDataList) {
                var olc = new OpenLocationCode(testData.Code);
                var decoded = olc.Decode();
                Assert.True(olc.Contains(decoded.CenterLatitude, decoded.CenterLongitude),
                    $"Containment relation is broken for the decoded middle point of code {testData.Code}");
                Assert.True(olc.Contains(decoded.SouthLatitude, decoded.WestLongitude),
                    $"Containment relation is broken for the decoded bottom left corner of code {testData.Code}");
                Assert.False(olc.Contains(decoded.NorthLatitude, decoded.EastLongitude),
                    $"Containment relation is broken for the decoded top right corner of code {testData.Code}");
                Assert.False(olc.Contains(decoded.SouthLatitude, decoded.EastLongitude),
                    $"Containment relation is broken for the decoded bottom right corner of code {testData.Code}");
                Assert.False(olc.Contains(decoded.NorthLatitude, decoded.WestLongitude),
                    $"Containment relation is broken for the decoded top left corner of code {testData.Code}");
            }
        }

        [Fact]
        public void ShouldDecodeToCodeAreaWithExpectedDimensions() {
            Assert.Equal(OpenLocationCode.Decode("67000000+").LongitudeWidth, 20.0, 0);
            Assert.Equal(OpenLocationCode.Decode("67000000+").LatitudeHeight, 20.0, 0);
            Assert.Equal(OpenLocationCode.Decode("67890000+").LongitudeWidth, 1.0, 0);
            Assert.Equal(OpenLocationCode.Decode("67890000+").LatitudeHeight, 1.0, 0);
            Assert.Equal(OpenLocationCode.Decode("6789CF00+").LongitudeWidth, 0.05, 0);
            Assert.Equal(OpenLocationCode.Decode("6789CF00+").LatitudeHeight, 0.05, 0);
            Assert.Equal(OpenLocationCode.Decode("6789CFGH+").LongitudeWidth, 0.0025, 0);
            Assert.Equal(OpenLocationCode.Decode("6789CFGH+").LatitudeHeight, 0.0025, 0);
            Assert.Equal(OpenLocationCode.Decode("6789CFGH+JM").LongitudeWidth, 0.000125, 0);
            Assert.Equal(OpenLocationCode.Decode("6789CFGH+JM").LatitudeHeight, 0.000125, 0);
            Assert.Equal(OpenLocationCode.Decode("6789CFGH+JMP").LongitudeWidth, 0.00003125, 0);
            Assert.Equal(OpenLocationCode.Decode("6789CFGH+JMP").LatitudeHeight, 0.000025, 0);
        }

        private bool IsNear(double a, double b) {
            return Math.Abs(a - b) < 1e-10;
        }
    }

    private struct TestData {

        internal TestData(string code, double lat, double lon, double decodedLatLo, double decodedLonLo, double decodedLatHi, double decodedLonHi) {
            Code = code;
            Lat = lat;
            Lon = lon;
            DecodedLatLo = decodedLatLo;
            DecodedLonLo = decodedLonLo;
            DecodedLatHi = decodedLatHi;
            DecodedLonHi = decodedLonHi;
        }

        internal string Code { get; }
        internal double Lat { get; }
        internal double Lon { get; }
        internal double DecodedLatLo { get; }
        internal double DecodedLatHi { get; }
        internal double DecodedLonLo { get; }
        internal double DecodedLonHi { get; }

    }
}
