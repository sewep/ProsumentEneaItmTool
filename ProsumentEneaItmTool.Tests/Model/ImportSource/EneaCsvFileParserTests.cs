using ProsumentEneaItmTool.Model.ImportSource;

namespace ProsumentEneaItmTool.Tests.Model.ImportSource
{
    internal class EneaCsvFileParserTests
    {
        [Test]
        public void ParsingCsv_Success()
        {
            var inputData = """
                Data;"Wolumen energii elektrycznej pobranej z sieci przed bilansowaniem godzinowym";"Wolumen energii elektrycznej oddanej do sieci przed bilansowaniem godzinowym";"Wolumen energii elektrycznej pobranej z sieci po bilansowaniu godzinowym";"Wolumen energii elektrycznej oddanej do sieci po bilansowaniu godzinowym"
                "2024.01.01 00:00:00" ;"0,46";"0";"0,46";"0"
                "2024.01.01 01:00:00" ;"0,298";"0";"0,298";"0"
                "2024.01.01 02:00:00" ;"0,697";"0";"0,697";"0"
                "2024.01.01 03:00:00" ;"1,16";"0";"1,16";"0"
                "2024.01.01 04:00:00" ;"0,184";"0";"0,184";"0"
                "2024.01.01 05:00:00" ;"0,205";"0";"0,205";"0"
                "2024.01.01 06:00:00" ;"0,179";"0";"0,179";"0"
                "2024.01.01 07:00:00" ;"0,327";"0";"0,327";"0"
                """;

            var result = EneaCsvFileParser.Parse(inputData);
            
        }
    }
}
