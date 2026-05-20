using System.ComponentModel.DataAnnotations;

namespace InvestPortfolio.Models
{
    public class PriceHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public double Price { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        // ===== Relations EF Core =====

        // FK vers Asset (1-to-N) : un historique de prix appartient à UN actif
        public int AssetId { get; set; }
        public Asset Asset { get; set; }
    }
}
