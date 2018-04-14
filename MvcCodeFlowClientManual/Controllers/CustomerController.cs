using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Intuit.Ipp.DataService;

namespace MvcCodeFlowClientManual.Controllers
{
    public class CustomerController : AppController
    {
        // GET: Customer
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> CustomerCall()
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

                    // Create customer object 
                    Customer customer = new Customer();

                    // Add custoemr fields

                    DataService dataService = new DataService(serviceContext);

                    // Add customer to QBO using dataService

                    return View("CustomerCall", (object)("QBO API calls Success!"));
                }
                catch (Exception ex)
                {
                    return View("CustomerCall", (object)"QBO API calls Failed!");
                }

            }
            else
                return View("CustomerCall", (object)"QBO API call Failed!");
        }
    }
}