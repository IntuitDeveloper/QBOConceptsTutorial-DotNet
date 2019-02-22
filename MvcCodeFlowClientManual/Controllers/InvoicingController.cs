
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
    public class InvoicingController : AppController
    {
        // GET: Customer
        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// This workflow coverscreating account, item, customer, invoice,payment and sending invoice
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> InvoicingWorkflow()
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

                    //Create Account 
                    Account account = CreateAccount();
                    Account accountAdded = dataService.Add<Account>(account);

                    //Add customer
                    Customer customer = CreateCustomer();
                    Customer customerCreated = dataService.Add<Customer>(customer);

                    //Add item
                    Item item = CreateItem(accountAdded);
                    Item itemAdded = dataService.Add<Item>(item);

                    //Add Invoice
                    Invoice objInvoice = CreateInvoice(realmId, customerCreated, itemAdded);
                    Invoice addedInvoice = dataService.Add<Invoice>(objInvoice);
                    //Email invoice

                    // sending invoice 
                    dataService.SendEmail<Invoice>(addedInvoice, "abc@gmail.com");

                    //Recieve payment for this invoice

                    Payment payment = CreatePayment(customerCreated, addedInvoice);
                    dataService.Add<Payment>(payment);

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

        #region create account
        private Account CreateAccount()
        {

            Random randomNum = new Random();
            Account account = new Account();


            account.Name = "Name_" + randomNum.Next();

            account.FullyQualifiedName = account.Name;

            account.Classification = AccountClassificationEnum.Revenue;
            account.ClassificationSpecified = true;
            account.AccountType = AccountTypeEnum.Bank;
            account.AccountTypeSpecified = true;

            account.CurrencyRef = new ReferenceType()
            {
                name = "United States Dollar",
                Value = "USD"
            };

            return account;
        }
    #endregion

        #region create item
        /// <summary>
        /// This API creates invoice item 
        /// </summary>
        /// <returns></returns>
        private Item CreateItem(Account incomeAccount)
        {

            Item item = new Item();

            Random randomNum = new Random();

            item.Name = "Replacement of Item-" + randomNum.Next();
            item.Description = "Description";
            item.Type = ItemTypeEnum.NonInventory;
            item.TypeSpecified = true;

            item.Active = true;
            item.ActiveSpecified = true;


            item.Taxable = false;
            item.TaxableSpecified = true;

            item.UnitPrice = new Decimal(100.00);
            item.UnitPriceSpecified = true;

            item.TrackQtyOnHand = false;
            item.TrackQtyOnHandSpecified = true;


            item.IncomeAccountRef = new ReferenceType()
            {
                name = incomeAccount.Name,
                Value = incomeAccount.Id
            };

            item.ExpenseAccountRef = new ReferenceType()
            {
                name = incomeAccount.Name,
                Value = incomeAccount.Id
            };

            //For inventory item, assetacocunref is required
            return item;

        }
        #endregion

        #region create customer
        /// <summary>
        /// This API creates customer 
        /// </summary>
        /// <returns></returns>
        private Customer CreateCustomer()
        {
            Random random = new Random();
            Customer customer = new Customer();
            
            customer.GivenName = "Bob" + random.Next();
            customer.FamilyName = "Serling";
            customer.DisplayName = customer.CompanyName;
            return customer;
        }


        /// <summary>
        /// This API creates an Invoice
        /// </summary>
        private Invoice CreateInvoice(string realmId, Customer customer, Item item)
        {


            // Step 1: Initialize OAuth2RequestValidator and ServiceContext
            ServiceContext serviceContext = IntializeContext(realmId);

            // Step 2: Initialize an Invoice object
            Invoice invoice = new Invoice();
            // invoice.Deposit = new Decimal(0.00);
            //invoice.DepositSpecified = true;
            
            
            // Step 3: Invoice is always created for a customer so lets retrieve reference to a customer and set it in Invoice
            /*QueryService<Customer> querySvc = new QueryService<Customer>(serviceContext);
            Customer customer = querySvc.ExecuteIdsQuery("SELECT * FROM Customer WHERE CompanyName like 'Amy%'").FirstOrDefault();*/
            invoice.CustomerRef = new ReferenceType()
            {
                Value = customer.Id
            };


            // Step 4: Invoice is always created for an item so lets retrieve reference to an item and a Line item to the invoice
            /* QueryService<Item> querySvcItem = new QueryService<Item>(serviceContext);
            Item item = querySvcItem.ExecuteIdsQuery("SELECT * FROM Item WHERE Name = 'Lighting'").FirstOrDefault();*/
            List<Line> lineList = new List<Line>();
            Line line = new Line();
            line.Description = "Description";
            line.Amount = new Decimal(100.00);
            line.AmountSpecified = true;

            SalesItemLineDetail salesItemLineDetail = new SalesItemLineDetail();
            salesItemLineDetail.Qty = new Decimal(1.0);
            salesItemLineDetail.ItemRef = new ReferenceType()
            {
                Value = item.Id
            };
            line.AnyIntuitObject = salesItemLineDetail;

            line.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
            line.DetailTypeSpecified = true;

            lineList.Add(line);
            invoice.Line = lineList.ToArray();

            // Step 5: Set other properties such as Total Amount, Due Date, Email status and Transaction Date
            invoice.DueDate = DateTime.UtcNow.Date;
            invoice.DueDateSpecified = true;


            invoice.TotalAmt = new Decimal(10.00);
            invoice.TotalAmtSpecified = true;

            invoice.EmailStatus = EmailStatusEnum.NotSet;
            invoice.EmailStatusSpecified = true;

            invoice.Balance = new Decimal(10.00);
            invoice.BalanceSpecified = true;

            invoice.TxnDate = DateTime.UtcNow.Date;
            invoice.TxnDateSpecified = true;
            invoice.TxnTaxDetail = new TxnTaxDetail()
            {
                TotalTax = Convert.ToDecimal(10),
                TotalTaxSpecified = true,
            };
            return invoice;
        }
#endregion

        #region create payment
        /// <summary>
        /// Creating payment transaction - Make sure payment is created for same customerref as invoice r must be parent of the customeref which invoice has
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="invoiceCreated"></param>
        /// <returns></returns>
        private Payment CreatePayment(Customer customer, Invoice invoiceCreated)
        {
            Payment payment = new Payment();
            payment.CustomerRef = new ReferenceType
            {
                name = customer.DisplayName,
                Value = customer.Id
            };
            payment.CurrencyRef = new ReferenceType
            {
                type = "Currency",
                Value = "USD"
            };
            payment.TotalAmt = invoiceCreated.TotalAmt;
            payment.TotalAmtSpecified = true;

            List<LinkedTxn> linkedTxns = new List<LinkedTxn>();
            linkedTxns.Add(new LinkedTxn()
            {
                TxnId = invoiceCreated.Id,
                TxnType = TxnTypeEnum.Invoice.ToString()
            });

            foreach (Line line in invoiceCreated.Line)
            {
                line.LinkedTxn = linkedTxns.ToArray();
            }

            payment.Line = invoiceCreated.Line;
            return payment;
        }
    }
    #endregion
}