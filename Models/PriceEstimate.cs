namespace Coflnet.Sky.Api.Models;

public class PriceEstimate
{
    /// <summary>
    /// The best matching lowest bin found on ah
    /// </summary>
    public long Lbin { get; set; }
    /// <summary>
    /// The best matching median sell price
    /// </summary>
    public long Median { get; set; }
    /// <summary>
    /// average 24 hour sell volume
    /// </summary>
    public float Volume { get; set; }
    /// <summary>
    /// The link to best matching lbin
    /// </summary>
    public string LbinLink { get; set; }
    /// <summary>
    /// Suggested price for selling withing 5 minutes
    /// </summary>
    public long FastSell { get; set; }
    /// <summary>
    /// They key for the lbin
    /// </summary>
    public string LbinKey { get; set; }
    /// <summary>
    /// The key of the median price
    /// </summary>
    public string MedianKey { get; set; }
    /// <summary>
    /// The key of the 
    /// </summary>
    public string ItemKey { get; set; }
}