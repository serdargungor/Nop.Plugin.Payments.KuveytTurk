using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.KuveytTurk.Infrastructure
{
    /// <summary>
    /// Represents plugin route provider
    /// </summary>
    public class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(KuveytTurkDefaults.Payment, "Plugins/PaymentKuveytTurk/Payment",
                 new { controller = "PaymentKuveytTurk", action = "Payment" });

            endpointRouteBuilder.MapControllerRoute(KuveytTurkDefaults.Fail, "Plugins/PaymentKuveytTurk/Fail",
                 new { controller = "PaymentKuveytTurk", action = "Fail" });

            endpointRouteBuilder.MapControllerRoute(KuveytTurkDefaults.Approval, "Plugins/PaymentKuveytTurk/Approval",
                 new { controller = "PaymentKuveytTurk", action = "Approval" });

            endpointRouteBuilder.MapControllerRoute(KuveytTurkDefaults.SendApprove, "Plugins/PaymentKuveytTurk/SendApprove",
                 new { controller = "PaymentKuveytTurk", action = "SendApprove" });

        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => 0;
    }
}