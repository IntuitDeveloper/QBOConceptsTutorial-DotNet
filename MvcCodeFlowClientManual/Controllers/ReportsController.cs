
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
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
        public async Task<ActionResult> ReportsWorkflow()
        {
            //Make QBO api calls using .Net SDK
            if (Session["realmId"] != null)
            {
                string realmId = Session["realmId"].ToString();

                try
                {
                    
                    //Initialize OAuth2RequestValidator and ServiceContext
                    ServiceContext serviceContext = base.IntializeContext(realmId);

                    // Create ReportService object
                    ReportService defaultReportService = new ReportService(serviceContext);

                    /*
                     * Read default profit and Loss report: 
                     * Default date used: This fiscal year to date
                     * Deafult accounting method: Defined in Preferences.ReportPrefs.ReportBasis. The two accepted values are "Accural" and "Cash"
                     * Default includes data for all customers
                     * */
                    Report defaultPnLReport = defaultReportService.ExecuteReport("ProfitAndLoss");

                    /*
                     * Read default Balance sheet report:
                     * Default date used: This fiscal year to date
                     * Deafult accounting method: Defined in Preferences.ReportPrefs.ReportBasis. The two accepted values are "Accural" and "Cash"
                     * Default includes data for all customers
                     * Default it is summarized by Total
                     * */
                    ReportService defaultReportService1 = new ReportService(serviceContext);
                    Report defaultBalanceSheet = defaultReportService1.ExecuteReport("BalanceSheet");

                    /*  Get Balnace Sheet report for given start and end date
                     *  set start_date and end_date properties in the ReportService instance with the date range in yyyy-mm-dd format
                     * */
                    ReportService dateRangeReportService = new ReportService(serviceContext);
                    dateRangeReportService.start_date = "2018-01-01";
                    dateRangeReportService.end_date = "2018-04-15";
                    Report dateRangeBalanceSheet = dateRangeReportService.ExecuteReport("BalanceSheet");

                    /*  Get Profit and Loss report for given start and end date
                     *  set start_date and end_date properties in the ReportService instance with the date range in yyyy-mm-dd format
                     * */
                    ReportService dateRangeReportService1 = new ReportService(serviceContext);
                    dateRangeReportService1.start_date = "2018-01-01";
                    dateRangeReportService1.end_date = "2018-04-15";
                    Report dateRangePnL = dateRangeReportService1.ExecuteReport("ProfitAndLoss");

                    /*  Get Profit and Loss report for given start and end date and cash accounting method
                     *  set accounting_method property to "Cash"
                     * */
                    ReportService cashReportService = new ReportService(serviceContext);
                    cashReportService.start_date = "2018-01-01";
                    cashReportService.end_date = "2018-04-15";
                    cashReportService.accounting_method = "Cash";
                    Report cashPnLReport = cashReportService.ExecuteReport("ProfitAndLoss");

                    /*  Get Balance Sheet report for given start and end date and cash accounting method
                     *  set accounting_method property to "Cash"
                     * */
                    ReportService cashReportService1 = new ReportService(serviceContext);
                    cashReportService1.start_date = "2018-01-01";
                    cashReportService1.end_date = "2018-04-15";
                    cashReportService1.accounting_method = "Cash";
                    Report cashBalanceSheet = cashReportService1.ExecuteReport("BalanceSheet");

                    /* Year End Balance Sheet report summarized by Customer
                     * set the customer property to the customer.Id and set summarize_column_by property to "Customers"
                     * You can also set customer property with multiple customer ids comma seperated.
                     * You can summarize by the following:Total, Customers, Vendors, Classes, Departments, Employees, ProductsAndServices by setting the summarize_column_by property
                     * */
                    ReportService customerReportService = new ReportService(serviceContext);
                    customerReportService.start_date = "2018-01-01";
                    customerReportService.end_date = "2018-12-31";
                    customerReportService.customer = "1";
                    customerReportService.summarize_column_by = "Customers";
                    Report yearEndReportByCustomer = customerReportService.ExecuteReport("BalanceSheet");

                    /* Year End report summarized by Customer
                     * set the customer property to the customer.Id and set summarize_column_by property to "Customers"
                     * You can also set customer property with multiple customer ids comma seperated.
                     * You can summarize by the following:Total, Customers, Vendors, Classes, Departments, Employees, ProductsAndServices by setting the summarize_column_by property
                     * */
                    ReportService customerReportService1 = new ReportService(serviceContext);
                    customerReportService1.start_date = "2018-01-01";
                    customerReportService1.end_date = "2018-12-31";
                    customerReportService1.customer = "1";
                    customerReportService1.summarize_column_by = "Customers";
                    Report yearEndPnLByCustomer = customerReportService1.ExecuteReport("ProfitAndLoss");

                    /* Since we are calling the service with different parameters you could also use the getReport helper method
                     * by passing the different query parameters as shown
                     * Report report_pnl = getReport(serviceContext, "2015-01-01", "2015-03-01", "Accural", "ProfitAndLoss");
                     * Report report_balance_sheet =getReport(serviceContext, "", "", "Cash", "BalanceSheet");
                     * */


                    return View("Index", (object)("QBO API calls success!"));
                }
                catch (Exception ex)
                {
                    return View("Index", (object)"QBO API calls Failed!");
                }

            }
            else
                return View("Index", (object)"QBO API call Failed!");
        }

        /*
         * Helper method that take parameters to generate Report. It can be extended to add more query parameters. Using the most used as parameters
         * for now.
        public Report getReport(ServiceContext context, String startDate, String endDate, String accountingMethod, String reportName){
            // Create ReportService object
            ReportService reportService =  new ReportService(context);
            //Set properties for Report
            if(!String.IsNullOrEmpty(startDate))
            {
                reportService.start_date = startDate;
            }
            if (!String.IsNullOrEmpty(endDate))
            {
                reportService.end_date = endDate;
            }
            if (!String.IsNullOrEmpty(accountingMethod))
            {
                reportService.accounting_method = accountingMethod;
            }
            //Execute Report API call
            Report report = reportService.ExecuteReport(reportName);
            return report;

        }
        */
    }
}