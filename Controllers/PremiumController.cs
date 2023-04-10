

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Coflnet.Payments.Client.Api;
using Coflnet.Sky.Filter;
using Coflnet.Sky.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Coflnet.Payments.Client.Model;
using Coflnet.Sky.Api;
using Coflnet.Sky.Api.Models;
using System.Threading;

namespace Coflnet.Sky.Api.Controller
{
    /// <summary>
    /// Endpoints for related to paid services
    /// </summary>
    [ApiController]
    [Route("api")]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, NoStore = false)]
    public class PremiumController : ControllerBase
    {
        private ProductsApi productsService;
        private TopUpApi topUpApi;
        private UserApi userApi;
        private GoogletokenService tokenService;

        /// <summary>
        /// Creates a new intance of <see cref="PremiumController"/>
        /// </summary>
        /// <param name="productsService"></param>
        /// <param name="topUpApi"></param>
        /// <param name="userApi"></param>
        /// <param name="premiumService"></param>
        public PremiumController(ProductsApi productsService, TopUpApi topUpApi, UserApi userApi, GoogletokenService premiumService)
        {
            this.productsService = productsService;
            this.topUpApi = topUpApi;
            this.userApi = userApi;
            this.tokenService = premiumService;
        }

        /// <summary>
        /// Products to top up
        /// </summary>
        /// <returns></returns>
        [Route("topup/options")]
        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IEnumerable<Payments.Client.Model.TopUpProduct>> TopupOptions()
        {
            var products = await productsService.ProductsTopupGetAsync();
            return products;
        }

        /// <summary>
        /// Start a new topup session with stripe
        /// </summary>
        /// <returns></returns>
        [Route("topup/stripe/{productSlug}")]
        [HttpPost]
        public async Task<IActionResult> StartTopUp(string productSlug, [FromBody] TopUpArguments args)
        {
            foreach (var item in Request.Headers)
            {
                Console.WriteLine(item.Key + ": " + String.Join(", ", item.Value));
            }
            var fingerprint = GetBrowserFingerprint();
            Console.WriteLine("Fingerprint: " + fingerprint);


            if (!TryGetUser(out GoogleUser user))
                return Unauthorized("no googletoken header");

            TopUpOptions options = GetOptions(args, fingerprint, user);

            var session = await topUpApi.TopUpStripePostAsync(user.Id.ToString(), productSlug, options);
            if (options.UserIp == "172.93.179.188")
                throw new CoflnetException("blacklisted_ip", "You are banned from using this service");
            return Ok(session);
        }

        private TopUpOptions GetOptions(TopUpArguments args, string fingerprint, GoogleUser user)
        {
            var realIp = (Request.Headers.Where(h => h.Key.ToLower() == "x-original-forwarded-for" || h.Key.ToUpper() == "CF-Connecting-IP").Select(h => h.Value).First()).ToString();
            Console.WriteLine("RealIp: " + realIp);
            var locale = "de-DE";
            if (Request.Headers.TryGetValue("cf-ipcountry", out StringValues country))
            {
                locale = country.ToString();
            }
            else if (Request.Headers.TryGetValue("accept-language", out StringValues acceptLanguage))
            {
                locale = acceptLanguage.First().ToString();
            }
            var options = new TopUpOptions()
            {
                UserEmail = user.Email,
                TopUpAmount = args.CoinAmount,
                UserIp = realIp,
                Fingerprint = fingerprint,
                Locale = locale
            };
            return options;
        }

        /// <summary>
        /// Start a new topup session with paypal
        /// </summary>
        /// <returns></returns>
        [Route("topup/paypal/{productSlug}")]
        [HttpPost]
        public async Task<IActionResult> StartTopUpPaypal(string productSlug, [FromBody] TopUpArguments args)
        {
            if (!TryGetUser(out GoogleUser user))
                return Unauthorized("no googletoken header");

            var session = await topUpApi.TopUpPaypalPostAsync(user.Id.ToString(), productSlug, new TopUpOptions()
            {
                UserEmail = user.Email,
                TopUpAmount = args.CoinAmount,
                SuccessUrl = args.SuccessUrl,
                CancelUrl = args.CancelUrl
            });
            return Ok(session);
        }

        private string GetBrowserFingerprint()
        {
            var userAgent = this.Request.Headers["User-Agent"].ToString();
            var acceptLanguage = this.Request.Headers["Accept-Language"].ToString();
            var acceptEncoding = this.Request.Headers["Accept-Encoding"].ToString();
            var accept = this.Request.Headers["Accept"].ToString();
            var referer = this.Request.Headers["Referer"].ToString();
            var host = this.Request.Headers["Host"].ToString();
            var connection = this.Request.Headers["Connection"].ToString();
            var md5hash = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(userAgent + acceptLanguage + acceptEncoding + accept + referer + host + connection));
            var hash = BitConverter.ToString(md5hash).Replace("-", "").ToLowerInvariant();
            return hash;
        }


        /// <summary>
        /// Purchase a service 
        /// </summary>
        /// <returns></returns>
        [Route("service/purchase")]
        [HttpPost]
        public async Task<IActionResult> PurchaseService([FromBody] PurchaseArgs args)
        {
            if (!TryGetUser(out GoogleUser user))
                return Unauthorized("no googletoken header");
            try
            {
                var reference = args.reference;
                var count = args.count == 0 ? 1 : args.count;
                if (string.IsNullOrEmpty(reference))
                    reference = "apiautofill" + DateTime.UtcNow;
                var purchaseResult = await userApi.UserUserIdServicePurchaseProductSlugPostAsync(user.Id.ToString(), args.slug, reference, count);
                return Ok(purchaseResult);
            }
            catch (Exception e)
            {
                throw new CoflnetException("payment_error", e.Message);
            }
        }

        /// <summary>
        /// Get adjusted prices
        /// </summary>
        /// <returns></returns>
        [Route("premium/prices/adjusted")]
        [HttpPost]
        public async Task<IActionResult> PurchaseService([FromBody] IEnumerable<string> slugs)
        {
            if (!TryGetUser(out GoogleUser user))
                return Unauthorized("no googletoken header");
            try
            {
                var adjusted = await productsService.ProductsUserUserIdGetAsync(user.Id.ToString(), slugs.ToList());
                if (adjusted == null)
                    return NotFound();
                return Ok(adjusted);
            }
            catch (Exception e)
            {
                throw new CoflnetException("payment_error", e.Message);
            }
        }
        /// <summary>
        /// Get adjusted prices
        /// </summary>
        /// <returns></returns>
        [Route("premium/user/owns")]
        [HttpPost]
        public async Task<ActionResult<Dictionary<string, Sky.Api.Models.OwnerShip>>> GetOwnerShips([FromBody] List<string> slugsToTest)
        {
            if (!TryGetUser(out GoogleUser user))
                return Unauthorized("no googletoken header");
            try
            {
                var cancelationSource = new CancellationTokenSource(10_000);
                var owns = await userApi.UserUserIdOwnsUntilPostAsync(user.Id.ToString(), slugsToTest, 0, cancelationSource.Token);
                if (owns == null)
                    return NotFound();
                return Ok(owns.Where(o => o.Value > DateTime.Now).ToDictionary(o => o.Key, o => new Sky.Api.Models.OwnerShip()
                {
                    ExpiresAt = o.Value
                }));
            }
            catch (Exception e)
            {
                throw new CoflnetException("payment_error", e.Message);
            }
        }

        private bool TryGetUser(out GoogleUser user)
        {
            user = default(GoogleUser);
            if (!Request.Headers.TryGetValue("GoogleToken", out StringValues value))
                return false;
            user = tokenService.GetUserWithToken(value);
            return true;
        }
    }
}

