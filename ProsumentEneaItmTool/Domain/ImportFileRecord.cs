using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProsumentEneaItmTool.Domain
{
    [Index(nameof(Date), Name = "IDX_DateRecord")]
    public class ImportFileRecord
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public double TakenVolumeBeforeBanancing { get; set; }
        public double FedVolumeBeforeBanancing { get; set; }
        public double TakenVolumeAfterBanancing { get; set; }
        public double FedVolumeAfterBanancing { get; set; }
    }
}
