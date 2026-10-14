using ProsumentEneaItmTool.Model.NetBilling;

namespace ProsumentEneaItmTool.Tests.Model.NetBilling
{
    internal class PseRceClientTests
    {
        // Shape of a real response of https://api.raporty.pse.pl/api/rce-pln with $select.
        private const string PageJson = """
            {
              "value": [
                { "business_date": "2026-07-01", "period": "00:00 - 00:15", "rce_pln": 600.0 },
                { "business_date": "2026-07-01", "period": "00:15 - 00:30", "rce_pln": 610.0 },
                { "business_date": "2026-07-01", "period": "00:30 - 00:45", "rce_pln": 620.0 },
                { "business_date": "2026-07-01", "period": "00:45 - 01:00", "rce_pln": 630.0 },
                { "business_date": "2026-07-01", "period": "23:45 - 24:00", "rce_pln": -12.5 }
              ],
              "nextLink": "https://api.raporty.pse.pl/api/rce-pln?$after=abc"
            }
            """;

        [Test]
        public void ParsePage_ReadsQuartersAndNextLink()
        {
            var page = PseRceClient.ParsePage(PageJson);

            Assert.Multiple(() =>
            {
                Assert.That(page.Quarters, Has.Count.EqualTo(5));
                Assert.That(page.Quarters[0].BusinessDate, Is.EqualTo(new DateTime(2026, 7, 1)));
                Assert.That(page.Quarters[4].PricePlnMwh, Is.EqualTo(-12.5));
                Assert.That(page.NextLink, Is.EqualTo("https://api.raporty.pse.pl/api/rce-pln?$after=abc"));
            });
        }

        [Test]
        [Explicit("Uses the real PSE API, run manually.")]
        public async Task GetHourlyPrices_RealApi_ReturnsWholeDays()
        {
            using var httpClient = new HttpClient();
            var client = new PseRceClient(httpClient);

            // Two days need two pages (192 quarters), one of them before the switch to quarter-hour prices.
            var prices = await client.GetHourlyPricesAsync(new DateTime(2025, 9, 30), new DateTime(2025, 10, 1), null, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(prices, Has.Count.EqualTo(48));
                Assert.That(prices[0].Hour, Is.EqualTo(new DateTime(2025, 9, 30, 0, 0, 0)));
                Assert.That(prices[^1].Hour, Is.EqualTo(new DateTime(2025, 10, 1, 23, 0, 0)));
            });
        }

        [Test]
        public void ParsePage_LastPageHasNoNextLink()
        {
            var page = PseRceClient.ParsePage("""{ "value": [] }""");

            Assert.Multiple(() =>
            {
                Assert.That(page.Quarters, Is.Empty);
                Assert.That(page.NextLink, Is.Null);
            });
        }

        [Test]
        public void AggregateToHours_AveragesQuartersOfLocalHour()
        {
            var hours = PseRceClient.AggregateToHours(PseRceClient.ParsePage(PageJson).Quarters);

            Assert.Multiple(() =>
            {
                Assert.That(hours.Select(x => x.Hour), Is.EqualTo(new[] { new DateTime(2026, 7, 1, 0, 0, 0), new DateTime(2026, 7, 1, 23, 0, 0) }));
                Assert.That(hours[0].PricePlnMwh, Is.EqualTo(615.0));
                // A negative price is kept here, the valuation treats it as zero.
                Assert.That(hours[1].PricePlnMwh, Is.EqualTo(-12.5));
            });
        }

        [Test]
        public void AggregateToHours_RepeatedHourWhenClocksGoBackGivesOneAverage()
        {
            var day = new DateTime(2025, 10, 26);
            var quarters = new[]
            {
                new PseRceClient.QuarterPrice(day, "02:00 - 02:15", 100),
                new PseRceClient.QuarterPrice(day, "02:00 - 02:15", 300),
            };

            var hours = PseRceClient.AggregateToHours(quarters);

            Assert.That(hours.Single().PricePlnMwh, Is.EqualTo(200));
        }
    }
}
