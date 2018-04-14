using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Intuit.Ipp.DataService;

namespace MvcCodeFlowClientManual.Controllers
{
    public class ItemController : AppController
    {
        // GET: Item
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> ItemCall()
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

                    Item item = new Item();


                    // Add item fields

                    // Add item income account

                    // Add item expense account

                    DataService dataService = new DataService(serviceContext);

                    // Add item to QBO using dataService

                    return View("ItemCall", (object)("QBO API calls Success!"));

                }
                catch (Exception ex)
                {
                    return View("ItemCall", (object)"QBO API calls Failed!");
                }

            }
            else
                return View("ItemCall", (object)"QBO API call Failed!");
        }
    }
}