namespace MyJetWallet.RiskManager.AvailableBalance.Models;

public class GetAvailableWalletBalanceRequest
{
    public string WalletId { get; set; }
    public string ClientId { get; set; }
    public string BrokerId { get; set; } 
    public string BrandId { get; set; } 
    public string BaseAsset { get; set; }
    public string Language { get; set; }
}