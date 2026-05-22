using System.ComponentModel.DataAnnotations;

namespace InvestPortfolio.Models
{
    public class Asset
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = "";

        [Required(ErrorMessage = "Le nom de l'actif est requis.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit faire entre 2 et 100 caractères.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Le symbole est requis.")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "Le symbole doit faire entre 1 et 10 caractères.")]
        public string Symbol { get; set; }

        [Required(ErrorMessage = "Le type d'actif est requis.")]
        public string AssetType { get; set; } = "Action"; // Action, Crypto, ETF

        [Range(0.01, double.MaxValue, ErrorMessage = "Le prix actuel doit être supérieur à 0.")]
        public double CurrentPrice { get; set; }

        public DateTime LastUpdate { get; set; } = DateTime.Now;

        // ===== Relations EF Core =====

        // 1-to-N : Un actif peut avoir plusieurs transactions
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        // 1-to-N : Un actif peut avoir plusieurs historiques de prix
        public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();

        // N-to-N : Un actif peut avoir plusieurs tags
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
