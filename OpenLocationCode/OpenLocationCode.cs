using System;
using System.Text;

namespace Google.OpenLocationCode {
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
        private const char SeparatorCharacter = '+';

        // The number of characters to place before the separator.
        private const int SeparatorPosition = 8;

        // The character used to pad codes.
        private const char PaddingCharacter = '0';

        // The character set used to encode the digit values.
        internal const string CodeAlphabet = "23456789CFGHJMPQRVWX";

        // The maximum digit value in the code alphabet
        internal static readonly int MaxDigitValue = CodeAlphabet.Length - 1;

        // The maximum latitude digit value for the first grid layer
        private const int FirstLatitudeDigitValueMax = 8; // lat -> 90

        // The maximum longitude digit value for the first grid layer
        internal const int FirstLongitudeDigitValueMax = 17; // lon -> 180

        // The ASCII integer of the minimum digit character used as the offset for indexed code digits
        private static readonly int IndexedDigitValueOffset = CodeAlphabet[0]; // 50

        // The digit values indexed by the character ASCII integer for efficient lookup of a digit value by its character
        private static readonly int[] IndexedDigitValues = new int[(CodeAlphabet[MaxDigitValue] - IndexedDigitValueOffset) + 1]; // int[38]

        // The base to use to convert numbers to/from.
        internal static readonly int EncodingBase = CodeAlphabet.Length;

        private static readonly int EncodingBaseSquared = EncodingBase * EncodingBase;

        // The maximum value for latitude in degrees.
        private const int LatitudeMax = 90;

        // The maximum value for longitude in degrees.
        private const int LongitudeMax = 180;

        // Maximum code length using just lat/lng pair encoding.
        internal const int PairCodeLength = 10;

        // Maximum code length for any plus code
        public static readonly int MaxCodeLength = 15;

        // Number of columns in the grid refinement method.
        internal const int RefinementGridColumns = 4;

        // Number of rows in the grid refinement method.
        private const int RefinementGridRows = 5;

        static OpenLocationCode() {
            for (int i = 0, digitVal = 0; i < IndexedDigitValues.Length; i++) {
                int digitIndex = CodeAlphabet[digitVal] - IndexedDigitValueOffset;
                IndexedDigitValues[i] = (digitIndex == i) ? digitVal++ : -1;
            }
        }


        private readonly Lazy<CodeArea> _codeArea;


        /// <summary>
        /// Creates Open Location Code object for the provided code.
        /// </summary>
        /// <param name="code">A valid OLC code. Can be a full code or a shortened code.</param>
        /// <exception cref="ArgumentException">If the code is null or not valid.</exception>
        public OpenLocationCode(string code) {
            if (code == null) {
                throw new ArgumentException("code cannot be null");
            }
            Code = code.ToUpper();
            if (!IsValidCodeUpperCase(Code)) {
                throw new ArgumentException($"The provided code '{code}' is not a valid Open Location Code.");
            }

            _codeArea = new Lazy<CodeArea>(() => DecodeValid(code));
        }

        /// <summary>
        /// Creates Open Location Code.
        /// </summary>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        /// <param name="codeLength">The desired number of digits in the code.</param>
        /// <exception cref="ArgumentException">If the code lenght is not valid.</exception>
        public OpenLocationCode(double latitude, double longitude, int codeLength) {
            // Limit the maximum number of digits in the code.
            codeLength = Math.Min(codeLength, MaxCodeLength);
            // Check that the code length requested is valid.
            if (codeLength < 4 || (codeLength < PairCodeLength && codeLength % 2 == 1)) {
                throw new ArgumentException("Illegal code length " + codeLength);
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
            // If the generated code is shorter than the separator position, pad the code and add the
            // separator.
            if (generatedDigits < SeparatorPosition) {
                for (; generatedDigits < SeparatorPosition; generatedDigits++) {
                    codeBuilder.Append(PaddingCharacter);
                }
                codeBuilder.Append(SeparatorCharacter);
            }
            Code = codeBuilder.ToString();
            _codeArea = new Lazy<CodeArea>(() => DecodeValid(Code));
        }

        /// <summary>
        /// Creates Open Location Code.
        /// </summary>
        /// <param name="coordinates">The geographic coordinates.</param>
        /// <param name="codeLength">The desired number of digits in the code.</param>
        public OpenLocationCode(GeoPoint coordinates, int codeLength) :
            this(coordinates.Latitude, coordinates.Longitude, codeLength) { }

        /// <summary>
        /// Creates Open Location Code with the default precision length of 10.
        /// </summary>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        public OpenLocationCode(double latitude, double longitude) : this(latitude, longitude, CodePrecisionNormal) { }

        /// <summary>
        /// Creates Open Location Code with the default precision length of 10.
        /// </summary>
        /// <param name="coordinates">The geographic coordinates.</param>
        public OpenLocationCode(GeoPoint coordinates) : this(coordinates.Latitude, coordinates.Longitude) { }

        // Used internally for codes which are guaranteed to be valid
        internal OpenLocationCode(char[] codeChars) {
            Code = PadCode(new string(codeChars));
            _codeArea = new Lazy<CodeArea>(() => DecodeValid(Code));
        }


        /// <summary>
        /// Returns the string representation of the code.
        /// </summary>
        /// <value>The current code for objects.</value>
        public string Code { get; }


        /// <summary>
        /// Encodes latitude/longitude into a 10 digit Open Location Code.
        /// This method is equivalent to creating the OpenLocationCode object and getting the code from it.
        /// </summary>
        /// <returns>The encoded Open Location Code.</returns>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        public static string Encode(double latitude, double longitude) {
            return new OpenLocationCode(latitude, longitude).Code;
        }

        /// <summary>
        /// Encodes latitude/longitude into an Open Location Code of the provided length.
        /// This method is equivalent to creating the OpenLocationCode object and getting the code from it.
        /// </summary>
        /// <returns>The encoded Open Location Code.</returns>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        /// <param name="codeLength">The length of the code.</param>
        /// <exception cref="ArgumentException">If the code lenght is not valid.</exception>
        public static string Encode(double latitude, double longitude, int codeLength) {
            return new OpenLocationCode(latitude, longitude, codeLength).Code;
        }

        /// /// <summary>
        /// Decodes an Open Location Code into CodeArea object encapsulating latitude/longitude bounding box.
        /// </summary>
        /// <returns>The decoded CodeArea for the given location code.</returns>
        /// <param name="code">The Open Location Code to be decoded.</param>
        /// <exception cref="ArgumentException">If the code is not valid.</exception>
        /// <exception cref="InvalidOperationException">If the code is not full.</exception>
        public static CodeArea Decode(string code) {
            return new OpenLocationCode(code).Decode();
        }

        // Decode() without any validity checks
        private static CodeArea DecodeValid(string code) {
            if (!IsCodeFull(code)) {
                throw new InvalidOperationException($"Method Decode() could only be called on valid full codes, code was {code}.");
            }
            // Strip padding and separator characters out of the code.
            string decoded = TrimCode(code);
            int decodedCodeLength = Math.Min(decoded.Length, MaxCodeLength);

            int digit = 0;
            // The precisions are initially set to ENCODING_BASE^2 because they will be immediately divided.
            double latPrecision = EncodingBaseSquared;
            double lngPrecision = EncodingBaseSquared;
            // Save the coordinates.
            double southLatitude = 0;
            double westLongitude = 0;

            // Decode the digits.
            while (digit < decodedCodeLength) {
                if (digit < PairCodeLength) {
                    // Decode a pair of digits, the first being latitude and the second being longitude.
                    latPrecision /= EncodingBase;
                    lngPrecision /= EncodingBase;
                    int digitVal = DigitValueOf(decoded[digit]);
                    southLatitude += latPrecision * digitVal;
                    digitVal = DigitValueOf(decoded[digit + 1]);
                    westLongitude += lngPrecision * digitVal;
                    digit += 2;
                } else {
                    // Use the 4x5 grid for digits after 10.
                    int digitVal = DigitValueOf(decoded[digit]);
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
        /// Decodes this Open Location Code into CodeArea object encapsulating latitude/longitude bounding box.
        /// </summary>
        /// <returns>The decoded CodeArea for this Open Location Code.</returns>
        /// <exception cref="InvalidOperationException">If this code is not full.</exception>
        public CodeArea Decode() => _codeArea.Value;


        /// <returns><c>true</c>, if this Open Location Code is full, <c>false</c> otherwise.</returns>
        public bool IsFull() {
            return IsCodeFull(Code);
        }

        /// <returns><c>true</c>, if the provided Open Location Code is full, <c>false</c> otherwise.</returns>
        /// <param name="code">Code.</param>
        /// <exception cref="ArgumentException">If the code is not valid.</exception>
        public static bool IsFull(string code) {
            return new OpenLocationCode(code).IsFull();
        }

        /// <returns><c>true</c>, if the code is a valid full Open Location Code, <c>false</c> otherwise.</returns>
        /// <param name="code">The code to check.</param>
        public static bool IsFullCode(string code) {
            try {
                return new OpenLocationCode(code).IsFull();
            } catch (ArgumentException) {
                return false;
            }
        }

        private static bool IsCodeFull(string code) {
            return code.IndexOf(SeparatorCharacter) == SeparatorPosition;
        }


        /// <returns><c>true</c>, if this Open Location Code is short, <c>false</c> otherwise.</returns>
        public bool IsShort() {
            int separatorIndex = Code.IndexOf(SeparatorCharacter);
            return separatorIndex >= 0 && separatorIndex < SeparatorPosition;
        }

        /// <returns><c>true</c>, if the provided Open Location Code is short, <c>false</c> otherwise.</returns>
        /// <param name="code">Code.</param>
        /// <exception cref="ArgumentException">If the code is not valid.</exception>
        public static bool IsShort(string code) {
            return new OpenLocationCode(code).IsShort();
        }


        /// <summary>
        /// An Open Location Code is padded when it contains less than 8 valid digits
        /// </summary>
        /// <returns><c>true</c>, if this Open Location Code is a padded, <c>false</c> otherwise.</returns>
        public bool IsPadded() {
            return Code.IndexOf(PaddingCharacter) >= 0;
        }

        /// <summary>
        /// An Open Location Code is padded when it contains less than 8 valid digits
        /// </summary>
        /// <returns><c>true</c>, if the provided Open Location Code is a padded, <c>false</c> otherwise.</returns>
        /// <param name="code">The Open Location Code to check.</param>
        /// <exception cref="ArgumentException">If the code is not valid.</exception>
        public static bool IsPadded(string code) {
            return new OpenLocationCode(code).IsPadded();
        }


        /// <summary>
        /// Shorten this full Open Location Code by removing four or six digits (depending on the provided reference point).
        /// It removes as many digits as possible.
        /// </summary>
        /// <returns>A new OpenLocationCode instance shortened from this Open Location Code.</returns>
        /// <param name="referenceLatitude">the reference latitude in decimal degrees.</param>
        /// <param name="referenceLongitude">the reference longitude in decimal degrees.</param>
        /// <exception cref="InvalidOperationException">If this code is not full or is padded.</exception>
        /// <exception cref="ArgumentException">If the reference point is too far from this code's center point.</exception>
        public OpenLocationCode Shorten(double referenceLatitude, double referenceLongitude) {
            if (!IsFull()) {
                throw new InvalidOperationException("Shorten() method could only be called on a full code.");
            }
            if (IsPadded()) {
                throw new InvalidOperationException("Shorten() method can not be called on a padded code.");
            }

            GeoPoint center = Decode().Center;
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
                    return new OpenLocationCode(Code.Substring(i * 2));
                }
            }
            throw new ArgumentException("Reference location is too far from the Open Location Code center.");
        }

        /// <returns>
        /// A new OpenLocationCode instance representing a full Open Location Code from this
        /// (short) Open Location Code, given the reference location
        /// </returns>
        /// <param name="referenceLatitude">The reference latitude in decimal degrees.</param>
        /// <param name="referenceLongitude">The reference longitude in decimal degrees.</param>
        public OpenLocationCode Recover(double referenceLatitude, double referenceLongitude) {
            if (IsFull()) {
                // Note: each code is either full xor short, no other option.
                return this;
            }
            referenceLatitude = ClipLatitude(referenceLatitude);
            referenceLongitude = NormalizeLongitude(referenceLongitude);

            int digitsToRecover = SeparatorPosition - Code.IndexOf(SeparatorCharacter);
            // The precision (height and width) of the missing prefix in degrees.
            double prefixPrecision = Math.Pow(EncodingBase, 2 - (digitsToRecover / 2));

            // Use the reference location to generate the prefix.
            string recoveredPrefix =
                new OpenLocationCode(referenceLatitude, referenceLongitude).Code.Substring(0, digitsToRecover);
            // Combine the prefix with the short code and decode it.
            OpenLocationCode recovered = new OpenLocationCode(recoveredPrefix + Code);
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

            return new OpenLocationCode(recoveredLatitude, recoveredLongitude, recovered.Code.Length - 1);
        }

        /// <returns>Whether the bounding box specified by this Open Location Code contains provided point.</returns>
        /// <remarks>Convenient alternative to AreaData.Contains()</remarks>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        /// <exception cref="InvalidOperationException">If this code is not full.</exception>
        public bool Contains(double latitude, double longitude) {
            return Decode().Contains(longitude, latitude);
        }


        /// <returns>A new OpenLocationCode instance representing the parent code area</returns>
        /// <exception cref="InvalidOperationException">If this code top-level (length == 2)</exception>
        public OpenLocationCode Parent() {
            string code = TrimCode(Code);
            if (code.Length == 2) {
                throw new InvalidOperationException("Method Parent() cannot be called on top-level codes.");
            }

            int length = code.Length - (code.Length > SeparatorPosition ? 1 : 2);
            return new OpenLocationCode(code.ToCharArray(0, length));
        }


        public override bool Equals(object obj) {
            if (this == obj) {
                return true;
            }
            if (!(obj is OpenLocationCode)) {
                return false;
            }
            return Code == ((OpenLocationCode) obj).Code;
        }

        public override int GetHashCode() {
            return Code.GetHashCode();
        }

        public override string ToString() {
            return Code;
        }

        // Exposed static helper methods.

        /** Returns whether the provided string is a valid Open Location code. */
        public static bool IsValidCode(string code) {
            return code != null && IsValidCodeUpperCase(code.ToUpper());
        }

        private static bool IsValidCodeUpperCase(string code) {
            if (code.Length < 2) {
                return false;
            }

            // There must be exactly one separator.
            int separatorPosition = code.IndexOf(SeparatorCharacter);
            if (separatorPosition == -1) {
                return false;
            }
            if (separatorPosition != code.LastIndexOf(SeparatorCharacter)) {
                return false;
            }

            if (separatorPosition % 2 != 0) {
                return false;
            }

            // Check first two characters: only some values from the alphabet are permitted.
            if (separatorPosition == 8) {
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
            for (int i = 0; i < separatorPosition; i++) {
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
            if (code.Length > separatorPosition + 1) {
                if (paddingStarted) {
                    return false;
                }
                // Only one character after separator is forbidden.
                if (code.Length == separatorPosition + 2) {
                    return false;
                }
                for (int i = separatorPosition + 1; i < code.Length; i++) {
                    if (CodeAlphabet.IndexOf(code[i]) == -1) {
                        return false;
                    }
                }
            }

            return true;
        }


        /// <returns><c>true</c>, if the code is a valid short Open Location Code, <c>false</c> otherwise.</returns>
        /// <param name="code">The code to check</param>
        public static bool IsShortCode(string code) {
            try {
                return new OpenLocationCode(code).IsShort();
            } catch (ArgumentException) {
                return false;
            }
        }

        /// <summary>
        /// This will simply append padding '0' characters and append or insert the separator '+' character if necessary.
        /// </summary>
        /// <param name="code">The trimmed code to pad into a valid code</param>
        /// <returns>A padded code from a trimmed code (without any validation)</returns>
        public static string PadCode(string code) {
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
        /// Trim or strip unnecessary characters from a location code. simply by removing any padding '0' and separator '+' characters.
        /// </summary>
        /// <param name="code">the code to trim</param>
        /// <returns>A trimmed code from a padded code (without any validation)</returns>
        public static string TrimCode(string code) {
            StringBuilder codeBuilder = new StringBuilder();
            foreach (char c in code) {
                if (c != PaddingCharacter && c != SeparatorCharacter) {
                    codeBuilder.Append(c);
                }
            }
            return codeBuilder.ToString();
        }

        // Private static methods.

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

    }
}
