using System;
using System.Collections.Generic;
using Google.OpenLocationCode;
using NUnit.Framework;

public static class ShorteningTest {

    // Test cases for validating codes and determining code type
    // https://github.com/google/open-location-code/blob/master/test_data/validityTests.csv
    private static readonly IEnumerable<TestData> ShorteningTestCases = TestDataUtils.ReadTestData<TestData>("shortening.csv");


    public class TheShortenMethod {
        [Test]
        public void ShouldShortenFullCodeToShortCodeFromReferencePoint() {
            foreach (var testData in ShorteningTestCases) {
                if (testData.TestType != 'B' && testData.TestType != 'S') {
                    continue;
                }

                OpenLocationCode.ShortCode shortened = OpenLocationCode.Shorten(testData.Code,
                    testData.Latitude, testData.Longitude);
                Assert.AreEqual(testData.ShortCode, shortened.Code,
                    $"Wrong shortening of code {testData.Code} from reference latitude {testData.Latitude} and longitude {testData.Longitude}.");
            }
        }

        [Test]
        public void ShouldThrowArgumentExceptionForInvalidOrShortOrPaddedCodes() {
            foreach (string code in new[] { null, "INVALID", "2222+22", "9C3W9Q00+" }) {
                Assert.Throws<ArgumentException>(() => OpenLocationCode.Shorten(code, 0, 0),
                    $"Expected exception was not thrown for code {code}");
            }
        }
    }

    public class TheRecoverNearestMethod {
        [Test]
        public void ShouldRecoverShortCodeToLongCodeFromReferencePoint() {
            foreach (var testData in ShorteningTestCases) {
                if (testData.TestType != 'B' && testData.TestType != 'R') {
                    continue;
                }
                OpenLocationCode recovered = OpenLocationCode.ShortCode.RecoverNearest(testData.ShortCode,
                    testData.Latitude, testData.Longitude);
                Assert.AreEqual(testData.Code, recovered.Code,
                    $"Wrong recovery of short code {testData.ShortCode} from reference latitude {testData.Latitude} and longitude {testData.Longitude}.");
            }
        }

        [Test]
        public void ShouldRecoverShortCodesNearSouthPole() {
            Assert.AreEqual("2CXXXXXX+XX", OpenLocationCode.ShortCode.RecoverNearest("XXXXXX+XX", - 81.0, 0.0).Code);
        }

        [Test]
        public void ShouldRecoverShortCodesNearNorthPole() {
            Assert.AreEqual("CFX22222+22", OpenLocationCode.ShortCode.RecoverNearest("2222+22", 89.6, 0.0).Code);
        }
    }

    public class TheShortCodeConstructor {
        [Test]
        public void ShouldThrowArgumentExceptionForInvalidShortCodes() {
            foreach (string code in new []{ null, "INVALID", "3W9Q00+", "9C3W9Q00+", "9C3W9QCJ+2VX" }) {
                Assert.Throws<ArgumentException>(() => new OpenLocationCode.ShortCode(code),
                    $"Expected exception not thrown for code {code}");
            }
        }
    }

    public class TestData {

        public string Code { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ShortCode { get; set; }
        public char TestType { get; set; }

    }

}
