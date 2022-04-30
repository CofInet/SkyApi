using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coflnet.Sky.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Coflnet.Sky.PlayerName.Client.Api;
using Coflnet.Sky.Commands.Shared;
using Coflnet.Sky.Api.Models;
using System;
using Newtonsoft.Json;
using Coflnet.Sky.Sniper.Client.Api;

namespace Coflnet.Hypixel.Controller
{
    /// <summary>
    /// Special endpoints for mods.
    /// Returns information about mod related things. e.g. available socket commands for a help text
    /// </summary>
    [ApiController]
    [Route("api/mod")]
    [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any, NoStore = false)]
    public class ModController : ControllerBase
    {
        private HypixelContext db;
        private PricesService priceService;
        private PlayerNameApi playerNamService;
        private ISniperApi sniperApi;

        /// <summary>
        /// Creates a new instance of <see cref="ModController"/>
        /// </summary>
        /// <param name="db"></param>
        /// <param name="pricesService"></param>
        /// <param name="playerName"></param>
        /// <param name="sniperApi"></param>
        public ModController(HypixelContext db, PricesService pricesService, PlayerNameApi playerName = null, ISniperApi sniperApi = null)
        {
            this.db = db;
            priceService = pricesService;
            this.playerNamService = playerName;
            this.sniperApi = sniperApi;
        }

        /// <summary>
        /// Returns a list of available server-side commands
        /// </summary>
        [Route("commands")]
        [HttpGet]
        public IEnumerable<CommandListEntry> GetSumary()
        {
            return new List<CommandListEntry>()
            {
                new CommandListEntry("report {message}","Creates an error report with an optional message"),
                new CommandListEntry("online", "Tells you how many connections there are to the server"),
                new CommandListEntry("reset", "Resets the mod (deletes everything)"),
                new CommandListEntry("profit 7","Shows your profit from flips in the last week"),
                new CommandListEntry("logout","Logs out all connected mods"),
                new CommandListEntry("set","Sets some setting"),
                new CommandListEntry("chat {message}","Sends message in chat")
            };
        }

        /// <summary>
        /// Returns extra information for an item
        /// </summary>
        [Route("item/{uuid}")]
        [HttpGet]
        public async Task<string> ItemDescription(string uuid, int count = 1)
        {
            if (uuid.Length < 32 && uuid.Length != 12)
            {
                if (ItemDetails.Instance.GetItemIdForTag(uuid) == 0)
                    throw new CoflnetException("invalid_id", "the passed id does not map to an item");
                var median = await priceService.GetSumary(uuid, new Dictionary<string, string>());
                return $"Median sell for {count} is {FormatPrice(median.Med)}";
            }

            var lookupId = NBT.UidToLong(uuid.Length == 12 ? uuid : uuid.Substring(24));
            var key = NBT.GetLookupKey("uId");
            var auctions = await db.Auctions.Where(a => a.NBTLookup.Where(l => l.KeyId == key && l.Value == lookupId).Any()).Include(a => a.Bids).OrderByDescending(a => a.End).ToListAsync();
            var lastSell = auctions.Where(a => a.End < System.DateTime.Now).FirstOrDefault();
            long med = await GetMedian(lastSell);
            var playerName = await playerNamService.PlayerNameNameUuidGetAsync(lastSell.Bids.FirstOrDefault()?.Bidder);
            if (lastSell == null)
                return "Item has no recorded sell";
            return $"Sold {auctions.Count} times\n"
                + (lastSell == null ? "" : $"last sold for {FormatPrice(lastSell.HighestBidAmount)} to {playerName?.Trim('"')}")
                + (auctions.Count == 0 ? "" : $"Median {FormatPrice(med)}");
        }
        /// <summary>
        /// Returns new descriptions for an array of items
        /// </summary>
        /// <param name="inventory">Inventory data. Either the decoded array of items or the nbt encoded, ziped, base64 encoded fullInventoryNbt has to be passed</param>
        /// <param name="conId">Connection id of the calling mod to apply user settings</param>
        /// <param name="uuid">The uuid of the calling player</param>
        /// <returns>New description for each passed item. The existing one should be replaced with the new one</returns>
        [Route("description")]
        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<string[]>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IEnumerable<string[]>> ItemDescription(
            [FromBody] InventoryData inventory,
            [FromHeader] string conId,
            [FromHeader] string uuid)
        {
            //var name = await db.Players.Where(p => p.UuId == uuid).Select(p => p.Name).FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(inventory.FullInventoryNbt))
                inventory.FullInventoryNbt = "H4sIAAAAAAAAAM1aa2/jVnrmZHKZWzKZJLvZJJuUk80Um044w7tEF/uBulimR6RsiZIsFYVwSB5KlHjR6jK29K2f+qlbFGiLtJsFCizQ+QHth/br/JEWg/6C/oCi7zmkbMuWps5kgq1hwxLP/b097/Me3mKYm8y14BbDMF+9wbwReNf+4RrzVjGZx7Nrt5jrM9S/ybyJY3fAkJ/rzM29wMO7IepP4ev/3GLe8YLpOEQL6FVNJvgGPP2c+fD5s3xhgtEoiPvsQXKMJ6wGDfeeP8uVUIT6eId9/sx9mFOYT+GRGcSkX2OMsUca0EPhG5Xnz7ftJpPZPMZZq8gzHHzK/xl0ePFvfwef/jz7mn/x7bdrX7/7Z/IV1v4C+u7OccjaKB7R5a1kxhrxdIbCEHvMl9AOv0bswr6neMr6pLOLxsgNZgv2OJgNmA+gwxhNZmywGvYIZr5PTjUJwpAtx/0g3eSFyX9xcfLnz1TYOHv+5OS4KnQiK7GXVyFTNMf9CfIwaybePNy0zlfpOvp4HAawCoJ5ptPgKWbn2ciXHOPLbOCCTQ9zAD2m7CxhZ4Ngmj5jPoM+zoKFQVSz0IhgE2LaeBc+mdgdoDhwod99mPPjTOhkq6L0Dc/zoI3H0giaWLrVvE0mD2Y4AlHHrIPZCfaTSR9795kHtIf84ne/Jwet41/PgwkVnbKHye4TH7aGia0pJjFXFMSsHYCp5R4xP6GyDKvlStkq6fUOW6ob1eoN5k0LRZj5BFrtYAYbnUfZaUt17khVFOYWc7d8MpsgfTabBM58hqc3iFMw79uGrVtG0+zRmXoyzDWfQ8NXriu7gutLnIc9jZNVReK0nONxOV9yHdkXFE8VbzA3Z0GEQdrRmLkrPhZzj0WR1XZkkT0wGeYN5u3ULRjymfjgX13wwct+d6sxmodh7TjGE9ihARtRFN/NOS7POa6c5yRXVjjNx4hzlbygabLmK7IL4w4myRhPZmAfN5kbM3wym4NUiftfu8G81ULhHF/7L3yc9I3iPo/aQuhK9YFzpAdGKembdoev2Ycn1vDwuGbrJ9bi+"
                                            + "IlR1AN3b/9pNwqn3WY4MgJdNYrGotM+PO6WwsisNE9M2+RrJfirtCKrdMjX2vXIsuthx94dWUVjWgz0vhEXFo7YHTuVVq0D66bz7FfQ0X54OCwoRpCtJYaxE+3yHjxvRq0Trx0uuu1Duj9vb1/oNtJ+XqUle3utRffIpG3pmWCd0Ko2mvL6M9L/aH+arZl4e/XjWpB/em6OudMO5922Befq8tVICb2ixnePBnQf1WUrhHMtaqXCyCrtjzpDXYK/EzNqBdayO+q2m5K5NODcpmxF3aG1LC+69iDsDMt8NyovOkOQSelQgud8J9oPanYrAnkFVf1sf067xXfa9YFXKV/cO9HVzBHroVM0+rWAyOhk7DSMJ6t2/zA5+7/HP/EPf/Ur0PjG6H2PeQ/8Mg3ajVkSE6P8JbhiMYkciG4kAmsnxHup/4H3TkknFs2oN6ZOq9oDNI8QmFY/mM5IZwgVYHURinE8Cxc02MzjMHFHZBSZsxAs0WRCPubGZO1HWczWyXBVVHjWRP3ARWGKJ9CyQwKZy4nSi9/9Bex0guM+BDcIOc5DWci9+O43EBhnOAyDPsAXJiFBI52//Se2OAlm7KnH/ZRE1jj49RxnUFVI4vl0h8zkPlT+89//htWdICQgcDrkaxhyLiAhIhsQQGNEQkkVPwXkODpqZUHws4ux6KDWLtfZhl2zyquIROJV2XVBOJPAhdAbxDMIsFuj0QflYrEMDmQUewe6YdmGVTmNR7yvYdXj8xyv5HhOlnnE5T1V5RRf8HA+5+axkF+PR/JjiSfxSNoRc6y+Fo+uZ/HovZfkBNeY283YIWiPnBBfu1KKcGtTiiBkytiQD0DLH6XY9vxZYoPFPMWTBesHE7CuceCO0AnOQDaJEg+H90H3T0Ga2GMJXsEAH0XJfEoEnU6RRGjKHsDQaRI/uhIewVDAtbBxUC4aepU9MIpP9KNTDRJbPAjAE+AgB9mOtqnvll492NN7ZIZTtamilvdd1+"
                                            + "W8HAa1eTmVy/Oix2lKHmkYyRpylG1qk8QLalvByJ9eUNurqOptKjOA3HA2SLUh8gTJ6eMS9nE8zfIyhT7+lCY6Z46XjaFtVxHzz9bFXNwrN+yDqm6fShoWcIsDkAJsdIazNAACAJpMt0r8bsPW6721uVKxC46q+lj1OVfyPE4WJABNR8tzouIi181JvMc7W8UubRH7q6C3IMqe5uZlzoX0lpNyoswh2BgHz5Erur6oInQl9Gb+FS/2T5HJXRgqfLcbfFgzhmPQTGsBKKEaEbTv6Wp1oZ3rq8xQWwk70v6gGx/OnajFV6V6iPfqghs1n9ZKLm9VTNm0dQWQ7KTTbgXdoXHcHXYkS+wOAIUXVsUadMSmYA4LA3M44jtih7eifUBAQ+zYhmDBd3PZHQAaLgn614r7mn/EP9mGRj9j3gdzqafWcYpHJOUupoYzn4KPQ+aH2FUnPX4ahLRLMqEJL7WP0jzu4yRmCyECn49IHvxZikuIJr0pDGl1HCegFY9Efw3aM7ukqe4kSiZZwn/aj9gzoKNaxWDsHpos4PvXO2TTay6TT7nPeW/Jk4Q5txbkPiQPMiBLGZLA0xz6sj9lDRTJipByu3g14MFZw3muRRo+hwYKbiz4B4jhLMS6D+UHKUE7OxntSQH9jLdECQRccARgBhBX6UZIhxUNOo+Jqyh+honMLXh8hovg6Ao4ehnCIFsv79bqlfI6KEIeopQmqA9a20sm8Vbnvl2q65Wa1dur1a1Tx9Ykh+cdR+D8vIDBsX2NQ7wmcFgUeUnOafCAv7Jjr2Dwo9cMg/coF1tXE898tMEMeObuKeP97jcZpyXqqiYJSRUg3zgyDIP5+"
                                            + "Zq6iFG71DpImEQ0oTITYH0AUh5EkjEZCX5EI7GzIBKH6Axgo6y0KoJtZ+nPTko04xlrT1AMLgTUElTz/BkGLdaNyp7NFqsAaRSIbRziMZB2Ygl51iGZ3hQes2iAkQebYf4Yvi2SOazusX1C3SiC8PDPB9KeWiZ0ocYMLkiaJXaK3ST2pnRvJIEwUYzYItgkEZOkXA1g3qeeHdb1Othbu1YvrewN5K7p0zF2V9wSCL230epuJpMAqL6N+sxNwy6bPbNsNVNjvKcT5LJ7td2evVfuQcp3i7lDjAQEF0FGMr3OvBOmOiNOc525Da6NIQvC02BKzejUhH1R0xxI2TjByUucnMcKl/d9n/PUvIYAIETNcy4ySyFPTDi/w6ubmSUjrEz49jZs2mCnbxCwJ+naMTEYI+5PsBfAYZg7oC6QZbFmmjVrJcfb8LDmTAMvQNud9kat0DBKhm5tAtDK/wmgGzZ5bfNuwKb9KnqK2MLcHeHZ9ihS1Vt6r9AsPinbm/b016+yp7eoQ7YCfMwiCINgVmDzwKtGiwJxCeoq40kC8pxOvwEYc8O5R0Sc9YLQOf2GkqViEoZgmOBw0K+O3WCM4QNxHgAlfD8NBrgYBoRRJSwkCfH9lQQgMqPViqyJ4zkFrXrQHwB2kBFfbxXKu40nnUK1VnxCLXyTWP5+"
                                            + "JZY3friqwDN9C4PjTdjDOZrMllv39fZhU6/b3U0bGq82dOf12A7EAJLSYzQJt9sOuHm53jso6/Xqa0sIFdGH3M/nJEFROckXXC7v+Dkuj/K8gBzXd2TnauWc37+snGPaxtIadkSr1D82G9vKOfuRZQ8gidsfWXYIn3XeHBqLWqUjmiVjYQ7LJ91SfWjah8edxUvLOYITd0Mnro+9iJQzTuDzYVbm2F+itjfvHJFks9XoHu0KpPTTpSWNAu/GrTDrdz65JW05uh7su8nP9u1g7RnpD/Md0/O+StJr2WXerJi8GXWHplg+6UTlRTdqQsJr8lbbUKxlK+rYfalrD0hJa2C2jZNayVx2IlOyoqZoLk2lMwTZDDsLcwlyK4GsillpZo+H/wWtGPPb8t93aADZBduhWS5kr0sSH1pJCLiWrArDhQktxxLMA+cn4dfrY4KZuaws8wVNdccrXNswDSyTB/v/E5JlNJJ56NAls4eElp9ipl2vHex12F2jsbfyE8iAtXSuwE2TdD+YDp4/I1sI6RzVQr1mdctbPeiTVq1a1C2j2KMpIJm8lw3ZkIe9ij+54Eai4kkcEnIyJ3mSzzmaqnI53seSJAPj4rWr+dO3Ly2Plg4lc+kKNXskW8FWfxqBvwlWe3/QaYOtDMG+hiOh096POsNC2B3uBp22segsC0CQXupPTUcUxuAzg//X5VGxG4FMFIgTi+7wcGGWOseddvPYsjvLztI9toiP2F7YWR6SsjFfqzQlswKksmScmBWIM3ZhZA7NY0ss89ayD37XGpqRGaQ+Dv4TkNLmWNvmRB/9iCTy5xtIJLmtCvwAWORnl1gkYUhjkgiuOBN1j0RPK1pTcA1YgvUDcL6ElC1/kbb7WQSArAqnaT2auGg6TfP6B2mnygBNZ49YAygaxtGUDYMR7bxgcTxMFmQ50g2x0xjRyitEBwgZyTxLeCEB/SLtcpxMQo/1J0nEkvSSXgPB/+ye6OyAW+jvBxfJrqhcjSHepgzROEcRT8PORooIFFoz8QyDkhoDNNmcrZP4cscs2+VavdfY02nCn2bYCDuyiniZEx1Z4WTkIg6pmscpbl4WVVWVZWFL9UfYkfktJLH7mknih5dJoihQntPAwH5gMhKjLlQBgO2TasIuniQu5W4pvYLpPiEsJ+6HgA92rdbbA+"
                                            + "LWs5pmoVynJdYVeVzxRtC8i2buAEwK0YqJmy24qgCQtTKymF1zVoKnVMHaNCOxhPhhat+wA5LNOiiYpU73kPTz6NlIrzEpA/O0MkIQBJaeQ5aa1WfdtHaxosbsLwWFXNB+TQ6X48nusx6prKD9fAdSQ/5q/YYXViAGOCVcMz2NllMeXIlEfnGxqE8Qy7AqbLusH6xRIbUxALYEOeT3ZZJ3CH9s7NXLJcgtL5HItxHVIWzWO2OM2HNUNYd9TvNEHuw5z3MIazyXcxxPcnns5HLaJcaoEXtWd4T8lmrmb18lw/9+jPFdeFhHx2wdOU6wnaK9XdcLBeMCO1tt8y9fI2e8CQ+LaDJJXkIXi3q9XrN7RGmvK+Xnsc/nfBlSflVCnJSXSc3Ky3N8zkWeIHs5x/WuVgP+lx+rBtwhqS+kvJCijDptgOPIkDrL/aDb3g0InAOdULolSI2Hh3xn2RpZ7Y5i2n2xEwGUi6Zg2t2BRaC/DSmAeMh37UPBbNAa8B8CvR9sLAED+qbQlvk7LefSlx2yu6YsIJ3vehUUFFQY9/65cWmJlZRlK6clsAfs2RUYPhk/olGVNpN3LRD/SKA9zsIzKeTRTiCNLEaQcEbZPqmcJRNaCUTU92jNLYLFf5ySLRxOWR1Oj5wtQY9WFmC8YZVLPZ1Az2kEy+"
                                            + "c8RZN5kZOQL3Cy6jtcHnuQpcuS4GmaqyB3e9n24jXYCpE/e82IfIcIYg2RtW80jf5B63ugYJKMjGMM+VmVAt65CjoOMWZTxGM9ckVACq/vpoVXVVmHUu/LWkwNM5niMzum9+OgedLJo3CMU+6XZm0Eq44DDJ9oRgca9JwQefjRl1dCtg/X79/WKqTk6q0Bq3hrt26sRu72vie6pTdydPYeGX8J4G5OVzIkZegzjFNcrGLX1TjRk0VOFn1AO0ETubziy66YyyFHWb8ofR8AToBfVhB3FOESyP2AeK0JruIKDuYEjdRp+HyOy7t5iVMF2KGPeV7M8X/gO7vucLQ029ag1m7y5M2T7pDEYFOwKt0BKd0ATV10ozJQK2to2WHQtZvH3agb1EqFsNauj2qV3cgqjYDSmlKtYkW1Ujiy0ni99Q2SN6n1ZvcMU/JmCEQhB4fJcfZuBFg6EALyj77vBsEmTtgwiUnNAgyqj2ekAErc4KNzBEAvFsuNRq3eWVkjYQdGDIoOPEQqo6xOWAlyN4P2S2zxJ+QVCtMo6bZRs3p63TZ29aJ9Vod3fBESOIfzJE3jgBK4nOOpiBN4Jy8KvOQpeP2Nivdyj2VicBte8MpCEvPxD33J8i3qyvp8lkRweBeFECc8iC0QC8gVBrAzlzIA4uRA8WIKXXOAEyCsmNZ+OXdVLXaT2A/687SO/MnFzHaT3NUD0GwSo5AtkTVBv7ltMeCDcda15ybRGLTT49fqpS/XzEG53qhZerVXKlfLNhA4ss6523uUl5AEhE0mt/eyIHNIQjzwNwFLno80T5WvMR9fmgS0a7TK19aBRHks0TCh7EjypcuT1U8aLB6+hvcq7m2+7v3ppltd8SWXuh9cuGoWgGN9eCnz0PiL183Slptl8MzVVWP+xT/+bXbVCKmCugvhj22Ac6YvSLGNOag2AN0XwiTxMqefzk5vOdJ7YgTcNMjgL+2U8kYKdfQGg1wxnr92I1mZml78pneG2etXBC1pnqJQxkkPRG4/MjS92ls8n14077OXQq4zb7lJmEyY//6Peytb/5wQuNU5s8vos5dPtmY3nzaaYHQGmFp2N73h1RNVpS+I8pyGNJ/cUEtc3pd8DjIcVxWEnKaI29/4ufzqCf35X8k8oLnOLQAA";
            var nbt = NBT.File(Convert.FromBase64String(inventory.FullInventoryNbt));
            var auctionRepresent = nbt.RootTag.Get<fNbt.NbtList>("i").Select(t =>
            {
                var compound = t as fNbt.NbtCompound;

                if (compound.Count == 0)
                    return (null, new string[0]);
                var auction = new SaveAuction();
                auction.Context = new Dictionary<string, string>();
                NBT.FillFromTag(auction, compound, true);
                var desc = NBT.GetLore(compound);
                //Console.WriteLine(JsonConvert.SerializeObject(auction));
                //Console.WriteLine(JsonConvert.SerializeObject(auction.Context));
                return (auction, desc);
            }).ToList();

            var res = await sniperApi.ApiSniperPricePostAsync(auctionRepresent.Select(el =>
            {
                var a = el.auction;
                if (a == null)
                    return null;
                return new Sky.Sniper.Client.Model.SaveAuction()
                {
                    Count = a.Count,
                    Enchantments = a.Enchantments.Select(e => new Sky.Sniper.Client.Model.Enchantment(0, (Sky.Sniper.Client.Model.EnchantmentType?)e.Type, e.Level)).ToList(),
                    FlatenedNBT = a.FlatenedNBT,
                    Tag = a.Tag
                };
            }).ToList());

            var result = new List<string[]>();
            for (int i = 0; i < auctionRepresent.Count; i++)
            {
                var desc = auctionRepresent[i].desc;
                var price = res[i];
                if (desc == null || price == null)
                    result.Add(null);
                else if(price.Median == 0)
                    result.Add(desc.Append("no price data").ToArray());
                else
                    result.Add(desc.Append($"lbin: {FormatNumber(price.Lbin.Price)}").Append($"Med: {FormatNumber(price.Median)} Vol: {price.Volume}").ToArray());
            }
            return result;
        }

        private string FormatNumber(long price)
        {
            return string.Format("{0:n0}", price);
        }

        /// <summary>
        /// Representation of an inventory
        /// </summary>
        public class InventoryData
        {
            public string ChestName;
            public string FullInventoryNbt;
        }

        public class ItemNbtData
        {
            public byte Count;
            public int Id;
            public ItemTag Tag;
        }

        public class ItemTag
        {
            public Display Display;
            public Dictionary<string, string> ExtraAttributes;
        }

        public class Display
        {
            public string[] Lore;
            public string Name;
        }

        private static async Task<long> GetMedian(SaveAuction lastSell)
        {
            var references = await FlipperService.Instance.GetReferences(lastSell.Uuid);
            var med = references.OrderByDescending(r => r.HighestBidAmount).Skip(references.Count / 2).FirstOrDefault()?.HighestBidAmount ?? 0;
            return med;
        }

        private static string FormatPrice(long price)
        {
            return string.Format("{0:n0}", price);
        }
    }
}

