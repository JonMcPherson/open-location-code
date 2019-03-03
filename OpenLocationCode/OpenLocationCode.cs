using System;
using System.Text;

namespace Google.OpenLocationCode {
    public sealed class OpenLocationCode {

        // Provides a normal precision code, approximately 14x14 meters.
        public static readonly int CodePrecisionNormal = 10;

        // Provides an extra precision code, approximately 2x3 meters.
        public static readonly int CodePrecisionExtra = 11;

        // A separator used to break the code into two parts to aid memorability.
        private static readonly char Separator = '+';

        // The number of characters to place before the separator.
        private static readonly int SeparatorPosition = 8;

        // The character used to pad codes.
        private static readonly char PaddingCharacter = '0';

        // The character set used to encode the values.
        private static readonly string CodeAlphabet = "23456789CFGHJMPQRVWX";

        // The base to use to convert numbers to/from.
        private static readonly int EncodingBase = CodeAlphabet.Length;

        private static readonly int EncodingBaseSquared = EncodingBase * EncodingBase;

        // The maximum value for latitude in degrees.
        private static readonly int LatitudeMax = 90;

        // The maximum value for longitude in degrees.
        private static readonly int LongitudeMax = 180;

        // Maximum code length using just lat/lng pair encoding.
        private static readonly int PairCodeLength = 10;

        // Maximum code length for any plus code
        public static readonly int MaxCodeLength = 15;

        // Number of columns in the grid refinement method.
        private static readonly int GridColumns = 4;

        // Number of rows in the grid refinement method.
        private static readonly int GridRows = 5;


        /// <summary>
        /// Creates Open Location Code object for the provided code.
        /// </summary>
        /// <param name="code">A valid OLC code. Can be a full code or a shortened code.</param>
        /// <exception cref="ArgumentException">If the code is not valid.</exception>
        public OpenLocationCode(string code) {
            if (!IsValidCode(code)) {
                throw new ArgumentException($"The provided code '{code}' is not a valid Open Location Code.");
            }
            Code = code.ToUpper();
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
                throw new ArgumentException($"Illegal code length {codeLength}.");
            }
            // Ensure that latitude and longitude are valid.
            latitude = ClipLatitude(latitude);
            longitude = NormalizeLongitude(longitude);

            // Latitude 90 needs to be adjusted to be just less, so the returned code can also be decoded.
            if (latitude == LatitudeMax) {
                latitude = latitude - 0.9 * ComputeLatitudePrecision(codeLength);
            }

            // Adjust latitude and longitude to be in positive number ranges.
            decimal remainingLatitude = (decimal) latitude + LatitudeMax;
            decimal remainingLongitude = (decimal) longitude + LongitudeMax;

            // Count how many digits have been created.
            int generatedDigits = 0;
            // Store the code.
            StringBuilder codeBuilder = new StringBuilder();
            // The precisions are initially set to ENCODING_BASE^2 because they will be immediately divided.
            decimal latPrecision = EncodingBaseSquared;
            decimal lngPrecision = EncodingBaseSquared;
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
                    latPrecision /= GridRows;
                    lngPrecision /= GridColumns;
                    int row = (int) Math.Floor(remainingLatitude / latPrecision);
                    int col = (int) Math.Floor(remainingLongitude / lngPrecision);
                    remainingLatitude -= latPrecision * row;
                    remainingLongitude -= lngPrecision * col;
                    codeBuilder.Append(CodeAlphabet[row * GridColumns + col]);
                    generatedDigits += 1;
                }
                // If we are at the separator position, add the separator.
                if (generatedDigits == SeparatorPosition) {
                    codeBuilder.Append(Separator);
                }
            }
            // If the generated code is shorter than the separator position, pad the code and add the
            // separator.
            if (generatedDigits < SeparatorPosition) {
                for (; generatedDigits < SeparatorPosition; generatedDigits++) {
                    codeBuilder.Append(PaddingCharacter);
                }
                codeBuilder.Append(Separator);
            }
            Code = codeBuilder.ToString();
        }

        /// <summary>
        /// Creates Open Location Code with the default precision length.
        /// </summary>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        public OpenLocationCode(double latitude, double longitude) : this(latitude, longitude, CodePrecisionNormal) { }


        /// <summary>
        /// Returns the string representation of the code.
        /// </summary>
        /// <value>The current code for objects.</value>
        public string Code { get; }


        /// <summary>
        /// Encodes latitude/longitude into 10 digit Open Location Code.
        /// This method is equivalent to creating the OpenLocationCode object and getting the code from it.
        /// </summary>
        /// <returns>The encoded Open Location Code.</returns>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        public static string Encode(double latitude, double longitude) {
            return new OpenLocationCode(latitude, longitude).Code;
        }

        /// <summary>
        /// Encodes latitude/longitude into Open Location Code of the provided length.
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

        /// <summary>
        /// Decodes this Open Location Code into CodeArea object encapsulating latitude/longitude bounding box.
        /// </summary>
        /// <returns>The decoded CodeArea for this Open Location Code.</returns>
        /// <exception cref="InvalidOperationException">If this code is not full.</exception>
        public CodeArea Decode() {
            if (!IsFullCode(Code)) {
                throw new InvalidOperationException($"Method {nameof(Decode)}() may only be called on a full code, code was {Code}.");
            }
            // Strip padding and separator characters out of the code.
            string decoded = Code.Replace(Separator.ToString(), "").Replace(PaddingCharacter.ToString(), "");
            int decodedCodeLength = Math.Min(decoded.Length, MaxCodeLength);

            int digit = 0;
            // The precisions are initially set to ENCODING_BASE^2 because they will be immediately divided.
            decimal latPrecision = EncodingBaseSquared;
            decimal lngPrecision = EncodingBaseSquared;
            // Save the coordinates.
            decimal southLatitude = 0;
            decimal westLongitude = 0;

            // Decode the digits.
            while (digit < decodedCodeLength) {
                if (digit < PairCodeLength) {
                    // Decode a pair of digits, the first being latitude and the second being longitude.
                    latPrecision /= EncodingBase;
                    lngPrecision /= EncodingBase;
                    int digitVal = CodeAlphabet.IndexOf(decoded[digit]);
                    southLatitude += latPrecision * digitVal;
                    digitVal = CodeAlphabet.IndexOf(decoded[digit + 1]);
                    westLongitude += lngPrecision * digitVal;
                    digit += 2;
                } else {
                    // Use the 4x5 grid for digits after 10.
                    int digitVal = CodeAlphabet.IndexOf(decoded[digit]);
                    int row = digitVal / GridColumns;
                    int col = digitVal % GridColumns;
                    latPrecision /= GridRows;
                    lngPrecision /= GridColumns;
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
        /// Decodes the provided Open Location Code into CodeArea object encapsulating latitude/longitude bounding box.
        /// </summary>
        /// <returns>The decode.</returns>
        /// <param name="code">The Open Location Code to be decoded.</param>
        /// <exception cref="ArgumentException">If the code is not valid.</exception>
        /// <exception cref="InvalidOperationException">If the code is not full.</exception>
        public static CodeArea Decode(string code) {
            return new OpenLocationCode(code).Decode();
        }

        /// <returns><c>true</c>, if this Open Location Code is full, <c>false</c> otherwise.</returns>
        public bool IsFull() {
            return Code.IndexOf(Separator) == SeparatorPosition;
        }

        /// <returns><c>true</c>, if the provided Open Location Code is full, <c>false</c> otherwise.</returns>
        /// <param name="code">Code.</param>
        /// <exception cref="ArgumentException">If the code is not valid.</exception>
        public static bool IsFull(string code) {
            return new OpenLocationCode(code).IsFull();
        }

        /// <returns><c>true</c>, if this Open Location Code is short, <c>false</c> otherwise.</returns>
        public bool IsShort() {
            var separatorIndex = Code.IndexOf(Separator);
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
        private bool IsPadded() {
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
                throw new InvalidOperationException($"Method {nameof(Shorten)}() may only be called on a full code.");
            }
            if (IsPadded()) {
                throw new InvalidOperationException($"Method {nameof(Shorten)}() may not be called on a padded code.");
            }

            CodeArea codeArea = Decode();
            double range = Math.Max(
                Math.Abs(referenceLatitude - codeArea.CenterLatitude),
                Math.Abs(referenceLongitude - codeArea.CenterLongitude)
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

            int digitsToRecover = SeparatorPosition - Code.IndexOf(Separator);
            // The precision (height and width) of the missing prefix in degrees.
            double prefixPrecision = Math.Pow(EncodingBase, 2 - (digitsToRecover / 2));

            // Use the reference location to generate the prefix.
            string recoveredPrefix =
                new OpenLocationCode(referenceLatitude, referenceLongitude).Code.Substring(0, digitsToRecover);
            // Combine the prefix with the short code and decode it.
            OpenLocationCode recovered = new OpenLocationCode(recoveredPrefix + Code);
            CodeArea recoveredCodeArea = recovered.Decode();
            // Work out whether the new code area is too far from the reference location. If it is, we
            // move it. It can only be out by a single precision step.
            double recoveredLatitude = recoveredCodeArea.CenterLatitude;
            double recoveredLongitude = recoveredCodeArea.CenterLongitude;

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
            double longitudeDiff = recoveredCodeArea.CenterLongitude - referenceLongitude;
            if (longitudeDiff > prefixPrecision / 2) {
                recoveredLongitude -= prefixPrecision;
            } else if (longitudeDiff < -prefixPrecision / 2) {
                recoveredLongitude += prefixPrecision;
            }

            return new OpenLocationCode(recoveredLatitude, recoveredLongitude, recovered.Code.Length - 1);
        }

        /// <returns>Whether the bounding box specified by the Open Location Code contains provided point.</returns>
        /// <param name="latitude">The latitude in decimal degrees.</param>
        /// <param name="longitude">The longitude in decimal degrees.</param>
        public bool Contains(double latitude, double longitude) {
            CodeArea codeArea = Decode();
            return codeArea.SouthLatitude <= latitude
                && latitude < codeArea.NorthLatitude
                && codeArea.WestLongitude <= longitude
                && longitude < codeArea.EastLongitude;
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
            if (code == null || code.Length < 2) {
                return false;
            }
            code = code.ToUpper();

            // There must be exactly one separator.
            int separatorPosition = code.IndexOf(Separator);
            if (separatorPosition == -1) {
                return false;
            }
            if (separatorPosition != code.LastIndexOf(Separator)) {
                return false;
            }

            if (separatorPosition % 2 != 0) {
                return false;
            }

            // Check first two characters: only some values from the alphabet are permitted.
            if (separatorPosition == 8) {
                // First latitude character can only have first 9 values.
                if (CodeAlphabet.IndexOf(code[0]) > 8) {
                    return false;
                }

                // First longitude character can only have first 18 values.
                if (CodeAlphabet.IndexOf(code[1]) > 17) {
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


        /// <returns><c>true</c>, if the code is a valid full Open Location Code, <c>false</c> otherwise.</returns>
        /// <param name="code">The code to check.</param>
        public static bool IsFullCode(string code) {
            try {
                return new OpenLocationCode(code).IsFull();
            } catch (ArgumentException) {
                return false;
            }
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

        // Private static methods.

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
            return Math.Pow(EncodingBase, -3) / Math.Pow(GridRows, codeLength - PairCodeLength);
        }

    }
}
