using InvestPortfolio.Models;

namespace InvestPortfolio.Services
{
    public interface IPortfolioService
    {
        // ===== Assets CRUD =====
        Task<List<Asset>> GetAssetsAsync();
        Task<Asset> GetAssetByIdAsync(int id);
        Task AddAssetAsync(Asset asset);
        Task UpdateAssetAsync(Asset asset);
        Task DeleteAssetAsync(int id);

        // ===== Transactions CRUD =====
        Task<List<Transaction>> GetTransactionsAsync();
        Task<List<Transaction>> GetTransactionsByAssetAsync(int assetId);
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task AddTransactionAsync(Transaction transaction);
        Task DeleteTransactionAsync(int id);

        // ===== Budget =====
        Task<Budget> GetBudgetAsync();
        Task UpdateBudgetAsync(Budget budget);

        // ===== KPIs & Agrégations =====
        Task<int> GetTotalAssetsCountAsync();
        Task<double> GetPortfolioValueAsync();
        Task<double> GetTotalGainLossAsync();
        Task<double> GetBudgetBalanceAsync();

        // ===== Statistiques pour graphiques (LINQ GroupBy) =====
        Task<List<AssetAllocationStat>> GetAllocationByTypeAsync();
        Task<List<AssetPerformanceStat>> GetPerformanceByAssetAsync();
        Task<List<MonthlyTransactionStat>> GetMonthlyTransactionsAsync();

        // ===== Recherche & Filtrage (IQueryable) =====
        Task<List<Asset>> SearchAssetsAsync(string searchText, string assetType);
        Task<List<Transaction>> SearchTransactionsAsync(string assetName, string transactionType);

        // ===== Reload =====
        Task ReloadAssetAsync(Asset asset);

        // ===== Historique des prix =====
        Task<List<PriceHistory>> GetPriceHistoryAsync(int assetId);

        // ===== Simulation de variation de prix (marche aléatoire) =====
        Task SimulatePriceChangeAsync(int assetId);
        Task SimulateAllPricesAsync();
    }
}
