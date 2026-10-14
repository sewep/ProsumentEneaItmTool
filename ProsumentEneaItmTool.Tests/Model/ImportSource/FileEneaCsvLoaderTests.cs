using System.IO.Abstractions.TestingHelpers;
using System.Text;
using ProsumentEneaItmTool.Model.ImportSource;

namespace ProsumentEneaItmTool.Tests.Model.ImportSource
{
    internal class FileEneaCsvLoaderTests
    {
        private const string Header = "Data;Pobrana przed;Oddana przed;Pobrana po;Oddana po\n";

        private static MockFileData Utf8(string content) => new(Encoding.UTF8.GetBytes(content));

        [Test]
        public void Load_MergesFilesAndLaterFileReplacesOverlappingHours()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\dane\styczen.csv"] = Utf8(Header +
                    "2024.01.31 22:00:00;1;0;1;0\n" +
                    "2024.01.31 23:00:00;2;0;2;0\n"),
                [@"C:\dane\luty.csv"] = Utf8(Header +
                    "2024.01.31 23:00:00;20;0;20;0\n" +
                    "2024.02.01 00:00:00;3;0;3;0\n"),
            });

            var result = new FileEneaCsvLoader(fileSystem).Load([@"C:\dane\styczen.csv", @"C:\dane\luty.csv"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Warnings, Is.Empty);
                Assert.That(result.Records.Select(x => x.Date), Is.EqualTo(new[]
                {
                    new DateTime(2024, 1, 31, 22, 0, 0),
                    new DateTime(2024, 1, 31, 23, 0, 0),
                    new DateTime(2024, 2, 1, 0, 0, 0),
                }));
                Assert.That(result.Records[1].TakenVolumeAfterBanancing, Is.EqualTo(20));
            });
        }

        [Test]
        public void Load_KeepsRepeatedHourWithinOneFile()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\dane\pazdziernik.csv"] = Utf8(Header +
                    "2024.10.27 02:00:00;0,2;0;0,2;0\n" +
                    "2024.10.27 02:00:00;0,3;0;0,3;0\n"),
            });

            var result = new FileEneaCsvLoader(fileSystem).Load([@"C:\dane\pazdziernik.csv"]);

            Assert.That(result.Records, Has.Count.EqualTo(2));
        }

        [Test]
        public void Load_ReportsMissingFileAndFileWithoutDataWithFileNames()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\dane\dobry.csv"] = Utf8(Header + "2024.01.01 00:00:00;1;0;1;0\nzly wiersz;1;0;1;0\n"),
                [@"C:\dane\pusty.csv"] = Utf8(Header),
            });

            var result = new FileEneaCsvLoader(fileSystem).Load([@"C:\dane\dobry.csv", @"C:\dane\pusty.csv", @"C:\dane\brak.csv"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Records, Has.Count.EqualTo(1));
                Assert.That(result.Warnings, Has.Some.StartsWith("dobry.csv: zly wiersz"));
                Assert.That(result.Warnings, Has.Some.StartsWith("pusty.csv: nie znaleziono danych"));
                Assert.That(result.Warnings, Has.Some.StartsWith("brak.csv: nie udało się odczytać pliku"));
            });
        }
    }
}
