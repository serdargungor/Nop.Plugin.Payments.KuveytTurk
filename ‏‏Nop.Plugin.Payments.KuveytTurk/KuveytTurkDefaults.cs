namespace Nop.Plugin.Payments.KuveytTurk
{
    /// <summary>
    /// Represents plugin constants
    /// </summary>
    public class KuveytTurkDefaults
    {
        /// <summary>
        /// Gets a name of the view component to display payment info in public store
        /// </summary>
        public const string PAYMENT_INFO_VIEW_COMPONENT_NAME = "PaymentKuveytTurk";

        /// <summary>
        /// Gets payment method system name
        /// </summary>
        public static string SystemName => "Payments.KuveytTurk";

        /// <summary>
        /// Gets IPN handler route name
        /// </summary>
        public static string Payment => "Plugin.Payments.KuveytTurk.Payment";
        public static string Fail => "Plugin.Payments.KuveytTurk.Fail";
        public static string Approval => "Plugin.Payments.KuveytTurk.Approval";
        public static string SendApprove => "Plugin.Payments.KuveytTurk.SendApprove";

        public static string OrderPaymentsDirectory => "OrderPayments";

        public static string LocalizationStringStart => "Plugins.Payments.KuveytTurk.";
    }
}