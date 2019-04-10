using System;
using System.Collections.Generic;
using Google.OpenLocationCode;
using NUnit.Framework;

public static class EncodingTest {

    // Test cases for encoding latitude and longitude to codes and expected
    // https://github.com/google/open-location-code/blob/master/test_data/encodingTests.csv
    private static readonly List<TestData> TestDataList = new List<TestData> {
        new TestData("7FG49Q00+", "7FG49Q", 20.375, 2.775, 20.35, 2.75, 20.4, 2.8),
        new TestData("7FG49QCJ+2V", "7FG49QCJ2V", 20.3700625, 2.7821875, 20.37, 2.782125, 20.370125, 2.78225),
        new TestData("7FG49QCJ+2VX", "7FG49QCJ2VX", 20.3701125, 2.782234375, 20.3701, 2.78221875, 20.370125, 2.78225),
        new TestData("7FG49QCJ+2VXGJ", "7FG49QCJ2VXGJ", 20.3701135, 2.78223535156, 20.370113, 2.782234375, 20.370114, 2.78223632813),
        new TestData("8FVC2222+22", "8FVC222222", 47.0000625, 8.0000625, 47.0, 8.0, 47.000125, 8.000125),
        new TestData("4VCPPQGP+Q9", "4VCPPQGPQ9", -41.2730625, 174.7859375, -41.273125, 174.785875, -41.273, 174.786),
        new TestData("62G20000+", "62G2", 0.5, -179.5, 0.0, -180.0, 1, -179),
        new TestData("22220000+", "2222", -89.5, -179.5, -90, -180, -89, -179),
        new TestData("7FG40000+", "7FG4", 20.5, 2.5, 20.0, 2.0, 21.0, 3.0),
        new TestData("22222222+22", "2222222222", -89.9999375, -179.9999375, -90.0, -180.0, -89.999875, -179.999875),
        new TestData("6VGX0000+", "6VGX", 0.5, 179.5, 0, 179, 1, 180),
        new TestData("6FH32222+222", "6FH32222222", 1, 1, 1, 1, 1.000025, 1.00003125),
        // Special cases over 90 latitude and 180 longitude
        new TestData("CFX30000+", "CFX3", 90, 1, 89, 1, 90, 2),
        new TestData("CFX30000+", "CFX3", 92, 1, 89, 1, 90, 2),
        new TestData("62H20000+", "62H2", 1, 180, 1, -180, 2, -179),
        new TestData("62H30000+", "62H3", 1, 181, 1, -179, 2, -178),
        new TestData("CFX3X2X2+X2", "CFX3X2X2X2", 90, 1, 89.9998750, 1, 90, 1.0001250),
        // Test non-precise latitude/longitude value
        new TestData("6FH56C22+22", "6FH56C2222", 1.2, 3.4, 1.2000000000000028, 3.4000000000000057, 1.2001249999999999, 3.4001250000000027)
    };

    public class TheEncodeMethod {
        [Test]
        public void ShouldEncodePointToExpectedLocationCode() {
            foreach (var testData in TestDataList) {
                Assert.AreEqual(testData.Code, OpenLocationCode.Encode(testData.EncodedLatitude, testData.EncodedLongitude, testData.CodeDigits.Length),
                    $"Latitude {testData.EncodedLatitude} and longitude {testData.EncodedLongitude} were wrongly encoded.");
            }
        }

        [Test]
        public void ShouldClipCoordinatesWhenExceedingMaximum() {
            Assert.AreEqual(OpenLocationCode.Encode(-90, 5), OpenLocationCode.Encode(-91, 5),
                "Clipping of negative latitude doesn't work.");
            Assert.AreEqual(OpenLocationCode.Encode(90, 5), OpenLocationCode.Encode(91, 5),
                "Clipping of positive latitude doesn't work.");
            Assert.AreEqual(OpenLocationCode.Encode(5, 175), OpenLocationCode.Encode(5, -185),
                "Clipping of negative longitude doesn't work.");
            Assert.AreEqual(OpenLocationCode.Encode(5, 175), OpenLocationCode.Encode(5, -905),
                "Clipping of very long negative longitude doesn't work.");
            Assert.AreEqual(OpenLocationCode.Encode(5, -175), OpenLocationCode.Encode(5, 905),
                "Clipping of very long positive longitude doesn't work.");
        }

        [Test]
        public void ShouldLimitCodeLengthWhenExceedingMaximum() {
            string code = OpenLocationCode.Encode(51.3701125, -10.202665625, 1000000);

            Assert.AreEqual(code.Length, OpenLocationCode.MaxCodeLength + 1,
                "Encoded code should have a length of MaxCodeLength + 1 for the plus symbol");
        }
    }

    public class TheEncodeConstructor {
        [Test]
        public void ShouldEncodePointToExpectedLocationCode() {
            foreach (var testData in TestDataList) {
                OpenLocationCode olc = new OpenLocationCode(testData.EncodedLatitude, testData.EncodedLongitude, testData.CodeDigits.Length);
                Assert.AreEqual(testData.Code, olc.Code,
                    $"Wrong code enocded for latitude {testData.EncodedLatitude} and longitude {testData.EncodedLongitude}.");
            }
        }
        [Test]
        public void ShouldTrimCodesIntoToExpectedCodeDigits() {
            foreach (var testData in TestDataList) {
                OpenLocationCode olc = new OpenLocationCode(testData.EncodedLatitude, testData.EncodedLongitude, testData.CodeDigits.Length);
                Assert.AreEqual(testData.CodeDigits, olc.CodeDigits,
                    $"Wrong digits trimmed for encoded latitude {testData.EncodedLatitude} and longitude {testData.EncodedLongitude}.");
            }
        }
    }


    public class TheDecodeMethod {
        [Test]
        public void ShouldDecodeFullCodesToExpectedCodeArea() {
            foreach (var testData in TestDataList) {
                AssertExpectedDecodedArea(testData, OpenLocationCode.Decode(testData.Code));
            }
        }

        [Test]
        public void ShouldDecodeFullCodesWithLowercaseCharactersToExpectedCodeArea() {
            foreach (var testData in TestDataList) {
                AssertExpectedDecodedArea(testData, OpenLocationCode.Decode(testData.Code.ToLower()));
            }
        }

        [Test]
        public void ShouldDecodeToCodeAreaWithValidContainmentRelation() {
            foreach (var testData in TestDataList) {
                var olc = new OpenLocationCode(testData.Code);
                var decoded = olc.Decode();
                Assert.True(decoded.Contains(decoded.CenterLatitude, decoded.CenterLongitude),
                    $"Containment relation is broken for the decoded middle point of code {testData.Code}");
                Assert.True(decoded.Contains(decoded.SouthLatitude, decoded.WestLongitude),
                    $"Containment relation is broken for the decoded bottom left corner of code {testData.Code}");
                Assert.False(decoded.Contains(decoded.NorthLatitude, decoded.EastLongitude),
                    $"Containment relation is broken for the decoded top right corner of code {testData.Code}");
                Assert.False(decoded.Contains(decoded.SouthLatitude, decoded.EastLongitude),
                    $"Containment relation is broken for the decoded bottom right corner of code {testData.Code}");
                Assert.False(decoded.Contains(decoded.NorthLatitude, decoded.WestLongitude),
                    $"Containment relation is broken for the decoded top left corner of code {testData.Code}");
            }
        }

        [Test]
        public void ShouldDecodeToCodeAreaWithExpectedDimensions() {
            Assert.AreEqual(20.0, OpenLocationCode.Decode("67000000+").LongitudeWidth, 0);
            Assert.AreEqual(20.0, OpenLocationCode.Decode("67000000+").LatitudeHeight, 0);
            Assert.AreEqual(1.0, OpenLocationCode.Decode("67890000+").LongitudeWidth, 0);
            Assert.AreEqual(1.0, OpenLocationCode.Decode("67890000+").LatitudeHeight, 0);
            Assert.AreEqual(0.05, OpenLocationCode.Decode("6789CF00+").LongitudeWidth, 0);
            Assert.AreEqual(0.05, OpenLocationCode.Decode("6789CF00+").LatitudeHeight, 0);
            Assert.AreEqual(0.0025, OpenLocationCode.Decode("6789CFGH+").LongitudeWidth, 0);
            Assert.AreEqual(0.0025, OpenLocationCode.Decode("6789CFGH+").LatitudeHeight, 0);
            Assert.AreEqual(0.000125, OpenLocationCode.Decode("6789CFGH+JM").LongitudeWidth, 0);
            Assert.AreEqual(0.000125, OpenLocationCode.Decode("6789CFGH+JM").LatitudeHeight, 0);
            Assert.AreEqual(0.00003125, OpenLocationCode.Decode("6789CFGH+JMP").LongitudeWidth, 0);
            Assert.AreEqual(0.000025, OpenLocationCode.Decode("6789CFGH+JMP").LatitudeHeight, 0);
        }

        [Test]
        public void ShouldThrowArgumentExceptionForInvalidOrShortCodes() {
            foreach (string code in new[] { null, "INVALID", "9QCJ+2VX" }) {
                Assert.Throws<ArgumentException>(() => OpenLocationCode.Decode(code),
                    $"Expected exception was not thrown for code {code}");
            }
        }

    }

    public class TheDecodeConstructor {
        [Test]
        public void ShouldAcceptFullCodesAndDecodeToExpectedCodeArea() {
            foreach (var testData in TestDataList) {
                AssertExpectedDecodedArea(testData, new OpenLocationCode(testData.Code).Decode());
            }
        }

        [Test]
        public void ShouldNormalizeFullCodeDigitsToExpectedCode() {
            foreach (var testData in TestDataList) {
                Assert.AreEqual(testData.Code, new OpenLocationCode(testData.CodeDigits).Code,
                    $"Wrong code normalized from code digits {testData.CodeDigits}");
            }
        }

        [Test]
        public void ShouldTrimCodesIntoToExpectedCodeDigits() {
            foreach (var testData in TestDataList) {
                Assert.AreEqual(testData.CodeDigits, new OpenLocationCode(testData.Code).CodeDigits,
                    $"Wrong digits trimmed from code {testData.Code}.");
            }
        }

        [Test]
        public void ShouldThrowArgumentExceptionForInvalidOrShortCodes() {
            foreach (string code in new[] { null, "INVALID", "9QCJ+2VX" }) {
                Assert.Throws<ArgumentException>(() => new OpenLocationCode(code),
                    $"Expected exception was not thrown for code {code}");
            }
        }
    }

    private static void AssertExpectedDecodedArea(TestData testData, CodeArea decodedArea) {
        Assert.True(IsNear(testData.DecodedArea.SouthLatitude, decodedArea.SouthLatitude),
            $"Wrong decoded low latitude for code {testData.Code}");
        Assert.True(IsNear(testData.DecodedArea.NorthLatitude, decodedArea.NorthLatitude),
            $"Wrong decoded high latitude for code {testData.Code}");
        Assert.True(IsNear(testData.DecodedArea.WestLongitude, decodedArea.WestLongitude),
            $"Wrong decoded low longitude for code {testData.Code}");
        Assert.True(IsNear(testData.DecodedArea.EastLongitude, decodedArea.EastLongitude),
            $"Wrong decoded high longitude for code {testData.Code}");
    }

    private static bool IsNear(double a, double b) {
        return Math.Abs(a - b) < 1e-10;
    }


    private struct TestData {

        internal TestData(string code, string codeDigits, double lat, double lon, double decodedMinLat, double decodedMinLon, double decodedMaxLat, double decodedMaxLon) {
            Code = code;
            CodeDigits = codeDigits;
            EncodedLatitude = lat;
            EncodedLongitude = lon;
            DecodedArea = new CodeArea(decodedMinLat, decodedMinLon, decodedMaxLat, decodedMaxLon);
        }

        internal string Code { get; }
        internal string CodeDigits { get; }
        internal double EncodedLatitude { get; }
        internal double EncodedLongitude { get; }
        internal CodeArea DecodedArea { get; }

    }
}
