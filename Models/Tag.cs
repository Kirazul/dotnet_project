using System.ComponentModel.DataAnnotations;

namespace InvestPortfolio.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string Label { get; set; }

        // ===== Relations EF Core =====

        // N-to-N : Un tag peut être associé à plusieurs actifs
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
