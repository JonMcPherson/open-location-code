using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;


public static class TestDataUtils
{

    public static ImmutableList<T> ReadTestData<T> ( string fileName )
    {
        var relativePath = Path.Combine("..", "..", "..", "TestData", fileName);
        using (var reader = new StreamReader(relativePath))
        {
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false, AllowComments = true }))
            {
                return csv.GetRecords<T>().ToImmutableList();
            }
        }
    }

}
