using System;
using System.Collections.Generic;
using Google.OpenLocationCode;
using NUnit.Framework;

public static class DecodingTest {

    private const double Precision = 1e-10;

    // Test cases for decoding valid codes into code areas
    // https://github.com/google/open-location-code/blob/master/test_data/decoding.csv
    private static readonly IEnumerable<TestData> DecodingTestCases = TestDataUtils.ReadTestData<TestData>("decoding.csv");


    public class TheDecodeMethod {
        [Test]
        public void ShouldDecodeFullCodesToExpectedCodeArea() {
            foreach (var testData in DecodingTestCases) {
                AssertExpectedDecodedArea(testData, OpenLocationCode.Decode(testData.Code));
            }
        }
    
        [Test]
        public void ShouldDecodeFullCodesWithLowercaseCharactersToExpectedCodeArea() {
            foreach (var testData in DecodingTestCases) {
                AssertExpectedDecodedArea(testData, OpenLocationCode.Decode(testData.Code.ToLower()));
            }
        }
    
        [Test]
        public void ShouldDecodeToCodeAreaWithValidContainmentRelation() {
            foreach (var testData in DecodingTestCases) {
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
            foreach (var testData in DecodingTestCases) {
                AssertExpectedDecodedArea(testData, new OpenLocationCode(testData.Code).Decode());
            }
        }
    
        [Test]
        public void ShouldNormalizeFullCodeDigitsToExpectedCode() {
            foreach (var testData in DecodingTestCases) {
                string codeDigits = OpenLocationCode.TrimCode(testData.Code);
                Assert.AreEqual(testData.Code, new OpenLocationCode(codeDigits).Code,
                    $"Wrong code normalized from code digits {codeDigits}");
            }
        }
    
        [Test]
        public void ShouldTrimCodesIntoToExpectedCodeDigits() {
            foreach (var testData in DecodingTestCases) {
                Assert.AreEqual(OpenLocationCode.TrimCode(testData.Code), new OpenLocationCode(testData.Code).CodeDigits,
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

    private static void AssertExpectedDecodedArea(TestData testData, CodeArea decoded) {
        Assert.AreEqual(testData.Length, decoded.CodeLength, $"Wrong length for code {testData.Code}");
        Assert.AreEqual(testData.LatLo, decoded.SouthLatitude, Precision, $"Wrong low latitude for code {testData.Code}");
        Assert.AreEqual(testData.LatHi, decoded.NorthLatitude, Precision, $"Wrong high latitude for code {testData.Code}");
        Assert.AreEqual(testData.LngLo, decoded.WestLongitude, Precision, $"Wrong low longitude for code {testData.Code}");
        Assert.AreEqual(testData.LngHi, decoded.EastLongitude, Precision, $"Wrong high longitude for code {testData.Code}");
    }


    public class TestData {

        public string Code { get; set; }
        public int Length { get; set; }
        public double LatLo { get; set; }
        public double LngLo { get; set; }
        public double LatHi { get; set; }
        public double LngHi { get; set; }

    }
}
