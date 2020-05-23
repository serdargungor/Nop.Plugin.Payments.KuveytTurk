using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.KuveytTurk.Models;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.KuveytTurk.Services
{
    public class KuveytTurkService
    {
        private readonly IWebHelper _webHelper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INopFileProvider _nopFileProvider;
        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;
        private readonly KuveytTurkPaymentSettings _kuveytTurkPaymentSettings;

        public KuveytTurkService(
            IWebHelper webHelper,
            IWebHostEnvironment webHostEnvironment,
            INopFileProvider nopFileProvider,
            IWorkContext workContext,
            ILocalizationService localizationService,
            KuveytTurkPaymentSettings kuveytTurkPaymentSettings)
        {
            _webHelper = webHelper;
            _webHostEnvironment = webHostEnvironment;
            _nopFileProvider = nopFileProvider;
            _workContext = workContext;
            _localizationService = localizationService;
            _kuveytTurkPaymentSettings = kuveytTurkPaymentSettings;
        }

        /// <summary>
        /// Hash some data in one string result
        /// </summary>
        /// <param name="merchantId"></param>
        /// <param name="merchantOrderId"></param>
        /// <param name="amount"></param>
        /// <param name="okUrl"></param>
        /// <param name="failUrl"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>Hashed data</returns>
        public string CreateHash(string merchantId, string merchantOrderId, string amount, string okUrl, string failUrl, string userName, string password)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            var hashedPassword = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
            var hashstr = merchantId + merchantOrderId + amount + okUrl + failUrl + userName + hashedPassword;
            var hashbytes = System.Text.Encoding.GetEncoding("ISO-8859-9").GetBytes(hashstr);
            return Convert.ToBase64String(sha.ComputeHash(hashbytes));
        }

        /// <summary>
        /// Send payment data to url
        /// </summary>
        /// <param name="url">url to send data to it</param>
        /// <param name="postdata">Data object</param>
        /// <returns>result</returns>
        public string PostPaymentDataToUrl(string url, string postdata)
        {
            var buffer = Encoding.UTF8.GetBytes(postdata);
            var webReq = WebRequest.Create(url) as HttpWebRequest;
            webReq.Timeout = 5 * 60 * 1000;
            webReq.Method = "POST";
            webReq.ContentType = "application/xml";
            webReq.ContentLength = buffer.Length;
            webReq.CookieContainer = new CookieContainer();

            var reqStream = webReq.GetRequestStream();
            reqStream.Write(buffer, 0, buffer.Length);
            reqStream.Close();

            var webRes = webReq.GetResponse();
            var resStream = webRes.GetResponseStream();
            var resReader = new StreamReader(resStream);

            return resReader.ReadToEnd();
        }

        /// <summary>
        /// Get data from ProcessPaymentRequest and generat XML result
        /// </summary>
        /// <param name="processPaymentRequest">data object</param>
        /// <returns>Xml result</returns>
        public string GetDataAsXml(ProcessPaymentRequest processPaymentRequest)
        {
            //Order amount (100 = 1TL)
            var amount = Convert.ToInt32(processPaymentRequest.OrderTotal * 100).ToString();
            //Order guid number
            var merchantOrderId = processPaymentRequest.OrderGuid.ToString();
            //Failed url (Redirect to this url if failed)
            var okUrl = $"{_webHelper.GetStoreLocation()}Plugins/PaymentKuveytTurk/SendApprove";
            //Success url (Redirect to this url if success 3D code)
            var failUrl = $"{_webHelper.GetStoreLocation()}Plugins/PaymentKuveytTurk/Fail";
            //Name of card holder
            var cardHolderName = processPaymentRequest.CreditCardName;
            //Card number (16 number)
            var cardNumber = processPaymentRequest.CreditCardNumber;
            //Card expire date (year like 2021)
            var cardExpireDateYear = processPaymentRequest.CreditCardExpireYear;
            //Card expire date (month like 1-12)
            var cardExpireDateMonth = processPaymentRequest.CreditCardExpireMonth;
            //Card CVV2 code (in back side of card 3-4 number)
            var cardCVV2 = processPaymentRequest.CreditCardCvv2;
            //Merchant number (Mağaza Kodu)
            var merchantId = _kuveytTurkPaymentSettings.MerchantId;
            //Customer number (Account Number)
            var customerId = _kuveytTurkPaymentSettings.CustomerId;
            //UserName in KuveytTurk SanalPos Control Panel (Api User)
            var userName = _kuveytTurkPaymentSettings.UserName;
            //Password in KuveytTurk SanalPos Control Panel (Api Password)
            var password = _kuveytTurkPaymentSettings.Password;
            //Transaction type (Sale or )
            var transactionType = "Sale";
            //Number of installments (0 if cash)
            var installmentCount = "0";
            //Currency code (TL = 0949)
            var currencyCode = "0949";
            //Transaction Security (For 3D enter 3)
            var transactionSecurity = "3";
            //Hashed code generated from merchantId, merchantOrderId, amount, okUrl, failUrl, userName and password
            var hashData = CreateHash(merchantId, merchantOrderId, amount, okUrl, failUrl, userName, password);

            //Generate XML code to send it to PayGate
            var postData =
                "<KuveytTurkVPosMessage xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>" +
                "<APIVersion>1.0.0</APIVersion>" +
                "<OkUrl>" + okUrl + "</OkUrl>" +
                "<FailUrl>" + failUrl + "</FailUrl>" +
                "<HashData>" + hashData + "</HashData>" +
                "<MerchantId>" + merchantId + "</MerchantId>" +
                "<CustomerId>" + customerId + "</CustomerId>" +
                "<UserName>" + userName + "</UserName>" +
                "<CardNumber>" + cardNumber + "</CardNumber>" +
                "<CardExpireDateYear>" + cardExpireDateYear.ToString().Substring(2, 2) + "</CardExpireDateYear>" +
                "<CardExpireDateMonth>" + cardExpireDateMonth.ToString("D2") + "</CardExpireDateMonth>" +
                "<CardCVV2>" + cardCVV2 + "</CardCVV2>" +
                "<CardHolderName>" + cardHolderName + "</CardHolderName>" +
                "<CardType>Troy</CardType>" +
                "<BatchID>0</BatchID>" +
                "<TransactionType>" + transactionType + "</TransactionType>" +
                "<InstallmentCount>" + installmentCount + "</InstallmentCount>" +
                "<Amount>" + amount + "</Amount>" +
                "<DisplayAmount>" + amount + "</DisplayAmount>" +
                "<CurrencyCode>" + currencyCode + "</CurrencyCode>" +
                "<MerchantOrderId>" + merchantOrderId + "</MerchantOrderId>" +
                "<TransactionSecurity>" + transactionSecurity + "</TransactionSecurity>" +
                "</KuveytTurkVPosMessage>";
            return postData;
        }

        /// <summary>
        /// Create OrderPayments directory and create file and save code in it
        /// </summary>
        /// <param name="result">Html code</param>
        public string PutHtmlCodeInFile(string result)
        {
            //Create OrderPayments directory if not excist
            var dir = $"{_webHostEnvironment.WebRootPath}\\{KuveytTurkDefaults.OrderPaymentsDirectory}";
            _nopFileProvider.CreateDirectory(dir);

            //Write result to file and create if not excist
            var file = "tmp" + DateTime.UtcNow.Ticks + "{" + _workContext.CurrentCustomer.Id + "}.html";
            _nopFileProvider.WriteAllText($"{dir}\\{file}", result, Encoding.UTF8);

            return file;
        }

        /// <summary>
        /// Delete temp files of this customer in OrderPayments directory
        /// </summary>
        public void ClearOrderPaymentsFiles(int customerId)
        {
            var searchBy = "*{" + customerId + "}*.html";
            var files = _nopFileProvider.GetFiles($"{_webHostEnvironment.WebRootPath}\\{KuveytTurkDefaults.OrderPaymentsDirectory}", searchBy);

            foreach (var file in files)
                _nopFileProvider.DeleteFile(file);
        }

        /// <summary>
        /// DeSerialize Xml object to VPosTransactionResponseContract object
        /// </summary>
        /// <param name="response">Xml objext</param>
        /// <returns>VPosTransactionResponseContract object</returns>
        public VPosTransactionResponseContract GetVPosTransactionResponseContract(string response)
        {
            var resp = System.Web.HttpUtility.UrlDecode(response);
            var x = new XmlSerializer(typeof(VPosTransactionResponseContract));
            var model = new VPosTransactionResponseContract();
            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(resp)))
            {
                model = x.Deserialize(ms) as VPosTransactionResponseContract;
            }

            return model;
        }


        /// <summary>
        /// Get Error Message from Localization Service
        /// </summary>
        /// <param name="responseCode">Error code</param>
        /// <returns>Localization Error Message</returns>
        public string GetErrorMessage(string responseCode)
        {
            string response;
            var startResponse = KuveytTurkDefaults.LocalizationStringStart;

            if (responseCode == "00")
                response = $"{startResponse}PaymentDone";
            else if (responseCode == "01")
                response = $"{startResponse}KartiVerenBankayiAraLim";
            else if (responseCode == "02")
                response = $"{startResponse}KartiVerenBankayiArayiniz";
            else if (responseCode == "03")
                response = $"{startResponse}GecersizUyeIsyeri";
            else if (responseCode == "04")
                response = $"{startResponse}KartaElKoyunuz";
            else if (responseCode == "05")
                response = $"{startResponse}IslemOnaylanmadi";
            else if (responseCode == "09")
                response = $"{startResponse}TekrarDeneyiniz";
            else if (responseCode == "11")
                response = $"{startResponse}VipIslemIcinOnayVerildi";
            else if (responseCode == "12")
                response = $"{startResponse}GecersizIslem";
            else if (responseCode == "13")
                response = $"{startResponse}GecersizIslemTutari";
            else if (responseCode == "14")
                response = $"{startResponse}GecersizKartNumarasi";
            else if (responseCode == "15")
                response = $"{startResponse}KartVerenBankaTanimsiz";
            else if (responseCode == "33")
                response = $"{startResponse}VadeSonuGecmisKartaElKoy";
            else if (responseCode == "34")
                response = $"{startResponse}SahtekarlikKartaelKoyunuz";
            else if (responseCode == "36")
                response = $"{startResponse}KisitliKartKartaElKoyunuz";
            else if (responseCode == "37")
                response = $"{startResponse}GuvenligiUyarinizKartaElKoyunuz";
            else if (responseCode == "41")
                response = $"{startResponse}KayipKartKartaElKoy";
            else if (responseCode == "43")
                response = $"{startResponse}CalintiKartKartaElKoy";
            else if (responseCode == "51")
                response = $"{startResponse}BakiyesiKrediLimitiYetersiz";
            else if (responseCode == "53")
                response = $"{startResponse}DovizHesabiBulunamasi";
            else if (responseCode == "54")
                response = $"{startResponse}VadeSonuGecmisKart";
            else if (responseCode == "55")
                response = $"{startResponse}HataliKartSifresi";
            else if (responseCode == "56")
                response = $"{startResponse}KartTanimliDegil";
            else if (responseCode == "57")
                response = $"{startResponse}IslemTipineIzinYok";
            else if (responseCode == "58")
                response = $"{startResponse}IslemTipiTerminaleKapali";
            else if (responseCode == "59")
                response = $"{startResponse}SahtekarlikSuphesi";
            else if (responseCode == "61")
                response = $"{startResponse}ParaCekmeTutarLimitiAsild";
            else if (responseCode == "62")
                response = $"{startResponse}KisitlanmisKart";
            else if (responseCode == "63")
                response = $"{startResponse}GuvenlikIhlali";
            else if (responseCode == "65")
                response = $"{startResponse}ParaÇekmeAdetLimitiAsildi";
            else if (responseCode == "66")
                response = $"{startResponse}IslemiReddedinizGuvenligi";
            else if (responseCode == "67")
                response = $"{startResponse}BuHesaptaHicbirIslemYapila";
            else if (responseCode == "68")
                response = $"{startResponse}TanimsizSube";
            else if (responseCode == "75")
                response = $"{startResponse}SifreDenemeSayisiAsildi";
            else if (responseCode == "76")
                response = $"{startResponse}SifrelerUyusmuyorKey";
            else if (responseCode == "77")
                response = $"{startResponse}SifreScriptTalebiReddedildi";
            else if (responseCode == "78")
                response = $"{startResponse}SifreGuvenilirBulanmadi";
            else if (responseCode == "79")
                response = $"{startResponse}ARQCKontroluBasarisiz";
            else if (responseCode == "85")
                response = $"{startResponse}SifreDegisikligi/YuklemeOnay";
            else if (responseCode == "88")
                response = $"{startResponse}IslemSupheliTamamlandiKontrol";
            else if (responseCode == "89")
                response = $"{startResponse}EkKartIleBuIslemYapilmaz";
            else if (responseCode == "90")
                response = $"{startResponse}GunSonuDevamEdiyor";
            else if (responseCode == "91")
                response = $"{startResponse}KartiVerenBankaHizmetdisi";
            else if (responseCode == "92")
                response = $"{startResponse}KartVerenBankaTanimliDegil";
            else if (responseCode == "96")
                response = $"{startResponse}SistemArizali";
            else if (responseCode == "PosMerchantIPError")
                response = $"{startResponse}IPAdresiTanimliDegildir";
            else
                response = $"{startResponse}OtherError";

            return _localizationService.GetResource(response);
        }
    }
}
