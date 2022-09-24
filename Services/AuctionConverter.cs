using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Coflnet.Sky.Core;
using Newtonsoft.Json;

namespace Coflnet.Sky.Api.Services
{
    public class AuctionConverter
    {
        List<Element> Order = new(){
            new(){Label="stars", Type="numeric0"},
            new(){Label="wood_singularity_count", Type="numeric0"},
            new(){Label="tuned_transmission", Type="numeric0"},
            new(){Label="stars", Type="numeric"},
                new(){Label="reforge", Type="reforge"},//reforge: soldItem.reforge == 'None' ? '' : soldItem.reforge,
            new(){Label="rarity_upgrades", Type="numeric0"},
            new(){Label="power_ability_scroll", Type="string"},
            new(){Label="hot_potato_count", Type="hpc"},
                new(){Label="gems", Type="gems"},
            new(){Label="ethermerge", Type="numeric"},
            new(){Label="drill_fuel", Type="numeric"},
            new(){Label="drill_part_upgrade_module", Type="string"},
            new(){Label="drill_part_fuel_tank", Type="string"},
            new(){Label="drill_part_engine", Type="string"},
            new(){Label="stored_drill_fuel", Type="numeric"},
            new(){Label="art_of_war_count", Type="numeric0"},
                new(){Label="ability_scroll", Type="ability_scroll"},
                new(){Label="count", Type="count"},
                new(){Label="enchantments", Type="enchantments"},
            new(){Label="winning_bid", Type="numeric0"},
            new(){Label="pet_exp", Type="numeric0"},
            new(){Label="pet_candy_used", Type="numeric"},
            new(){Label="pet_type", Type="string"},
            new(){Label="pet_tier", Type="string"},
            new(){Label="pet_held_item", Type="string"},
            new(){Label="pet_skin", Type="string"},
            new(){Label="potion_type", Type="string"},
            new(){Label="potion_level", Type="numeric"},
            new(){Label="potion_enchanced", Type="numeric"},
            new(){Label="potion_extended", Type="numeric"},
            new(){Label="potion_splash", Type="numeric"},
            new(){Label="potion_name", Type="string"},
            new(){Label="dungeon_potion", Type="numeric"},
            new(){Label="upgrade_level", Type="numeric"},
            new(){Label="party_hat_year", Type="numeric"},
            new(){Label="party_hat_color", Type="string"},
            new(){Label="talisman_enrichment", Type="string"},
            new(){Label="dungeon_paper_id", Type="string"},
            new(){Label="runes", Type="runes"}, // needs preprocessing
            new(){Label="stats_book", Type="string"},
            new(){Label="base_stat_boost_percentage", Type="numeric"},
            new(){Label="item_tier", Type="numeric"},
            new(){Label="item_durability", Type="numeric"},
            new(){Label="traps_defused", Type="numeric"},
            new(){Label="training_weights_held_time", Type="numeric"},
            new(){Label="maxed_stats", Type="numeric"},
            new(){Label="skin", Type="string"},
            new(){Label="basket_edition", Type="numeric"},
            new(){Label="basket_player_name", Type="string"},
            new(){Label="radius", Type="numeric"},
            new(){Label="year_obtained", Type="numeric"},
            new(){Label="cake_owner", Type="string"},
            new(){Label="soul_durability", Type="numeric"},
            new(){Label="ultimate_soul_eater_data", Type="numeric"},
            new(){Label="expertise_kills", Type="numeric"},
            new(){Label="is_shiny", Type="numeric"},
            new(){Label="date", Type="numeric"},
            new(){Label="edition", Type="numeric"},
            new(){Label="sender_name", Type="string"},
            new(){Label="recipient_name", Type="string"},
            new(){Label="farmed_cultivating", Type="numeric"},
            new(){Label="farming_for_dummies_count", Type="numeric"},
            new(){Label="ranchers_speed", Type="numeric"},
            new(){Label="skeletor_kills", Type="numeric"},
            new(){Label="zombies_killed", Type="numeric"},
            new(){Label="necromancer_souls", Type="necromancer_souls"},// needs preprocessing
            new(){Label="sword_kills", Type="numeric"},
            new(){Label="spider_kills", Type="numeric"},
            new(){Label="health", Type="numeric"},
            new(){Label="rep_radius", Type="numeric"},
            new(){Label="blocks_broken", Type="numeric"},
            new(){Label="gilded_gifted_coins", Type="numeric"},
            new(){Label="compact_blocks", Type="numeric"},
            new(){Label="zombie_kills", Type="numeric"},
            new(){Label="last_potion_ingredient", Type="string"},
            new(){Label="should_give_alchemy_exp", Type="numeric"},
            new(){Label="boss_tier", Type="numeric"},
            new(){Label="raider_kills", Type="numeric"},
            new(){Label="bow_kills", Type="numeric"},
            new(){Label="ammo", Type="numeric"},
            new(){Label="bottle_of_jyrre_seconds", Type="numeric"},
            new(){Label="new_years_cake", Type="numeric"},
            new(){Label="magma_cubes_killed", Type="numeric"},
            new(){Label="fishes_caught", Type="numeric"},
            new(){Label="leader_votes", Type="numeric"},
            new(){Label="leader_position", Type="numeric"},
            new(){Label="blood_god_kills", Type="numeric"},
            new(){Label="tuning_fork_tuning", Type="numeric"},
            new(){Label="eman_kills", Type="numeric"},
            new(){Label="wishing_compass_uses", Type="numeric"},
            new(){Label="blocks_walked", Type="numeric"},
            new(){Label="yogs_killed", Type="numeric"},
            new(){Label="recall_potion_biome", Type="string"},
            new(){Label="gemstone_slots", Type="numeric"},
            new(){Label="attributes", Type="attributes"},// needs preprocessing
            new(){Label="thunger_charge", Type="numeric"},
            new(){Label="ghast_blaster", Type="numeric"},
            new(){Label="EXE", Type="numeric"},
            new(){Label="WAI", Type="numeric"},
            new(){Label="glowing", Type="numeric"},
            new(){Label="ZEE", Type="numeric"},
            new(){Label="blaze_consumer", Type="numeric"},
            new(){Label="magma_cube_absorber", Type="numeric"},
            new(){Label="td_attune_mode", Type="numeric"}
        };

        public string GetHeader()
        {
            return "uuid,item_id,price," + string.Join(',', Order.Select(o => o.Label));
        }

        public string Transform(SaveAuction auction)
        {
            var itemTag = auction.Tag;
            if (Regex.IsMatch(itemTag, @"^PET_(?!ITEM)(?!SKIN)\w+"))
            {
                itemTag = "PET";
            }
            else if (itemTag.StartsWith("RUNE_"))
            {
                itemTag = "RUNE";
            }
            else if (itemTag.StartsWith("POTION_"))
            {
                itemTag = "POTION";
            }
            var values = Order.Select(item =>
            {
                if (auction.FlatenedNBT.TryGetValue(item.Label, out string value) && (item.Type.StartsWith("numeric") || item.Type == "string"))
                {
                    return value;
                }
                return item.Type switch
                {
                    "numeric" => "-1",
                    "numeric0" => "0",
                    "string" => "",
                    "reforge" => auction.Reforge.ToString(),
                    "count" => auction.Count.ToString(),
                    "gems" => SerializeGems(auction),
                    "ability_scroll" => SerializeAbilityScroll(auction),
                    "enchantments" => PrintEnchants(auction),
                    "runes" => PrintNbt(auction, "runes"),
                    "necromancer_souls" => Necromancer(auction),
                    "attributes" => PrintNbt(auction, "attributes"),
                    "hpc" => NbtString(auction, "hpc", "0"),
                    _ => throw new CoflnetException("transform", "unkown type " + item.Type)
                };

            });
            var builder = new StringBuilder(100);
            builder.AppendJoin(',', auction.Uuid, itemTag, auction.HighestBidAmount);
            builder.Append(',').AppendJoin(',', values);
            builder.AppendLine();
            return builder.ToString();
        }

        private static string SerializeGems(SaveAuction auction)
        {
            if (auction.NbtData.Data.TryGetValue("gems", out object gems))
                return JsonConvert.SerializeObject(gems);
            return "";
        }
        private static string SerializeAbilityScroll(SaveAuction auction)
        {
            if (auction.NbtData.Data.TryGetValue("ability_scroll", out object gems))
                return JsonConvert.SerializeObject(gems);
            return "[]";
        }
        private static string PrintEnchants(SaveAuction auction)
        {
            var dict = new Dictionary<string, int>();
            foreach (var item in auction.Enchantments)
            {
                dict[item.Type.ToString()] = item.Level;
            }
            return JsonConvert.SerializeObject(dict);
        }
        private static string PrintNbt(SaveAuction auction, string key)
        {
            if (auction.NbtData.Data.TryGetValue(key, out object runes))
                return JsonConvert.SerializeObject(runes);
            return "{}";
        }
        private static string NbtString(SaveAuction auction, string key, string defaultString = "")
        {
            return auction.FlatenedNBT.Where(f => f.Key == key).Select(f=>f.Value).FirstOrDefault(defaultString);
        }

        private static string Necromancer(SaveAuction auction)
        {
            if (auction.NbtData.Data.TryGetValue("necromancer_souls", out object runes))
            {
                var list = new List<string>();
                foreach (dynamic item in runes as List<object>)
                {
                    list.Add(item["mob_id"].ToString());
                }
                return JsonConvert.SerializeObject(list);
            }

            return "[]";
        }

        private static void Add(StringBuilder builder, string val)
        {
            builder.Append(val);
            builder.Append(',');
        }

        public class Element
        {
            public string Label;
            public string Type;
        }
    }
}