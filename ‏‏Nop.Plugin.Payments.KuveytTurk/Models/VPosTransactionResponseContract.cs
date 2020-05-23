using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.KuveytTurk.Models
{
    public class VPosTransactionResponseContract
    {

        public string ACSURL { get; set; }
        public string AuthenticationPacket { get; set; }
        public string HashData { get; set; }
        public bool IsEnrolled { get; set; }
        public bool IsSuccess { get; }
        public bool IsVirtual { get; set; }
        public string MD { get; set; }
        public string MerchantOrderId { get; set; }
        public int OrderId { get; set; }
        public string PareqHtmlFormString { get; set; }
        public string Password { get; set; }
        public string ProvisionNumber { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string RRN { get; set; }
        public string SafeKey { get; set; }
        public string Stan { get; set; }
        public DateTime TransactionTime { get; set; }
        public string TransactionType { get; set; }
        public KuveytTurkVPosMessage VPosMessage { get; set; }

    }
    public class KuveytTurkVPosMessage
    {
        public decimal Amount { get; set; }
    }
}
