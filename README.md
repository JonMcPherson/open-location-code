# open-location-code

> The C# (.NET Standard) implementation of the Google Open Location Code API ([google/open-location-code](https://github.com/google/open-location-code)).

Convert locations to and from convenient codes known as Open Location Codes or [Plus Codes](https://plus.codes/)

Open Location Codes are short, ~10 character codes that can be used instead of street addresses. The codes can be generated and decoded offline, and use a reduced character set that minimises the chance of codes including words.

## Usage

> [API Reference](https://github.com/JonMcPherson/open-location-code/wiki)

```csharp
using Google.OpenLocationCode;
```

Create a code object:
```csharp
// From a valid full code
OpenLocationCode code = new OpenLocationCode("7JVW52GR+2V");

// From a valid short code
OpenLocationCode.ShortCode shortCode = new OpenLocationCode.ShortCode("52GR+2V");

// From coordinates encoded with default normal precision (~ 14x14 meters)
OpenLocationCode encodedCode = new OpenLocationCode(27.175063, 78.042188);
string encodedCodeStr = encodedCode.Code; // "7JVW52GR+2V"

// From coordinates encoded with extra precision (~ 2x3 meters)
OpenLocationCode encodedCode2 = new OpenLocationCode(27.175063, 78.042188, CodePrecisionExtra);
string encodedCodeStr2 = encodedCode2.Code; // "7JVW52GR+2VG"

// From coordinates encoded with low precision (~ 5560x5560 meters)
OpenLocationCode encodedCode3 = new OpenLocationCode(27.175063, 78.042188, codeLength: 6);
string encodedCodeStr3 = encodedCode3.Code; // "7JVW5200+"
encodedCode3.IsPadded(); // true
```

Decode the code area:
```csharp
CodeArea codeArea = code.Decode();

GeoPoint areaMin = codeArea.Min;
GeoPoint areaMax = codeArea.Max;
GoePoint areaCenter = codeArea.Center;
// Alternative properties
double areaMinLat = codeArea.SouthLatitude; // codeArea.Min.Latitude
double areaMaxLng = codeArea.EastLongitude; // codeArea.Max.Longitude
// Check point containment
bool areaContainsPoint = codeArea.Contains(areaCenter);
```

Shorten the code:
```csharp
OpenLocationCode.ShortCode shortCode = code.Shorten(27.1, 78.0);

string shortCodeStr = shortCode.Code; // "GR+2V"
```

Recover a short code:
```csharp
OpenLocationCode recoveredCode = shortCode.RecoverNearest(27.1, 78.0);

string recoveredCodeStr = recoveredCode.Code; // "7JVW52GR+2V"
```

Validate code strings:
```csharp
OpenLocationCode.IsValid("7JVW52GR+2V"); // true
OpenLocationCode.IsValid("GR+2V"); // true
OpenLocationCode.IsValid("12345678+"); // false

OpenLocationCode.IsFull("7JVW52GR+2V"); // true
OpenLocationCode.IsFull("GR+2V"); // false
OpenLocationCode.IsFull("12345678+"); // false

OpenLocationCode.IsShort("7JVW52GR+2V"); // false
OpenLocationCode.IsShort("GR+2V"); // true
OpenLocationCode.IsShort("12345678+"); // false
```

Alternatively use the static methods for all operations if a code object is not needed.
```csharp
string code = OpenLocationCode.Encode(27.175063, 78.042188);

CodeArea codeArea = OpenLocationCode.Decode("7JVW52GR+2V");

OpenLocationCode.ShortCode shortCode = OpenLocationCode.Shorten("7JVW52GR+2V", 27.1, 78.0);

OpenLocationCode recoveredCode = OpenLocationCode.ShortCode.RecoverNearest("52GR+2V", 27.1, 78.0);
```

## NuGet Package

The .NET Standard assembly of this OpenLocationCode library is published to the NuGet package repository.

https://www.nuget.org/packages/OpenLocationCode
