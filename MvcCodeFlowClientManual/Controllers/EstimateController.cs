using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Intuit.Ipp.DataService;

namespace MvcCodeFlowClientManual.Controllers
{
    public class EstimateController : AppController
    {
        // GET: Estimate
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> EstimateCall()
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

                    // Create estimate object 
                    Estimate estimate = new Estimate();

                    // Add estimate fields

                    DataService dataService = new DataService(serviceContext);

                    // Add estimate to QBO using dataService

                    return View("EstimateCall", (object)("QBO API calls Success!"));
                }
                catch (Exception ex)
                {
                    return View("EstimateCall", (object)"QBO API calls Failed!");
                }

            }
            else
                return View("EstimateCall", (object)"QBO API call Failed!");
        }
    }
}