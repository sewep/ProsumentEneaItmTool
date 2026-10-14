using System.ComponentModel.DataAnnotations;

namespace ProsumentEneaItmTool.Domain
{
    /// <summary>
    /// Hourly market price of energy (RCE) published by PSE.
    /// </summary>
    public class RcePrice
    {
        /// <summary>
        /// Local start of the hour.
        /// </summary>
        [Key]
        public DateTime Hour { get; set; }

        /// <summary>
        /// Average of quarter-hour prices in the hour, PLN/MWh. Can be negative.
        /// </summary>
        public double PricePlnMwh { get; set; }
    }

    /// <summary>
    /// Monthly market price of energy (RCEm), entered by the user from PSE publications.
    /// </summary>
    public class RcemPrice
    {
        /// <summary>
        /// First day of the month.
        /// </summary>
        [Key]
        public DateTime Month { get; set; }

        /// <summary>
        /// PLN/MWh.
        /// </summary>
        public double PricePlnMwh { get; set; }
    }
}
