using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Payments.KuveytTurk.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.KuveytTurk.Components
{
    /// <summary>
    /// Represents payment info view component
    /// </summary>
    [ViewComponent(Name = KuveytTurkDefaults.PAYMENT_INFO_VIEW_COMPONENT_NAME)]
    public class PaymentKuveytTurkViewComponent : NopViewComponent
    {
        /// <summary>
        /// Invoke view component
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data</param>
        /// <returns>View component result</returns>
        public IViewComponentResult Invoke()//string widgetZone, object additionalData)
        {
            var model = new PaymentInfoModel();

            //years
            for (var i = DateTime.UtcNow.Year; i < DateTime.UtcNow.Year + 15; i++)
                model.ExpireYears.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString(), });

            //months
            for (var i = 1; i <= 12; i++)
                model.ExpireMonths.Add(new SelectListItem { Text = i.ToString("D2"), Value = i.ToString(), });

            //set postback values (we cannot access "Form" with "GET" requests)
            if (Request.Method != WebRequestMethods.Http.Get)
            {
                model.CardholderName = Request.Form["CardholderName"];
                model.CardNumber = Request.Form["CardNumber"];
                model.CardCode = Request.Form["CardCode"];
                var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(Request.Form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedMonth != null)
                    selectedMonth.Selected = true;
                var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(Request.Form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedYear != null)
                    selectedYear.Selected = true;
            }

            return View("~/Plugins/Payments.KuveytTurk/Views/PaymentInfo.cshtml", model);
        }
    }
}