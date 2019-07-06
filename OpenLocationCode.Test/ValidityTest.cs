using System.Collections.Generic;
using Google.OpenLocationCode;
using NUnit.Framework;

public static class ValidityTest {

    // Test cases for validating codes and determining code type
    // https://github.com/google/open-location-code/blob/master/test_data/validityTests.csv
    private static readonly IEnumerable<TestData> ValidityTestCases = TestDataUtils.ReadTestData<TestData>("validity.csv");


    public class TheIsValidMethod {
        [Test]
        public void ShouldDetermineValidityOfACode() {
            foreach (TestData testData in ValidityTestCases) {
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
            foreach (TestData testData in ValidityTestCases) {
                Assert.AreEqual(testData.IsShort, OpenLocationCode.IsShort(testData.Code),
                    $"Shortness of code {testData.Code} is wrong.");
            }
        }
    }

    public class TheIsFullMethod {
        [Test]
        public void ShouldDetermineFullnessOfACode() {
            foreach (TestData testData in ValidityTestCases) {
                Assert.AreEqual(testData.IsFull, OpenLocationCode.IsFull(testData.Code),
                    $"Fullness of code {testData.Code} is wrong.");
            }
        }
    }

    // Nonstandard
    public class TheIsPaddedMethod {
        [Test]
        public void ShouldDeterminePaddingOfACode() {
            foreach (TestData testData in ValidityTestCases) {
                Assert.AreEqual(testData.IsPadded, OpenLocationCode.IsPadded(testData.Code),
                    $"Padding for code {testData.Code} is wrong.");
            }
        }
    }

    public class TestData {

        public string Code { get; set; }
        public bool IsValid { get; set; }
        public bool IsShort { get; set; }
        public bool IsFull { get; set; }
        public bool IsPadded { get; set; }

    }

}
