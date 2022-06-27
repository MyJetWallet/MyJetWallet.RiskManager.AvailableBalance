using System.Collections.Generic;
using System.Threading.Tasks;
using MyJetWallet.Domain;
using Service.ClientWallets.Domain.Models;

namespace MyJetWallet.RiskManager.AvailableBalance.AvailableBalance
{
    public interface IAvailableBalanceService
    {
        ValueTask<List<Models.AvailableBalance>>
            CalculateAvailableWalletBalances(string walletId, string clientId,
                string brokerId, string brandId, string baseAsset, string language,
                List<string> assetShowOnlyWithBalance);
    }
}