using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Html;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.KuveytTurk.Models;
using Nop.Plugin.Payments.KuveytTurk.Services;
using Nop.Plugin.Payments.KuveytTurk.Validators;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Mvc.Filters;
using Ubiety.Dns.Core;

namespace Nop.Plugin.Payments.KuveytTurk
{
    /// <summary>
    /// Represents KuveytTurk payment processor
    /// </summary>
    public class KuveytTurkPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICurrencyService _currencyService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IEncryptionService _encryptionService;
        private readonly ICustomerService _customerService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INopFileProvider _nopFileProvider;
        private readonly KuveytTurkService _kuveytTurkService;
        private readonly KuveytTurkPaymentSettings _kuveytTurkPaymentSettings;

        #endregion

        #region Ctor

        public KuveytTurkPaymentProcessor(CurrencySettings currencySettings,
            ICurrencyService currencyService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IPaymentService paymentService,
            ISettingService settingService,
            IWebHelper webHelper,
            IEncryptionService encryptionService,
            ICustomerService customerService,
            IWebHostEnvironment webHostEnvironment,
            INopFileProvider nopFileProvider,
            KuveytTurkService kuveytTurkService,
            KuveytTurkPaymentSettings twoCheckoutPaymentSettings)
        {
            _currencySettings = currencySettings;
            _currencyService = currencyService;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _paymentService = paymentService;
            _settingService = settingService;
            _webHelper = webHelper;
            _encryptionService = encryptionService;
            _customerService = customerService;
            _webHostEnvironment = webHostEnvironment;
            _nopFileProvider = nopFileProvider;
            _kuveytTurkService = kuveytTurkService;
            _kuveytTurkPaymentSettings = twoCheckoutPaymentSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult
            {
                AllowStoringCreditCardNumber = true,
            };
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //Get payment details
            var creditCardName = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardName);
            var creditCardNumber = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardNumber);
            var creditCardExpirationYear = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardExpirationYear);
            var creditCardExpirationMonth = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardExpirationMonth);
            var creditCardCvv2 = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardCvv2);

            //Save details in an object
            var processPaymentRequest = new ProcessPaymentRequest
            {
                CreditCardName = creditCardName,
                CreditCardNumber = creditCardNumber,
                CreditCardExpireYear = Convert.ToInt32(creditCardExpirationYear),
                CreditCardExpireMonth = Convert.ToInt32(creditCardExpirationMonth),
                CreditCardCvv2 = creditCardCvv2,
                OrderGuid = postProcessPaymentRequest.Order.OrderGuid,
                OrderTotal = postProcessPaymentRequest.Order.OrderTotal,
            };

            //Convert data from ProcessPaymentRequest to Xml object
            var postData = _kuveytTurkService.GetDataAsXml(processPaymentRequest);
            //Send Xml object to url and get result
            var result = _kuveytTurkService.PostPaymentDataToUrl("https://boa.kuveytturk.com.tr/sanalposservice/Home/ThreeDModelPayGate", postData);

            //Create directory and save Html Code in it
            var file = _kuveytTurkService.PutHtmlCodeInFile(result);

            //Redirect to new file HTML page
            _httpContextAccessor.HttpContext.Response.Redirect($"{_webHelper.GetStoreLocation()}OrderPayments/{file}");
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
            //return _paymentService.CalculateAdditionalFee(cart,
            //    _twoCheckoutPaymentSettings.AdditionalFee, _twoCheckoutPaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //do not allow reposting (it can take up to several hours until your order is reviewed
            return false;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest
            {
                CreditCardType = form["CreditCardType"],
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            };
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentKuveytTurk/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return KuveytTurkDefaults.PAYMENT_INFO_VIEW_COMPONENT_NAME;
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new KuveytTurkPaymentSettings()
            {
                //UseSandbox = true,
                //UseMd5Hashing = true
            });

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}CustomerId", "Account No");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}CustomerId.Hint", "Enter account no.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}MerchantId", "Merchant No");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}MerchantId.Hint", "Enter merchant no.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}UserName", "User Name");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}UserName.Hint", "Enter User Name.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}Password", "Password");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}Password.Hint", "Enter Password.");

            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}PaymentMethodDescription", "You will be redirected to pay after complete the order.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}RedirectionTip", "You will be redirected to pay after complete the order.");

            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}PaymentDone", "Payment have been done!");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartiVerenBankayiAraLim", "Call the bank.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GecersizUyeIsyeri", "Invalid Member Merchant.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartaElKoyunuz", "Not working card.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemOnaylanmadi", "The transaction has not been approved.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}VipIslemIcinOnayVerildi", "Approved for VIP Operation.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GecersizIslem", "No Transaction.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GecersizIslemTutari", "No Transaction Amount.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GecersizKartNumarasi", "Invalid Card Number.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartVerenBankaTanimsiz", "Card Issuer Bank Undefined.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}VadeSonuGecmisKartaElKoy", "Seize Card Overdue.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SahtekarlikKartaelKoyunuz", "Falsify Your Card.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KisitliKartKartaElKoyunuz", "Card limit exceeded.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GuvenligiUyarinizKartaElKoyunuz", "The card was rejected for security reasons.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KayipKartKartaElKoy", "Lost card.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}CalintiKartKartaElKoy", "Stolen card.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}BakiyesiKrediLimitiYetersiz", "Balance Credit Limit Insufficient.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}DovizHesabiBulunamasi", "No Exchange Account Found.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}VadeSonuGecmisKart", "Expiry Card.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}HataliKartSifresi", "Wrong Card Password.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartTanimliDegil", "Card Not Defined.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemTipineIzinYok", "No Transaction Type.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemTipiTerminaleKapali", "Operation Type Closed to Terminal.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SahtekarlikSuphesi", "Suspicion of Fraud.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}ParaCekmeTutarLimitiAsild", "Withdrawal Amount Limit Exceeded.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KisitlanmisKart", "Restricted Card.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GuvenlikIhlali", "Security Violation.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}ParaÇekmeAdetLimitiAsildi", "Withdrawal Number Limit Exceeded.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemiReddedinizGuvenligi", "Transaction Rejected Security.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}BuHesaptaHicbirIslemYapila", "No Transactions Made On This Account.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}TanimsizSube", "Undefined Branch.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifreDenemeSayisiAsildi", "Number of Enter Password Excided.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifrelerUyusmuyorKey", "Encryption Key is not Match.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifreScriptTalebiReddedildi", "Password Script Request Denied.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifreGuvenilirBulanmadi", "Security Password Not Found.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}ARQCKontroluBasarisiz", "ARQC Control Failed.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifreDegisikligi/YuklemeOnay", "Password Change/Download Confirmation.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemSupheliTamamlandiKontrol", "Operation Suspicious Completed Check.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}EkKartIleBuIslemYapilmaz", "This Operation Cannot Be Done By Additional Card.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GunSonuDevamEdiyor", "End of Day Calculating Continues.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartiVerenBankaHizmetdisi", "Bank Issuing Card Out of Service.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartVerenBankaTanimliDegil", "Unknown Bank Card.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SistemArizali", "Problem in system.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IPAdresiTanimliDegildir", "Your IP is not define.");
            _localizationService.AddOrUpdatePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}OtherError", "Unknown Error.");

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<KuveytTurkPaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}CustomerId");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}CustomerId.Hint");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}MerchantId");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}MerchantId.Hint");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}UserName");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}UserName.Hint");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}Password");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}Password.Hint");

            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}PaymentMethodDescription");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}RedirectionTip");

            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}PaymentDone");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartiVerenBankayiAraLim");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GecersizUyeIsyeri");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartaElKoyunuz");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemOnaylanmadi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}VipIslemIcinOnayVerildi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GecersizIslem");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GecersizIslemTutari");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GecersizKartNumarasi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartVerenBankaTanimsiz");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}VadeSonuGecmisKartaElKoy");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SahtekarlikKartaelKoyunuz");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KisitliKartKartaElKoyunuz");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GuvenligiUyarinizKartaElKoyunuz");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KayipKartKartaElKoy");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}CalintiKartKartaElKoy");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}BakiyesiKrediLimitiYetersiz");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}DovizHesabiBulunamasi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}VadeSonuGecmisKart");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}HataliKartSifresi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartTanimliDegil");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemTipineIzinYok");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemTipiTerminaleKapali");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SahtekarlikSuphesi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}ParaCekmeTutarLimitiAsild");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KisitlanmisKart");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GuvenlikIhlali");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}ParaÇekmeAdetLimitiAsildi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemiReddedinizGuvenligi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}BuHesaptaHicbirIslemYapila");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}TanimsizSube");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifreDenemeSayisiAsildi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifrelerUyusmuyorKey");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifreScriptTalebiReddedildi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifreGuvenilirBulanmadi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}ARQCKontroluBasarisiz");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SifreDegisikligi/YuklemeOnay");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IslemSupheliTamamlandiKontrol");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}EkKartIleBuIslemYapilmaz");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}GunSonuDevamEdiyor");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartiVerenBankaHizmetdisi");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}KartVerenBankaTanimliDegil");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}SistemArizali");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}IPAdresiTanimliDegildir");
            _localizationService.DeletePluginLocaleResource($"{KuveytTurkDefaults.LocalizationStringStart}OtherError");

            base.Uninstall();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => false;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription => _localizationService.GetResource("Plugins.Payments.KuveytTurk.PaymentMethodDescription");

        #endregion
    }
}