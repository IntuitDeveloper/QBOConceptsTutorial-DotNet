using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Intuit.Ipp.ReportService;

namespace MvcCodeFlowClientManual.Controllers
{
    public class ReportsController : AppController
    {
        // GET: Reports
        public ActionResult Index()
        {
            return View();
        }
        public async Task<ActionResult> ReportCall()
        {
            //Make QBO api calls using .Net SDK
            if (Session["realmId"] != null)
            {
                string realmId = Session["realmId"].ToString();

                try
                {
                    // Use access token to retrieve company Info and create an Invoice
                    //Initialize OAuth2RequestValidator and ServiceContext
                    ServiceContext serviceContext = base.IntializeContext(realmId);

                    ReportService reportService = new ReportService(serviceContext);
                    // Add report query parameters

                    Report report_pnl = reportService.ExecuteReport("ProfitAndLoss");

                    Report report_balance_sheet = reportService.ExecuteReport("BalanceSheet");

                    return View("ReportCall", (object)("QBO API calls success!"));
                }
                catch (Exception ex)
                {
                    return View("ReportCall", (object)"QBO API calls Failed!");
                }

            }
            else
                return View("ReportCall", (object)"QBO API call Failed!");
        }
    }
}