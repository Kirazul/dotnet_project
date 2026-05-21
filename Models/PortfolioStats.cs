namespace InvestPortfolio.Models
{
    // Classe pour transporter les statistiques agrégées (comme LocationStat dans le ref)
    public class AssetAllocationStat
    {
        public string AssetType { get; set; }
        public double TotalValue { get; set; }
        public int Count { get; set; }
    }

    public class AssetPerformanceStat
    {
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public double GainLoss { get; set; }
        public double GainLossPercent { get; set; }
    }

    public class MonthlyTransactionStat
    {
        public string Month { get; set; }
        public double TotalBuy { get; set; }
        public double TotalSell { get; set; }
    }
}
