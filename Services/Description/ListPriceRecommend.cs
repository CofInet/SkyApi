using Coflnet.Sky.Api.Models.Mod;
using Coflnet.Sky.Commands.MC;
using Coflnet.Sky.Commands.Shared;
using Coflnet.Sky.Sniper.Client.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Coflnet.Sky.Api.Services.Description;

public class ListPriceRecommend : CustomModifier
{
    public void Apply(DataContainer data)
    {
        string text = GetRecommendText(data.res[13], data.modService);
        data.mods[31].Insert(0, new DescModification(DescModification.ModType.INSERT, 1, text));
    }

    public static string GetRecommendText(PriceEstimate pricing, ModDescriptionService modService)
    {
        if (pricing == null || pricing.Median <= 4_000_000)
        {
            return $"No recommended instasell from Coflnet";
        }
        double target = SniperClient.InstaSellPrice(pricing);

        var formattedPrice = modService.FormatNumber(target);
        return $"{McColorCodes.GREEN}Instasell: {McColorCodes.DARK_GREEN}{formattedPrice} {McColorCodes.WHITE}based on Coflnet data";
    }
}