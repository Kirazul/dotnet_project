using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestPortfolio.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le type de transaction est requis.")]
        public string Type { get; set; } = "Achat"; // Achat ou Vente

        [Range(0.0001, double.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0.")]
        public double Quantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Le prix unitaire doit être supérieur à 0.")]
        public double UnitPrice { get; set; }

        // Propriété calculée (non mappée en BDD)
        [NotMapped]
        public double TotalAmount => Quantity * UnitPrice;

        public DateTime Date { get; set; } = DateTime.Now;

        public string Notes { get; set; } = "";

        // ===== Relations EF Core =====

        // FK vers Asset (1-to-N) : chaque transaction concerne UN actif
        [Range(1, int.MaxValue, ErrorMessage = "Veuillez sélectionner un actif.")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }
    }
}
