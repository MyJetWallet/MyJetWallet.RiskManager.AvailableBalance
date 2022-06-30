using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyJetWallet.Domain;
using MyJetWallet.RiskManager.AvailableBalance.Models;
using Service.ClientWallets.Domain.Models;

namespace MyJetWallet.RiskManager.AvailableBalance.AvailableBalance
{
    public interface IAvailableBalanceService
    {
        [ObsoleteAttribute("This method is obsolete. Call GetAndCalculateAvailableWalletBalances instead.", true)]
        ValueTask<List<Models.AvailableBalance>>
            CalculateAvailableWalletBalances(string walletId, string clientId,
                string brokerId, string brandId, string baseAsset, string language,
                List<string> assetShowOnlyWithBalance);

        ValueTask<List<Models.AvailableBalance>> GetAndCalculateAvailableWalletBalances(
            GetAvailableWalletBalanceRequest request, 
            List<string> assetShowOnlyWithBalance);

        ValueTask<List<Models.AvailableBalance>> CalculateAvailableWalletBalances(
            GetAvailableWalletBalanceRequest request, 
            List<Models.AvailableBalance> balances);
    }
}