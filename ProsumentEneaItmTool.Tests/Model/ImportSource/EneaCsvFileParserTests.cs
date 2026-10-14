using System.Text;
using ProsumentEneaItmTool.Model.ImportSource;

namespace ProsumentEneaItmTool.Tests.Model.ImportSource
{
    internal class EneaCsvFileParserTests
    {
        private const string Header = "Data;\"Wolumen energii elektrycznej pobranej z sieci przed bilansowaniem godzinowym\";\"Wolumen energii elektrycznej oddanej do sieci przed bilansowaniem godzinowym\";\"Wolumen energii elektrycznej pobranej z sieci po bilansowaniu godzinowym\";\"Wolumen energii elektrycznej oddanej do sieci po bilansowaniu godzinowym\"";

        [Test]
        public void Parse_StandardReport()
        {
            var inputData = """
                Data;"Wolumen energii elektrycznej pobranej z sieci przed bilansowaniem godzinowym";"Wolumen energii elektrycznej oddanej do sieci przed bilansowaniem godzinowym";"Wolumen energii elektrycznej pobranej z sieci po bilansowaniu godzinowym";"Wolumen energii elektrycznej oddanej do sieci po bilansowaniu godzinowym"
                "2024.01.01 00:00:00" ;"0,46";"0";"0,46";"0"
                "2024.01.01 01:00:00" ;"0,298";"0";"0,298";"0"
                "2024.01.01 12:00:00" ;"0,1";"1,25";"0";"1,15"
                """;

            var result = EneaCsvFileParser.Parse(inputData);

            Assert.Multiple(() =>
            {
                Assert.That(result.Warnings, Is.Empty);
                Assert.That(result.Records, Has.Count.EqualTo(3));
                Assert.That(result.Records[0].Date, Is.EqualTo(new DateTime(2024, 1, 1, 0, 0, 0)));
                Assert.That(result.Records[0].TakenVolumeBeforeBanancing, Is.EqualTo(0.46));
                Assert.That(result.Records[2].Date, Is.EqualTo(new DateTime(2024, 1, 1, 12, 0, 0)));
                Assert.That(result.Records[2].TakenVolumeBeforeBanancing, Is.EqualTo(0.1));
                Assert.That(result.Records[2].FedVolumeBeforeBanancing, Is.EqualTo(1.25));
                Assert.That(result.Records[2].TakenVolumeAfterBanancing, Is.EqualTo(0));
                Assert.That(result.Records[2].FedVolumeAfterBanancing, Is.EqualTo(1.15));
            });
        }

        [Test]
        public void Parse_ReportWithDashDatesAndEqualsSigns()
        {
            var inputData = Header + "\r\n" +
                "=\"2024-12-01 05:00\";=\"0,5\";=\"0\";=\"0,5\";=\"0\"\r\n" +
                "=\"2024-12-01 06:00\";=\"0,25\";=\"0,1\";=\"0,15\";=\"0\"\r\n";

            var result = EneaCsvFileParser.Parse(inputData);

            Assert.Multiple(() =>
            {
                Assert.That(result.Warnings, Is.Empty);
                Assert.That(result.Records.Select(x => x.Date), Is.EqualTo(new[] { new DateTime(2024, 12, 1, 5, 0, 0), new DateTime(2024, 12, 1, 6, 0, 0) }));
                Assert.That(result.Records[1].FedVolumeBeforeBanancing, Is.EqualTo(0.1));
            });
        }

        [Test]
        public void Parse_SkipsSeparatorAndIncompleteRowsSilently()
        {
            var inputData = Header + "\n" +
                "---;---;---;---;---\n" +
                "2024.01.01 00:00:00;0,1;0;0,1;0\n" +
                "2024.01.01 01:00:00;;;;\n" +
                "\n";

            var result = EneaCsvFileParser.Parse(inputData);

            Assert.Multiple(() =>
            {
                Assert.That(result.Records, Has.Count.EqualTo(1));
                Assert.That(result.Warnings, Is.Empty);
            });
        }

        [Test]
        public void Parse_ReportsInvalidRowsAsWarnings()
        {
            var inputData = Header + "\n" +
                "2024.01.01 00:00:00;0,1;0;0,1;0\n" +
                "nie data;0,1;0;0,1;0\n" +
                "2024.01.01 02:00:00;abc;0;0,1;0\n" +
                "2024.01.01 03:00:00;0,1;0\n";

            var result = EneaCsvFileParser.Parse(inputData);

            Assert.Multiple(() =>
            {
                Assert.That(result.Records, Has.Count.EqualTo(1));
                Assert.That(result.Warnings, Has.Count.EqualTo(3));
                Assert.That(result.Warnings[0], Does.StartWith("nie data"));
            });
        }

        [Test]
        public void Parse_EmptyFile()
        {
            var result = EneaCsvFileParser.Parse(string.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(result.Records, Is.Empty);
                Assert.That(result.Warnings, Is.Empty);
            });
        }

        [Test]
        public void Parse_KeepsBothRowsOfRepeatedHourWhenClocksGoBack()
        {
            var inputData = Header + "\n" +
                "2024.10.27 02:00:00;0,2;0;0,2;0\n" +
                "2024.10.27 02:00:00;0,3;0;0,3;0\n";

            var result = EneaCsvFileParser.Parse(inputData);

            Assert.That(result.Records.Select(x => x.TakenVolumeAfterBanancing), Is.EqualTo(new[] { 0.2, 0.3 }));
        }

        [TestCase("utf-8")]
        [TestCase("utf-8-bom")]
        [TestCase("utf-16")]
        [TestCase("utf-16-bom")]
        public void Decode_RecognizesEncoding(string encodingName)
        {
            var text = Header + "\r\n2024.01.01 00:00:00;0,1;0;0,1;0\r\n";
            var bytes = encodingName switch
            {
                "utf-8" => new UTF8Encoding(false).GetBytes(text),
                "utf-8-bom" => Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray(),
                "utf-16" => Encoding.Unicode.GetBytes(text),
                _ => Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray(),
            };

            var decoded = EneaCsvFileParser.Decode(bytes);

            Assert.Multiple(() =>
            {
                Assert.That(decoded, Is.EqualTo(text));
                Assert.That(EneaCsvFileParser.Parse(decoded).Records, Has.Count.EqualTo(1));
            });
        }
    }
}
