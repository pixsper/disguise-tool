using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace Pixsper.DisguiseTool;

class DisguiseCueTable
{
    private static readonly Regex HeaderLineExpression = new("^Cue table for (.+)$", RegexOptions.Compiled);

    private static readonly CsvConfiguration CsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        Delimiter = "\t"
    };

    public static DisguiseCueTable Read(string path)
    {
        using var reader = new StreamReader(path);

        var headerLine = reader.ReadLine();
        if (headerLine is null)
            throw new FormatException("Disguise cue table file is empty");

        var headerLineMatch = HeaderLineExpression.Match(headerLine);
        if (!headerLineMatch.Success)
            throw new FormatException("Disguise cue table incorrectly formatted, first line should contain 'Cue table for [Track Name]'");

        using var csv = new CsvReader(reader, CsvConfig);
        return new DisguiseCueTable(headerLineMatch.Groups[1].Value, csv.GetRecords<Entry>());
    }

    private DisguiseCueTable(string trackName, IEnumerable<Entry> entries)
    {
        TrackName = trackName;
        Entries = entries.ToList();
    }


    public string TrackName { get; }
    public IReadOnlyList<Entry> Entries { get; }

    public void Write(string path)
    {
        using var writer = new StreamWriter(path) { NewLine = "\r\n" };
        writer.WriteLine($"Cue table for {TrackName}");

        using var csvWriter = new CsvWriter(writer, CsvConfig);
        csvWriter.WriteRecords(Entries);
    }

    public record Entry
    {
        [Name("Beat")]
        public int Beat { get; init; }

        [Name("Tag")]
        public string? Tag { get; init; }

        [Name("Note")]
        public string? Note { get; init; }

        [Name("Track_Time")]
        public string? TrackTime { get; init; }

        [Name("TC_Time")]
        public string? TcTime { get; init; }
    }
}

