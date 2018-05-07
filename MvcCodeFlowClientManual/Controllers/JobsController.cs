
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
using Intuit.Ipp.DataService;

namespace MvcCodeFlowClientManual.Controllers
{
    public class JobsController : AppController
    {
        // GET: Estimate
        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// This workflow covers create customer, item, estimate and invoice and updating estimate
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> JobsWorkflow()
        {
            //Make QBO api calls using .Net SDK
            if (Session["realmId"] != null)
            {
                string realmId = Session["realmId"].ToString();

                try
                {
             
                    //Initialize OAuth2RequestValidator and ServiceContext
                    ServiceContext serviceContext = base.IntializeContext(realmId);
                    DataService dataService = new DataService(serviceContext);

                    #region add customer
                    // Add Customer
                    Customer customer = new Customer();
                    String customerName = "Brad Smith";
                    //Check if the customer already exists
                    QueryService<Customer> customerQueryService = new QueryService<Customer>(serviceContext);
                    List<Customer> existingCustomers = customerQueryService.ExecuteIdsQuery($"Select * From Customer WHERE DisplayName='{customerName}'").ToList<Customer>();
                    if (existingCustomers.Count > 0) {
                        //Use the existing customer if already in QuickBooks
                        customer = existingCustomers[0];
                    } else
                    {
                        //Create a new customer in QuickBooks
                        customer.DisplayName = customerName;
                        customer = dataService.Add(customer);
                    }
                    #endregion

                    #region add item
                    // Add Item
                    Item item = new Item();
                    String itemName = "Hair Bronzing";
                    //Check if the item already exists
                    QueryService<Item> itemQueryService = new QueryService<Item>(serviceContext);
                    List<Item> existingItems = itemQueryService.ExecuteIdsQuery($"Select * From Item WHERE Name='{itemName}'").ToList<Item>();
                    if (existingItems.Count > 0) {
                        //Use the existing item if it already exists
                        item = existingItems[0];
                    }
                    else
                    {
                        //Find an account for the new item
                        Account account = new Account();
                        String servicesAccountName = "Services Income";
                        //Check if the item's account already exists
                        QueryService<Account> serviceAccountQueryService = new QueryService<Account>(serviceContext);
                        List<Account> existingServiceAccounts = serviceAccountQueryService.ExecuteIdsQuery($"Select * From Account Where Name='{servicesAccountName}'").ToList<Account>();
                        if (existingServiceAccounts.Count > 0) {
                            //Use the existing account if it is already in QuickBooks
                            account = existingServiceAccounts[0];
                        } else
                        {
                            //Create a new services account in QuickBooks
                            account.Name = servicesAccountName;
                            account.AccountType = AccountTypeEnum.Income;
                            account.AccountTypeSpecified = true;
                            account = dataService.Add<Account>(account);
                        }
                        //Create a new item in QuickBooks
                        item.Name = itemName;
                        item.Type = ItemTypeEnum.Service;
                        item.TypeSpecified = true;
                        //Set the item's income account to the service account
                        item.IncomeAccountRef = new ReferenceType()
                        {
                            type = objectNameEnumType.Account.ToString(),
                            Value = account.Id
                        };
                        item = dataService.Add(item);
                    }
                    #endregion

                    #region create estimate
                    // Create Estimate
                    Estimate estimate = new Estimate();
                    estimate.CustomerRef = new ReferenceType()
                    {
                        type = objectNameEnumType.Customer.ToString(),
                        Value = customer.Id
                    };
                    List<Line> lineList = new List<Line>();
                    Line line = new Line();
                    line.Description = "Wake up with perfect hair!";
                    line.Amount = new Decimal(9000.00);
                    line.AmountSpecified = true;
                    line.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                    line.DetailTypeSpecified = true;
             
                    line.AnyIntuitObject = new SalesItemLineDetail() {
                        ItemRef = new ReferenceType() {
                            Value = item.Id
                        }
                    };
                    lineList.Add(line);
                    estimate.Line = lineList.ToArray();
                    estimate = dataService.Add<Estimate>(estimate);
                    #endregion

                    #region update estimate amount
                    // Update Amount in Estimate
                    lineList = estimate.Line.ToList();
                    foreach (Line estimateLine in lineList) {
                        if (estimateLine.DetailType == LineDetailTypeEnum.SalesItemLineDetail) {
                            //Find the estimate line to update
                            if (((SalesItemLineDetail)estimateLine.AnyIntuitObject).ItemRef.Value == item.Id) {
                                estimateLine.Amount = 18000;
                                estimateLine.AmountSpecified = true;
                            }
                        }
                    }
                    estimate = dataService.Update<Estimate>(estimate);
                    #endregion

                    #region convert/link estimate to invoice
                    // Convert Estimate to Invoice
                    Invoice invoice = new Invoice();
                    invoice.CustomerRef = estimate.CustomerRef;
                    invoice.Line = estimate.Line;
                    //Include a reference to the Estimate on the new Invoice
                    LinkedTxn estimateLink = new LinkedTxn()
                    {
                        TxnType = "Estimate",
                        TxnId = estimate.Id
                    };
                    invoice.LinkedTxn = new LinkedTxn[] { estimateLink };
                    invoice = dataService.Add<Invoice>(invoice);
                    
                    // Update Invoice to add $5 Discount
                    Account discountAccount = new Account();
                    String discountAccountName = "Discounts given";
                    //Check if the discount account already exists
                    QueryService<Account> discountAccountQueryService = new QueryService<Account>(serviceContext);
                    List<Account> existingDiscountAccounts = discountAccountQueryService.ExecuteIdsQuery($"Select * From Account Where Name='{discountAccountName}'").ToList<Account>();
                    if (existingDiscountAccounts.Count > 0)
                    {
                        //Use the existing discount account if it is already in QuickBooks
                        discountAccount = existingDiscountAccounts[0];
                    }
                    else
                    {
                        //Create a new discount account in QuickBooks
                        discountAccount.Name = discountAccountName;
                        discountAccount.AccountSubType = AccountSubTypeEnum.DiscountsRefundsGiven.ToString();
                        discountAccount.SubAccountSpecified = true;
                        discountAccount = dataService.Add<Account>(discountAccount);
                    }
                    lineList = invoice.Line.ToList();
                    line = new Line();
                    line.Amount = new Decimal(5.00);
                    line.AmountSpecified = true;
                    line.DetailType = LineDetailTypeEnum.DiscountLineDetail;
                    line.DetailTypeSpecified = true;
                    //Use the discount account on a new invoice line
                    line.AnyIntuitObject = new DiscountLineDetail()
                    {
                        PercentBased = false,
                        PercentBasedSpecified = true,
                        DiscountAccountRef = new ReferenceType()
                        {
                            Value = discountAccount.Id
                        }
                    };
                    //Add the new discount line to the invoice
                    lineList.Add(line);
                    invoice.Line = lineList.ToArray();
                    //Set DepositSpecified explicity to false, as it may not be supported by all QBO SKUs
                    invoice.DepositSpecified = false;
                    invoice = dataService.Update<Invoice>(invoice);
                    #endregion

                    return View("Index", (object)("QBO API calls Success!"));
                }
                catch (Exception ex)
                {
                    return View("Index", (object)"QBO API calls Failed!");
                }

            }
            else
                return View("Index", (object)"QBO API call Failed!");
        }
    }
}