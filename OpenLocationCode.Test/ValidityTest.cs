using System.Collections.Generic;
using Google.OpenLocationCode;
using NUnit.Framework;

public static class ValidityTest {

    // Test cases for validating codes and determining code type.
    // See: https://github.com/google/open-location-code/blob/master/test_data/validityTests.csv
    private static readonly List<TestData> TestDataList = new List<TestData> {
        // Valid full codes:
        FullCode("8FWC2345+G6"),
        FullCode("8FWC2345+G6G"),
        FullCode("8fwc2345+"),
        FullCode("8FWCX400+", padded: true),
        FullCode("8FWC0000+", padded: true),
        FullCode("8F000000+", padded: true),
        // Valid short codes:
        ShortCode("WC2345+G6g"),
        ShortCode("2345+G6"),
        ShortCode("45+G6"),
        ShortCode("+G6"),
        // Invalid codes:
        InvalidCode("G+"),
        InvalidCode("+"),
        InvalidCode("8FWC2345+G"),
        InvalidCode("8FWC2_45+G6"),
        InvalidCode("8FWC2η45+G6"),
        InvalidCode("8FWC2345+G6+"),
        InvalidCode("8FWC2345G6+"),
        InvalidCode("8FWC2300+G6"),
        InvalidCode("8FWC2300+00"),
        InvalidCode("WC2300+G6g"),
        InvalidCode("WC2345+G"),
        InvalidCode("WC2300+")
    };


    public class TheIsValidCodeMethod {
        [Test]
        public void ShouldDetermineValidityOfACode() {
            foreach (TestData testData in TestDataList) {
                Assert.AreEqual(testData.IsValid, OpenLocationCode.IsValid(testData.Code),
                    $"Validity of code {testData.Code} is wrong.");
            }
        }

        [Test]
        public void ShouldValidateCodesExceedingMaximumLength() {
            string code = OpenLocationCode.Encode(51.3701125, -10.202665625, 1000000);
            Assert.True(OpenLocationCode.IsValid(code), "Code should be valid.");

            // Extend the code with a valid character and make sure it is still valid.
            Assert.True(OpenLocationCode.IsValid(code + "W"),
                "Too long code with all valid characters should be valid.");

            // Extend the code with an invalid character and make sure it is invalid.
            Assert.False(OpenLocationCode.IsValid(code + "U"),
                "Too long code with invalid character should be invalid.");
        }
    }

    public class TheIsShortMethod {
        [Test]
        public void ShouldDetermineShortnessOfACode() {
            foreach (TestData testData in TestDataList) {
                Assert.AreEqual(testData.IsShort, OpenLocationCode.IsShort(testData.Code),
                    $"Shortness of code {testData.Code} is wrong.");
            }
        }
    }

    public class TheIsFullMethod {
        [Test]
        public void ShouldDetermineFullnessOfACode() {
            foreach (TestData testData in TestDataList) {
                Assert.AreEqual(testData.IsFull, OpenLocationCode.IsFull(testData.Code),
                    $"Fullness of code {testData.Code} is wrong.");
            }
        }
    }

    // Nonstandard
    public class TheIsPaddedMethod {
        [Test]
        public void ShouldDeterminePaddingOfACode() {
            foreach (TestData testData in TestDataList) {
                Assert.AreEqual(testData.IsPadded, OpenLocationCode.IsPadded(testData.Code),
                    $"Padding for code {testData.Code} is wrong.");
            }
        }
    }


    private static TestData FullCode(string code, bool padded = false) => new TestData(code, true, false, true, padded);

    private static TestData ShortCode(string code) => new TestData(code, true, true, false, false);

    private static TestData InvalidCode(string code) => new TestData(code, false, false, false, false);

    private struct TestData {

        internal TestData(string code, bool isValid, bool isShort, bool isFull, bool isPadded) {
            Code = code;
            IsValid = isValid;
            IsShort = isShort;
            IsFull = isFull;
            IsPadded = isPadded; // Nonstandard
        }

        internal string Code { get; }
        internal bool IsValid { get; }
        internal bool IsShort { get; }
        internal bool IsFull { get; }
        internal bool IsPadded { get; }

    }

}
