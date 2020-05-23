using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.KuveytTurk
{
    /// <summary>
    /// Represents plugin settings
    /// </summary>
    public class KuveytTurkPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets an account Id
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// Gets or sets a MerchantId
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets a Username
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets a Password
        /// </summary>
        public string Password { get; set; }
    }
}