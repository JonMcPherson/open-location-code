using System.Collections.Generic;
using Google.OpenLocationCode;
using Xunit;

public static class ValidityTest {

    private static readonly List<TestData> testDataList = new List<TestData> {
        new TestData("8FWC2345+G6", true, false, true),
        new TestData("8FWC2345+G6G", true, false, true),
        new TestData("8fwc2345+", true, false, true),
        new TestData("8FWCX400+", true, false, true),
        // Valid short codes:
        new TestData("WC2345+G6g", true, true, false),
        new TestData("2345+G6", true, true, false),
        new TestData("45+G6", true, true, false),
        new TestData("+G6", true, true, false),
        // Invalid codes
        new TestData("G+", false, false, false),
        new TestData("+", false, false, false),
        new TestData("8FWC2345+G", false, false, false),
        new TestData("8FWC2_45+G6", false, false, false),
        new TestData("8FWC2η45+G6", false, false, false),
        new TestData("8FWC2345+G6+", false, false, false),
        new TestData("8FWC2300+G6", false, false, false),
        new TestData("WC2300+G6g", false, false, false),
        new TestData("WC2345+G", false, false, false)
    };


    public class TheIsValidCodeMethod {
        [Fact]
        public void ShouldTestValidityOfACode() {
            foreach (TestData testData in testDataList) {
                Assert.True(testData.IsValid == OpenLocationCode.IsValidCode(testData.Code),
                            $"Validity of code {testData.Code} is wrong.");
            }
        }
    }

    public class TheIsShortCodeMethod {
        [Fact]
        public void ShouldTestShortnessOfACode() {
            foreach (TestData testData in testDataList) {
                Assert.True(testData.IsShort == OpenLocationCode.IsShortCode(testData.Code),
                            $"Shortness of code {testData.Code} is wrong.");
            }
        }
    }

    public class TheIsFullCodeMethod {
        [Fact]
        public void ShouldTestFullnessOfACode() {
            foreach (TestData testData in testDataList) {
                Assert.True(testData.IsFull == OpenLocationCode.IsFullCode(testData.Code),
                            $"Fullness of code {testData.Code} is wrong.");
            }
        }
    }

    private struct TestData {

        internal TestData(string code, bool isValid, bool isShort, bool isFull) {
            Code = code;
            IsValid = isValid;
            IsShort = isShort;
            IsFull = isFull;
        }

        internal string Code { get; }
        internal bool IsValid { get; }
        internal bool IsShort { get; }
        internal bool IsFull { get; }

    }

}
