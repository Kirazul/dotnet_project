using System.ComponentModel.DataAnnotations;

namespace InvestPortfolio.Models
{
    public class Budget
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = "Le montant initial est requis.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le budget doit être supérieur à 0.")]
        public double InitialAmount { get; set; }

        public double CurrentBalance { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }
}
