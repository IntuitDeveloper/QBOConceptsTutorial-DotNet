using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Intuit.Ipp.DataService;

namespace MvcCodeFlowClientManual.Controllers
{
    public class AccountController : AppController
    {
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> AccountCall()
        {
            //Make QBO api calls using .Net SDK
            if (Session["realmId"] != null)
            {
                string realmId = Session["realmId"].ToString();

                try
                {
                    // Use access token to retrieve company Info and create an Invoice
                    //Initialize ServiceContext
                    ServiceContext serviceContext = base.IntializeContext(realmId);

                    // Create account object 
                    Account account = new Account();

                    // Add account fields

                    DataService dataService = new DataService(serviceContext);

                    // Add account to QBO using dataService

                    return View("AccountCall", (object)("QBO API calls Success!"));
                }
                catch (Exception ex)
                {
                    return View("AccountCall", (object)"QBO API calls Failed!");
                }

            }
            else
                return View("AccountCall", (object)"QBO API call Failed!");
        }
    }
}