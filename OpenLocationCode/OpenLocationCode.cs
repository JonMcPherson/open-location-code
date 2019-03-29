using System;
using System.Text;

namespace Google.OpenLocationCode {
    /// <summary>
    /// Convert locations to and from convenient codes known as Open Location Codes
    /// or <see cref="https://plus.codes/">Plus Codes</see>
    /// <para>
    /// Open Location Codes are short, ~10 character codes that can be used instead of street
    /// addresses. The codes can be generated and decoded offline, and use a reduced character set that
    /// minimises the chance of codes including words.
    /// </para>
    /// This implements the
    /// <see cref="https://github.com/google/open-location-code/blob/master/API.txt">Open Location Code API</see>
    /// through the static methods:
    /// <list type="bullet">
    /// <item><see cref="IsValid(string)"/></item>
    /// <item><see cref="IsShort(string)"/></item>
    /// <item><see cref="IsFull(string)"/></item>
    /// <item><see cref="Encode(double, double, int)"/></item>
    /// <item><see cref="Decode(string)"/></item>
    /// <item><see cref="Shorten(string, double, double)"/></item>
    /// <item><see cref="ShortCode.RecoverNearest(string, double, double)"/></item>
    /// </list>
    /// Additionally an object type is provided which can be created using the constructors:
    /// <list type="bullet">
    /// <item><see cref="OpenLocationCode(string)"/></item>
    /// <item><see cref="OpenLocationCode(double, double, int)"/></item>
    /// <item><see cref="ShortCode(string)"/></item>
    /// </list>
    /// <example><code>
    /// OpenLocationCode code = new OpenLocationCode("7JVW52GR+2V");
    /// OpenLocationCode code = new OpenLocationCode(27.175063, 78.042188);
    /// OpenLocationCode code = new OpenLocationCode(27.175063, 78.042188, 11);
    /// OpenLocationCode.ShortCode shortCode = new OpenLocationCode.ShortCode("52GR+2V");
    /// </code></example>
    /// 
    /// With a code object you can invoke the various methods such as to shorten the code:
    /// <example><code>
    /// OpenLocationCode.ShortCode shortCode = code.shorten(27.176, 78.05);
    /// OpenLocationCode recoveredCode = shortCode.recoverNearest(27.176, 78.05);
    /// </code></example>
    /// Or decode the <see cref="CodeArea"/> coordinates.
    /// <example><code>
    /// CodeArea codeArea = code.decode()
    /// </code></example>
    /// </summary>
    public sealed class OpenLocationCode {

        /// <summary>
        /// Provides a normal precision code, approximately 14x14 meters.
        /// </summary>
        public const int CodePrecisionNormal = 10;

        /// <summary>
        /// Provides an extra precision code, approximately 2x3 meters.
        /// </summary>
        public const int CodePrecisionExtra = 11;


        // A separator used to break the code into two parts to aid memorability.
        internal const char SeparatorCharacter = '+';

        // The number of characters to place before the separator.
        internal const int SeparatorPosition = 8;

        // The character used to pad codes.
        internal const char PaddingCharacter = '0';

        // The character set used to encode the digit values.
        internal const string CodeAlphabet = "23456789CFGHJMPQRVWX";

        // The base to use to convert numbers to/from.
        internal static readonly int EncodingBase = CodeAlphabet.Length;

        // The encoding base squared also rep
        internal static readonly int EncodingBaseSquared = EncodingBase * EncodingBase;

        // The maximum value for latitude in degrees.
        internal const int LatitudeMax = 90;

        // The maximum value for longitude in degrees.
        internal const int LongitudeMax = 180;

        // Maximum code length using just lat/lng pair encoding.
        internal const int PairCodeLength = 10;

        // Maximum code length for any plus code
        internal const int MaxCodeLength = 15;

        // Number of columns in the grid refinement method.
        internal const int RefinementGridColumns = 4;

        // Number of rows in the grid refinement method.
        internal const int RefinementGridRows = 5;

        // The maximum latitude digit value for the first grid layer
        internal const int FirstLatitudeDigitValueMax = 8; // lat -> 90

        // The maximum longitude digit value for the first grid layer
        internal const int FirstLongitudeDigitValueMax = 17; // lon -> 180

        // The ASCII integer of the minimum digit character used as the offset for indexed code digits
        private static readonly int IndexedDigitValueOffset = CodeAlphabet[0]; // 50

        // The digit values indexed by the character ASCII integer for efficient lookup of a digit value by its character
        private static readonly int[] IndexedDigitValues = new int[(CodeAlphabet[CodeAlphabet.Length - 1] - IndexedDigitValueOffset) + 1]; // int[38]

        static OpenLocationCode() {
            for (int i = 0, digitVal = 0; i < IndexedDigitValues.Length; i++) {
                int digitIndex = CodeAlphabet[digitVal] - IndexedDigitValueOffset;
                IndexedDigitValues[i] = (digitIndex == i) ? digitVal++ : -1;
            }
        }


        /// <summary>
        /// Creates an Open Location Code object for the provided full code (or <see cref="CodeDigits"/>).
        /// Use <see cref="ShortCode"/> for short codes.
        /// </summary>
        /// <param name="code">A valid full Open Location Code or <see cref="CodeDigits"/></param>
        /// <exception cref="ArgumentException">If the code is null, not valid, or not full.</exception>
        public OpenLocationCode(string code) {
            if (code == null) {
                throw new ArgumentException("code cannot be null");
            }
            Code = NormalizeCode(code.ToUpper());
            if (!IsValidUpperCase(Code) || !IsCodeFull(Code)) {
                throw new ArgumentException($"The provided code '{code}' is not a valid full Open Location Code (or code digits).");
            }
            CodeDigits = TrimCode(Code);
        }

        /// <summary>
        /// Creates Open Location Code.
        /// </summary>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        /// <param name="codeLength">The number of digits in the code (Default: <see cref="CodePrecisionNormal"/>).</param>
        /// <exception cref="ArgumentException">If the code length is not valid.</exception>
        public OpenLocationCode(double latitude, double longitude, int codeLength = CodePrecisionNormal) {
            Code = Encode(latitude, longitude, codeLength);
            CodeDigits = TrimCode(Code);
        }

        /// <summary>
        /// Creates Open Location Code.
        /// </summary>
        /// <param name="coordinates">The geographic coordinates.</param>
        /// <param name="codeLength">The desired number of digits in the code.</param>
        public OpenLocationCode(GeoPoint coordinates, int codeLength = CodePrecisionNormal) :
            this(coordinates.Latitude, coordinates.Longitude, codeLength) { }

        // Used internally for codes which are guaranteed to be valid
        internal OpenLocationCode(char[] codeDigits) {
            CodeDigits = new string(codeDigits);
            Code = NormalizeCode(CodeDigits);
        }


        /// <summary>
        /// The code which is a valid full Open Location Code (plus code)
        /// </summary>
        /// <value>The string representation of the code.</value>
        public string Code { get; }

        /// <summary>
        /// The digits of the code which excludes the separator '+' character and any padding '0' characters.
        /// This is useful to more concisely represent or encode a full Open Location Code
        /// since the code digits can be normalized back into a valid full code.
        /// </summary>
        /// <example>"8FWC2300+" -> "8FWC23", "8FWC2345+G6" -> "8FWC2345G6"</example>
        /// <value>The string representation of the code digits</value>
        /// <remarks>This is a nonstandard code format.</remarks>
        public string CodeDigits { get; }


        /// <summary>
        /// Decodes this Open Location Code into CodeArea object encapsulating latitude/longitude bounding box.
        /// </summary>
        /// <returns>The decoded CodeArea for this Open Location Code.</returns>
        public CodeArea Decode() {
            return DecodeValid(CodeDigits);
        }


        /// <summary>
        /// Determines if this Open Location Code is padded which is defined by <see cref="IsPadded(string)"/>.
        /// </summary>
        /// <returns><c>true</c>, if this Open Location Code is a padded, <c>false</c> otherwise.</returns>
        public bool IsPadded() {
            return IsCodePadded(Code);
        }


        /// <summary>
        /// Shorten this full Open Location Code by removing four or six digits (depending on the provided reference point).
        /// It removes as many digits as possible.
        /// </summary>
        /// <returns>A new <see cref="ShortCode"/> instance shortened from this Open Location Code.</returns>
        /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
        /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
        /// <exception cref="InvalidOperationException">If this code is padded (<see cref="IsPadded()"/>).</exception>
        /// <exception cref="ArgumentException">If the reference point is too far from this code's center point.</exception>
        public ShortCode Shorten(double referenceLatitude, double referenceLongitude) {
            return ShortenValid(Decode(), Code, referenceLatitude, referenceLongitude);
        }


        /// <returns>Whether the bounding box specified by this Open Location Code contains provided point.</returns>
        /// <remarks>Convenient alternative to Decode().Contains()</remarks>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        public bool Contains(double latitude, double longitude) {
            return Decode().Contains(longitude, latitude);
        }


        /// <summary>
        /// Determines whether the specified object is an OpenLocationCode with the same <see cref="Code"/> as this OpenLocationCode.
        /// </summary>
        /// <param name="obj">The object to compare with this OpenLocationCode.</param>
        /// <returns><c>true</c> if the specified object is equal to this OpenLocationCode, <c>false</c> otherwise.</returns>
        public override bool Equals(object obj) {
            return this == obj || (obj is OpenLocationCode olc && olc.Code == Code);
        }

        /// <returns>The hashcode of the <see cref="Code"/> string.</returns>
        public override int GetHashCode() {
            return Code.GetHashCode();
        }

        /// <returns>The <see cref="Code"/> string.</returns>
        public override string ToString() {
            return Code;
        }


        // API Spec Implementation

        /// <summary>
        /// Determines if the provided string is a valid Open Location Code sequence.
        /// A valid Open Location Code can be either full or short (XOR).
        /// </summary>
        /// <returns><c>true</c>, if the provided code is a valid Open Location Code, <c>false</c> otherwise.</returns>
        /// <param name="code">The code string to check.</param>
        public static bool IsValid(string code) {
            return code != null && IsValidUpperCase(code.ToUpper());
        }

        private static bool IsValidUpperCase(string code) {
            if (code.Length < 2) {
                return false;
            }

            // There must be exactly one separator.
            int separatorIndex = code.IndexOf(SeparatorCharacter);
            if (separatorIndex == -1) {
                return false;
            }
            if (separatorIndex != code.LastIndexOf(SeparatorCharacter)) {
                return false;
            }
            // There must be an even number of at most eight characters before the separator.
            if (separatorIndex % 2 != 0 || separatorIndex > SeparatorPosition) {
                return false;
            }

            // Check first two characters: only some values from the alphabet are permitted.
            if (separatorIndex == SeparatorPosition) {
                // First latitude character can only have first 9 values.
                if (CodeAlphabet.IndexOf(code[0]) > FirstLatitudeDigitValueMax) {
                    return false;
                }

                // First longitude character can only have first 18 values.
                if (CodeAlphabet.IndexOf(code[1]) > FirstLongitudeDigitValueMax) {
                    return false;
                }
            }

            // Check the characters before the separator.
            bool paddingStarted = false;
            for (int i = 0; i < separatorIndex; i++) {
                if (paddingStarted) {
                    // Once padding starts, there must not be anything but padding.
                    if (code[i] != PaddingCharacter) {
                        return false;
                    }
                    continue;
                }
                if (CodeAlphabet.IndexOf(code[i]) != -1) {
                    continue;
                }
                if (PaddingCharacter == code[i]) {
                    paddingStarted = true;
                    // Padding can start on even character: 2, 4 or 6.
                    if (i != 2 && i != 4 && i != 6) {
                        return false;
                    }
                    continue;
                }
                return false; // Illegal character.
            }

            // Check the characters after the separator.
            if (code.Length > separatorIndex + 1) {
                if (paddingStarted) {
                    return false;
                }
                // Only one character after separator is forbidden.
                if (code.Length == separatorIndex + 2) {
                    return false;
                }
                for (int i = separatorIndex + 1; i < code.Length; i++) {
                    if (CodeAlphabet.IndexOf(code[i]) == -1) {
                        return false;
                    }
                }
            } else if (paddingStarted) {

            }

            return true;
        }

        /// <summary>
        /// Determines if a code is a valid short Open Location Code.
        /// <para>
        /// A short Open Location Code is a sequence created by removing an even number
        /// of characters from the start of a full Open Location Code. Short codes must
        /// include the separator character and it must be before eight or less characters.
        /// </para>
        /// </summary>
        /// <returns><c>true</c>, if the provided code is a valid short Open Location Code, <c>false</c> otherwise.</returns>
        /// <param name="code">The code string to check.</param>
        public static bool IsShort(string code) {
            return IsValid(code) && IsCodeShort(code);
        }

        private static bool IsCodeShort(string code) {
            int separatorIndex = code.IndexOf(SeparatorCharacter);
            return separatorIndex >= 0 && separatorIndex < SeparatorPosition;
        }

        /// <summary>
        /// Determines if a code is a valid full Open Location Code.
        /// <para>
        /// Full codes must include the separator character and it must be after eight characters.
        /// </para>
        /// </summary>
        /// <returns><c>true</c>, if the provided code is a valid full Open Location Code, <c>false</c> otherwise.</returns>
        /// <param name="code">The code string to check.</param>
        public static bool IsFull(string code) {
            return IsValid(code) && IsCodeFull(code);
        }

        private static bool IsCodeFull(string code) {
            return code.IndexOf(SeparatorCharacter) == SeparatorPosition;
        }

        /// <summary>
        /// Determines if a code is a valid padded Open Location Code.
        /// <para>
        /// An Open Location Code is padded when it has only 2, 4, or 6 valid digits
        /// followed by zero <c>'0'</c> as padding to form a full 8 digit code.
        /// If this returns <c>true</c> that that the code is padded,
        /// then it is also implied to be full since short codes cannot be padded.
        /// </para>
        /// </summary>
        /// <returns><c>true</c>, if the provided code is a valid Open Location Code and is a padded, <c>false</c> otherwise.</returns>
        /// <param name="code">The code string to check.</param>
        /// <remarks>
        /// This is not apart of the API specification but it is useful to check if a code can
        /// <see cref="Shorten(string, double, double)"/> since padded codes cannot be shortened.
        /// </remarks>
        public static bool IsPadded(string code) {
            return IsValid(code) && IsCodePadded(code);
        }

        private static bool IsCodePadded(string code) {
            return code.IndexOf(PaddingCharacter) >= 0;
        }


        /// <summary>
        /// Encodes latitude/longitude into a full Open Location Code of the provided length.
        /// </summary>
        /// <returns>The encoded Open Location Code.</returns>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        /// <param name="codeLength">The number of digits in the code (Default: <see cref="CodePrecisionNormal"/>).</param>
        /// <exception cref="ArgumentException">If the code length is not valid.</exception>
        public static string Encode(double latitude, double longitude, int codeLength = CodePrecisionNormal) {
            // Limit the maximum number of digits in the code.
            codeLength = Math.Min(codeLength, MaxCodeLength);
            // Check that the code length requested is valid.
            if (codeLength < 4 || (codeLength < PairCodeLength && codeLength % 2 == 1)) {
                throw new ArgumentException($"Illegal code length {codeLength}.");
            }
            // Ensure that latitude and longitude are valid.
            latitude = ClipLatitude(latitude);
            longitude = NormalizeLongitude(longitude);

            // Latitude 90 needs to be adjusted to be just less, so the returned code can also be decoded.
            if ((int) latitude == LatitudeMax) {
                latitude = latitude - 0.9 * ComputeLatitudePrecision(codeLength);
            }

            // Adjust latitude and longitude to be in positive number ranges.
            double remainingLatitude = latitude + LatitudeMax;
            double remainingLongitude = longitude + LongitudeMax;

            // Count how many digits have been created.
            int generatedDigits = 0;
            // Store the code.
            StringBuilder codeBuilder = new StringBuilder();
            // The precisions are initially set to ENCODING_BASE^2 because they will be immediately divided.
            double latPrecision = EncodingBaseSquared;
            double lngPrecision = EncodingBaseSquared;
            while (generatedDigits < codeLength) {
                if (generatedDigits < PairCodeLength) {
                    // Use the normal algorithm for the first set of digits.
                    latPrecision /= EncodingBase;
                    lngPrecision /= EncodingBase;
                    int latDigit = (int) Math.Floor(remainingLatitude / latPrecision);
                    int lngDigit = (int) Math.Floor(remainingLongitude / lngPrecision);
                    remainingLatitude -= latPrecision * latDigit;
                    remainingLongitude -= lngPrecision * lngDigit;
                    codeBuilder.Append(CodeAlphabet[latDigit]);
                    codeBuilder.Append(CodeAlphabet[lngDigit]);
                    generatedDigits += 2;
                } else {
                    // Use the 4x5 grid for remaining digits.
                    latPrecision /= RefinementGridRows;
                    lngPrecision /= RefinementGridColumns;
                    int row = (int) Math.Floor(remainingLatitude / latPrecision);
                    int col = (int) Math.Floor(remainingLongitude / lngPrecision);
                    remainingLatitude -= latPrecision * row;
                    remainingLongitude -= lngPrecision * col;
                    codeBuilder.Append(CodeAlphabet[row * RefinementGridColumns + col]);
                    generatedDigits += 1;
                }
                // If we are at the separator position, add the separator.
                if (generatedDigits == SeparatorPosition) {
                    codeBuilder.Append(SeparatorCharacter);
                }
            }
            // If the generated code is shorter than the separator position, pad the code and add the separator.
            if (generatedDigits < SeparatorPosition) {
                codeBuilder.Append(PaddingCharacter, SeparatorPosition - generatedDigits);
                codeBuilder.Append(SeparatorCharacter);
            }
            return codeBuilder.ToString();
        }

        /// <summary>
        /// Decodes an Open Location Code into CodeArea object encapsulating latitude/longitude bounding box.
        /// </summary>
        /// <returns>The decoded CodeArea for the given location code.</returns>
        /// <param name="code">The Open Location Code to be decoded.</param>
        /// <exception cref="ArgumentException">If the code is not valid or not full.</exception>
        public static CodeArea Decode(string code) {
            code = ValidateCode(code);
            if (!IsCodeFull(code)) {
                throw new ArgumentException($"{nameof(Decode)}(code: {code}) - code cannot be short.");
            }
            return DecodeValid(TrimCode(code));
        }

        private static CodeArea DecodeValid(string codeDigits) {
            int codeLength = Math.Min(codeDigits.Length, MaxCodeLength);

            int digit = 0;
            // The precisions are initially set to ENCODING_BASE^2 because they will be immediately divided.
            double latPrecision = EncodingBaseSquared;
            double lngPrecision = EncodingBaseSquared;
            // Save the coordinates.
            double southLatitude = 0;
            double westLongitude = 0;

            // Decode the digits.
            while (digit < codeLength) {
                if (digit < PairCodeLength) {
                    // Decode a pair of digits, the first being latitude and the second being longitude.
                    latPrecision /= EncodingBase;
                    lngPrecision /= EncodingBase;
                    int digitVal = DigitValueOf(codeDigits[digit]);
                    southLatitude += latPrecision * digitVal;
                    digitVal = DigitValueOf(codeDigits[digit + 1]);
                    westLongitude += lngPrecision * digitVal;
                    digit += 2;
                } else {
                    // Use the 4x5 grid for digits after 10.
                    int digitVal = DigitValueOf(codeDigits[digit]);
                    int row = digitVal / RefinementGridColumns;
                    int col = digitVal % RefinementGridColumns;
                    latPrecision /= RefinementGridRows;
                    lngPrecision /= RefinementGridColumns;
                    southLatitude += latPrecision * row;
                    westLongitude += lngPrecision * col;
                    digit += 1;
                }
            }
            return new CodeArea(
                southLatitude - LatitudeMax,
                westLongitude - LongitudeMax,
                (southLatitude - LatitudeMax) + latPrecision,
                (westLongitude - LongitudeMax) + lngPrecision
            );
        }

        /// <summary>
        /// Shorten a full Open Location Code by removing four or six digits (depending on the provided reference point).
        /// It removes as many digits as possible.
        /// </summary>
        /// <returns>A new <see cref="ShortCode"/> instance shortened from the the provided Open Location Code.</returns>
        /// <param name="code">The Open Location Code to shorten.</param>
        /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
        /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
        /// <exception cref="ArgumentException">If the code is not valid, not full, or is padded.</exception>
        /// <exception cref="ArgumentException">If the reference point is too far from the code's center point.</exception>
        public static ShortCode Shorten(string code, double referenceLatitude, double referenceLongitude) {
            code = ValidateCode(code);
            if (!IsCodeFull(code)) {
                throw new ArgumentException($"{nameof(Shorten)}(code: \"{code}\") - code cannot be short.");
            }
            if (IsCodePadded(code)) {
                throw new ArgumentException($"{nameof(Shorten)}(code: \"{code}\") - code cannot be padded.");
            }
            return ShortenValid(Decode(code), code, referenceLatitude, referenceLongitude);
        }

        private static ShortCode ShortenValid(CodeArea codeArea, string code, double referenceLatitude, double referenceLongitude) {
            GeoPoint center = codeArea.Center;
            double range = Math.Max(
                Math.Abs(referenceLatitude - center.Latitude),
                Math.Abs(referenceLongitude - center.Longitude)
            );
            // We are going to check to see if we can remove three pairs, two pairs or just one pair of
            // digits from the code.
            for (int i = 4; i >= 1; i--) {
                // Check if we're close enough to shorten. The range must be less than 1/2
                // the precision to shorten at all, and we want to allow some safety, so
                // use 0.3 instead of 0.5 as a multiplier.
                if (range < (ComputeLatitudePrecision(i * 2) * 0.3)) {
                    // We're done.
                    return new ShortCode(code.Substring(i * 2), valid: true);
                }
            }
            throw new ArgumentException("Reference location is too far from the Open Location Code center.");
        }

        private static string ValidateCode(string code) {
            if (code == null) {
                throw new ArgumentException("code cannot be null");
            }
            code = code.ToUpper();
            if (!IsValidUpperCase(code)) {
                throw new ArgumentException($"code '{code}' is not a valid Open Location Code.");
            }
            return code;
        }


        // Private static utility methods.

        internal static int DigitValueOf(char digitChar) {
            return IndexedDigitValues[digitChar - IndexedDigitValueOffset];
        }

        private static double ClipLatitude(double latitude) {
            return Math.Min(Math.Max(latitude, -LatitudeMax), LatitudeMax);
        }

        private static double NormalizeLongitude(double longitude) {
            while (longitude < -LongitudeMax) {
                longitude = longitude + LongitudeMax * 2;
            }
            while (longitude >= LongitudeMax) {
                longitude = longitude - LongitudeMax * 2;
            }
            return longitude;
        }

        /// <summary>
        /// Normalize a location code by adding the separator '+' character and any padding '0' characters
        /// that are necessary to form a valid location code.
        /// </summary>
        private static string NormalizeCode(string code) {
            if (code.Length < SeparatorPosition) {
                return code + new string(PaddingCharacter, SeparatorPosition - code.Length) + SeparatorCharacter;
            } else if (code.Length == SeparatorPosition) {
                return code + SeparatorCharacter;
            } else if (code[SeparatorPosition] != SeparatorCharacter) {
                return code.Substring(0, SeparatorPosition) + SeparatorCharacter + code.Substring(SeparatorPosition);
            }
            return code;
        }

        /// <summary>
        /// Trim a location code by removing the separator '+' character and any padding '0' characters
        /// resulting in only the code digits.
        /// </summary>
        internal static string TrimCode(string code) {
            StringBuilder codeBuilder = new StringBuilder();
            foreach (char c in code) {
                if (c != PaddingCharacter && c != SeparatorCharacter) {
                    codeBuilder.Append(c);
                }
            }
            return codeBuilder.Length != code.Length ? codeBuilder.ToString() : code;
        }

        /// <summary>
        /// Compute the latitude precision value for a given code length. Lengths &lt;= 10 have the same
        /// precision for latitude and longitude, but lengths > 10 have different precisions due to the
        /// grid method having fewer columns than rows.
        /// </summary>
        /// <remarks>Copied from the JS implementation.</remarks>
        private static double ComputeLatitudePrecision(int codeLength) {
            if (codeLength <= CodePrecisionNormal) {
                return Math.Pow(EncodingBase, codeLength / -2 + 2);
            }
            return Math.Pow(EncodingBase, -3) / Math.Pow(RefinementGridRows, codeLength - PairCodeLength);
        }


        /// <summary>
        /// A class representing a short Open Location Code which is defined by <see cref="IsShort(string)"/>.
        /// <para>
        /// A ShortCode instance can be created the following ways:
        /// <list type="bullet">
        /// <item><see cref="Shorten(double, double)"/> - Shorten a full Open Location Code</item>
        /// <item><see cref="ShortCode(string)"/> - Construct for a valid short Open Location Code</item>
        /// </list>
        /// </para>
        /// A ShortCode can be recovered back to a full Open Location Code using <see cref="RecoverNearest(double, double)"/>
        /// or using the static method <see cref="RecoverNearest(string, double, double)"/> (as defined by the spec).
        /// </summary>
        public class ShortCode {
        
            /// <summary>
            /// Creates a <see cref="ShortCode"/> object for the provided short Open Location Code.
            /// Use <see cref="OpenLocationCode"/> for full codes.
            /// </summary>
            /// <param name="shortCode">A valid short Open Location Code</param>
            /// <exception cref="ArgumentException">If the code is null, not valid, or not short.</exception>
            public ShortCode(string shortCode) {
                Code = ValidateShortCode(shortCode);
            }

            // Used internally for short codes which are guaranteed to be valid
            // ReSharper disable once UnusedParameter.Local - because public constructor 
            internal ShortCode(string shortCode, bool valid) {
                Code = shortCode;
            }

            /// <summary>
            /// Gets the code which is a valid short Open Location Code (plus code)
            /// </summary>
            /// <example>9QCJ+2VX</example>
            /// <value>The string representation of the short code.</value>
            public string Code { get; }


            /// <returns>
            /// A new OpenLocationCode instance representing a full Open Location Code
            /// recovered from this (short) Open Location Code, given the reference location.
            /// </returns>
            /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
            /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
            public OpenLocationCode RecoverNearest(double referenceLatitude, double referenceLongitude) {
                return RecoverNearestValid(Code, referenceLatitude, referenceLongitude);
            }


            public override bool Equals(object obj) {
                return obj == this || (obj is ShortCode shortCode && shortCode.Code == Code);
            }

            public override int GetHashCode() {
                return Code.GetHashCode();
            }

            public override string ToString() {
                return Code;
            }


            /// <returns>
            /// A new OpenLocationCode instance representing a full Open Location Code
            /// recovered from the provided short Open Location Code, given the reference location.
            /// </returns>
            /// <param name="shortCode">The valid short Open Location Code to recover</param>
            /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
            /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
            /// <exception cref="ArgumentException">If the code is null, not valid, or not short.</exception>
            public static OpenLocationCode RecoverNearest(string shortCode, double referenceLatitude, double referenceLongitude) {
                return RecoverNearestValid(ValidateShortCode(shortCode), referenceLatitude, referenceLongitude);
            }

            private static OpenLocationCode RecoverNearestValid(string shortCode, double referenceLatitude, double referenceLongitude) {
                referenceLatitude = ClipLatitude(referenceLatitude);
                referenceLongitude = NormalizeLongitude(referenceLongitude);

                int digitsToRecover = SeparatorPosition - shortCode.IndexOf(SeparatorCharacter);
                // The precision (height and width) of the missing prefix in degrees.
                double prefixPrecision = Math.Pow(EncodingBase, 2 - (digitsToRecover / 2));

                // Use the reference location to generate the prefix.
                string recoveredPrefix =
                    new OpenLocationCode(referenceLatitude, referenceLongitude).Code.Substring(0, digitsToRecover);
                // Combine the prefix with the short code and decode it.
                OpenLocationCode recovered = new OpenLocationCode(recoveredPrefix + shortCode);
                GeoPoint recoveredCodeAreaCenter = recovered.Decode().Center;
                // Work out whether the new code area is too far from the reference location. If it is, we
                // move it. It can only be out by a single precision step.
                double recoveredLatitude = recoveredCodeAreaCenter.Latitude;
                double recoveredLongitude = recoveredCodeAreaCenter.Longitude;

                // Move the recovered latitude by one precision up or down if it is too far from the reference,
                // unless doing so would lead to an invalid latitude.
                double latitudeDiff = recoveredLatitude - referenceLatitude;
                if (latitudeDiff > prefixPrecision / 2 && recoveredLatitude - prefixPrecision > -LatitudeMax) {
                    recoveredLatitude -= prefixPrecision;
                } else if (latitudeDiff < -prefixPrecision / 2 && recoveredLatitude + prefixPrecision < LatitudeMax) {
                    recoveredLatitude += prefixPrecision;
                }

                // Move the recovered longitude by one precision up or down if it is too far from the
                // reference.
                double longitudeDiff = recoveredCodeAreaCenter.Longitude - referenceLongitude;
                if (longitudeDiff > prefixPrecision / 2) {
                    recoveredLongitude -= prefixPrecision;
                } else if (longitudeDiff < -prefixPrecision / 2) {
                    recoveredLongitude += prefixPrecision;
                }

                return new OpenLocationCode(recoveredLatitude, recoveredLongitude, recovered.CodeDigits.Length);
            }

            private static string ValidateShortCode(string shortCode) {
                if (shortCode == null) {
                    throw new ArgumentException("shortCode cannot be null");
                }
                shortCode = shortCode.ToUpper();
                if (!IsValidUpperCase(shortCode) || !IsCodeShort(shortCode)) {
                    throw new ArgumentException($"The provided code '{shortCode}' is not a valid short Open Location Code.");
                }
                return shortCode;
            }

        }

    }
}
