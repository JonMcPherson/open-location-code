using System;
using System.Collections.Generic;
using Google.OpenLocationCode;
using NUnit.Framework;

public static class EncodingTest {

    // Test cases for encoding latitude and longitude to codes 
    // https://github.com/google/open-location-code/blob/master/test_data/encoding.csv
    private static readonly IEnumerable<TestData> EncodingTestCases = TestDataUtils.ReadTestData<TestData>("encoding.csv");


    public class TheEncodeMethod {
        [Test]
        public void ShouldEncodePointToExpectedLocationCode() {
            foreach (var testData in EncodingTestCases) {
                Assert.AreEqual(testData.Code, OpenLocationCode.Encode(testData.Latitude, testData.Longitude, testData.Length),
                    $"Latitude {testData.Latitude} and longitude {testData.Longitude} were wrongly encoded.");
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

            Assert.AreEqual(code.Length, 16,
                "Encoded code should have a length of 16 (15 + 1 for the plus symbol)");
        }
    }

    public class TheEncodeConstructor {
        [Test]
        public void ShouldEncodePointToExpectedLocationCode() {
            foreach (var testData in EncodingTestCases) {
                OpenLocationCode olc = new OpenLocationCode(testData.Latitude, testData.Longitude, testData.Length);
                Assert.AreEqual(testData.Code, olc.Code,
                    $"Wrong code encoded for latitude {testData.Latitude} and longitude {testData.Longitude}.");
            }
        }
        [Test]
        public void ShouldTrimCodesIntoToExpectedCodeDigits() {
            foreach (var testData in EncodingTestCases) {
                OpenLocationCode olc = new OpenLocationCode(testData.Latitude, testData.Longitude, testData.Length);
                int expectedLength = Math.Min(testData.Length, 15);
                Assert.AreEqual(expectedLength, olc.CodeDigits.Length,
                    $"Wrong length of digits trimmed for encoded latitude {testData.Latitude} and longitude {testData.Longitude}.");
                Assert.AreEqual(testData.Code.Replace("+", "").Substring(0, expectedLength), olc.CodeDigits,
                    $"Wrong digits trimmed for encoded latitude {testData.Latitude} and longitude {testData.Longitude}.");
            }
        }
    }

    public class TestData {

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Length { get; set; }
        public string Code { get; set; }

    }
}
