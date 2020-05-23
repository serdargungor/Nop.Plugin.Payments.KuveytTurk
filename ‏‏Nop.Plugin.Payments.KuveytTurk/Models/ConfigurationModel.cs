using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.KuveytTurk.Models
{
    /// <summary>
    /// Represents configuration model
    /// </summary>
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.KuveytTurk.CustomerId")]
        public string CustomerId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.KuveytTurk.MerchantId")]
        public string MerchantId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.KuveytTurk.UserName")]
        public string UserName { get; set; }

        [NopResourceDisplayName("Plugins.Payments.KuveytTurk.Password")]
        [DataType(DataType.Password)]
        [NoTrim]
        public string Password { get; set; }
    }
}