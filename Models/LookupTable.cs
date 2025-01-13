using System.Collections.Immutable;
using System.Globalization;
using CsvHelper;
using FellowOakDicom.Imaging;

namespace Models;

public static class LinqExtensions
{
    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts)
    {
        return list.Select((item, index) => new { index, item })
                   .GroupBy(x => x.index % parts)
                   .Select(x => x.Select(y => y.item));
    }
}

public record class LookupTable(string Name, ImmutableArray<RGB<byte>> Values)
{
    public static List<LookupTable> LoadLUTsFromCSV(string csvPath)
    {
        //new PaletteColorLUT().
        //throw new NotImplementedException();
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();

        var lutnames = csv.HeaderRecord!.Skip(1).SkipLast(1).ToArray();

        var keyvaluepairs = lutnames.Select(ln =>
            KeyValuePair.Create(ln, new List<byte>())
            );

        var dict = new Dictionary<string, List<byte>>(keyvaluepairs);

        while (csv.Read())
        {
            if (!csv.Parser.RawRecord.StartsWith(",,,,,,,,,,,0"))
                foreach (var lutname in lutnames)
                {
                    dict [lutname].Add(csv.GetField<byte>(lutname));
                }
        }

        return dict.AsEnumerable().Select(kv =>
        {
            var r = kv.Value.Slice(0, 256);
            var g = kv.Value.Slice(256, 256);
            var b = kv.Value.Slice(512, 256);
            var colors = r.Zip(g, b).Select(s => new RGB<byte>(s.First, s.Second, s.Third)).ToImmutableArray();

            return new LookupTable(kv.Key, colors);
        }).ToList();
    }
}