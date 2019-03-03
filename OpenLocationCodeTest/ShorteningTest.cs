using System.Collections.Generic;
using Google.OpenLocationCode;
using NUnit.Framework;

public static class ShorteningTest {

    private static readonly List<TestData> TestDataList = new List<TestData> {
        new TestData("9C3W9QCJ+2VX", 51.3701125, -1.217765625, "+2VX", "B"),
        // Adjust so we can't trim by 8 (+/- .000755)
        new TestData("9C3W9QCJ+2VX", 51.3708675, -1.217765625, "CJ+2VX", "B"),
        new TestData("9C3W9QCJ+2VX", 51.3693575, -1.217765625, "CJ+2VX", "B"),
        new TestData("9C3W9QCJ+2VX", 51.3701125, -1.218520625, "CJ+2VX", "B"),
        new TestData("9C3W9QCJ+2VX", 51.3701125, -1.217010625, "CJ+2VX", "B"),
        // Adjust so we can't trim by 6 (+/- .0151)
        new TestData("9C3W9QCJ+2VX", 51.3852125, -1.217765625, "9QCJ+2VX", "B"),
        new TestData("9C3W9QCJ+2VX", 51.3550125, -1.217765625, "9QCJ+2VX", "B"),
        new TestData("9C3W9QCJ+2VX", 51.3701125, -1.232865625, "9QCJ+2VX", "B"),
        new TestData("9C3W9QCJ+2VX", 51.3701125, -1.202665625, "9QCJ+2VX", "B"),
        // Added to detect error in recoverNearest functionality
        new TestData("8FJFW222+", 42.899, 9.012, "22+", "B"),
        new TestData("796RXG22+", 14.95125, -23.5001, "22+", "B"),
        // Reference location is in the 4 digit cell to the south.
        new TestData("8FVC2GGG+GG", 46.976, 8.526, "2GGG+GG", "B"),
        // Reference location is in the 4 digit cell to the north.
        new TestData("8FRCXGGG+GG", 47.026, 8.526, "XGGG+GG", "B"),
        // Reference location is in the 4 digit cell to the east.
        new TestData("8FR9GXGG+GG", 46.526, 8.026, "GXGG+GG", "B"),
        // Reference location is in the 4 digit cell to the west.
        new TestData("8FRCG2GG+GG", 46.526, 7.976, "G2GG+GG", "B"),
        // Added to detect errors recovering codes near the poles.
        // This tests recovery function,  but these codes won't shorten.
        new TestData("CFX22222+22", 89.6, 0.0, "2222+22", "R"),
        new TestData("2CXXXXXX+XX", -81.0, 0.0, "XXXXXX+XX", "R")
    };

    public class TheShortenMethod {
        [Test]
        public void ShouldShortenLongCodeToShortCodeFromReferencePoint() {
            foreach (var testData in TestDataList) {
                if (testData.TestType != "B" && testData.TestType != "S") {
                    continue;
                }
                OpenLocationCode olc = new OpenLocationCode(testData.Code);
                OpenLocationCode shortened = olc.Shorten(testData.ReferenceLatitude, testData.ReferenceLongitude);
                Assert.AreEqual(testData.ShortCode, shortened.Code);
            }
        }
    }

    public class TheRecoverMethod {
        [Test]
        public void ShouldRecoverShortCodeToLongCodeFromReferencePoint() {
            foreach (var testData in TestDataList) {
                if (testData.TestType != "B" && testData.TestType != "R") {
                    continue;
                }
                OpenLocationCode olc = new OpenLocationCode(testData.ShortCode);
                OpenLocationCode recovered = olc.Recover(testData.ReferenceLatitude, testData.ReferenceLongitude);
                Assert.AreEqual(testData.Code, recovered.Code);
            }
        }

        [Test]
        public void ShouldRecoverShortCodesNearSouthPole() {
            OpenLocationCode olc = new OpenLocationCode("XXXXXX+XX");
            Assert.AreEqual("2CXXXXXX+XX", olc.Recover(-81.0, 0.0).Code);
        }

        [Test]
        public void ShouldRecoverShortCodesNearNorthPole() {
            OpenLocationCode olc = new OpenLocationCode("2222+22");
            Assert.AreEqual("CFX22222+22", olc.Recover(89.6, 0.0).Code);
        }
    }

    private struct TestData {

        internal TestData(string code, double referenceLatitude, double referenceLongitude, string shortCode, string testType) {
            Code = code;
            ReferenceLatitude = referenceLatitude;
            ReferenceLongitude = referenceLongitude;
            ShortCode = shortCode;
            TestType = testType;
        }

        internal string Code { get; }
        internal double ReferenceLatitude { get; }
        internal double ReferenceLongitude { get; }
        internal string ShortCode { get; }
        internal string TestType { get; }

    }
}
