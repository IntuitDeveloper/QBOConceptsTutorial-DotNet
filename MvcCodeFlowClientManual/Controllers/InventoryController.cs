
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
    public class InventoryController : AppController
    {
        // GET: Item
        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// This workflow covers creating accounts, inventory item, invoice
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> InventoryWorkflow()
        {
            //Make QBO api calls using .Net SDK
            if (Session["realmId"] != null)
            {
                string realmId = Session["realmId"].ToString();

                try
                {
                    
                    //Initialize ServiceContext
                    ServiceContext serviceContext = base.IntializeContext(realmId);
                    DataService dataService = new DataService(serviceContext);

                    //Create Income, Expense and Asset Accounts into Chart of Accounts 
                    //We will use these accounts in Inventory creation

                    //Create Income Account
                    Account addedIncomeAccount = CreateAccount(dataService,AccountTypeEnum.Income, AccountSubTypeEnum.SalesOfProductIncome);
                    //Create Expense Account
                    Account addedExpenseAccount = CreateAccount(dataService, AccountTypeEnum.CostofGoodsSold, AccountSubTypeEnum.SuppliesMaterialsCogs);
                    //Create Asset Account
                    Account addedAssetAccount = CreateAccount(dataService, AccountTypeEnum.OtherAsset, AccountSubTypeEnum.Inventory);

                    //Creating Inventory Item and using all the above accounts 
                    // Add inventory item with initial quantity on hand =10, income account, expense account and asset account to the item
                    Item addedInventory = CreateInventoryItem(dataService, addedIncomeAccount, addedExpenseAccount, addedAssetAccount);
                    //Create Invoice with the above item
                    Invoice addedInvoice = CreateInvoice(dataService, serviceContext, addedInventory);
                    // Query quantity for the Inventory item
                    Item queryInventory = QueryItemByName(serviceContext, addedInventory.Name);
                    
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


        private Item CreateInventoryItem(DataService dataService, Account incomeAccount, Account expenseAccount, Account assetAccount) {
            Item newItem = new Item
            {
                Type = ItemTypeEnum.Inventory,
                Name = "My Inventory 15"  +Guid.NewGuid().ToString("N"), // Please change the name every time
                QtyOnHand = 10,
                InvStartDate = DateTime.Today,
                Description = "New Inventory with quantity 10",
                TrackQtyOnHand = true,
                TypeSpecified = true,
                QtyOnHandSpecified = true,
                TrackQtyOnHandSpecified = true,
                InvStartDateSpecified = true
            };

            //QueryService<Account> querySvc = new QueryService<Account>(serviceContext);

            newItem.IncomeAccountRef = new ReferenceType()
            {
                Value = incomeAccount.Id
            };

            newItem.ExpenseAccountRef = new ReferenceType()
            {
                Value = expenseAccount.Id
            };

            newItem.AssetAccountRef = new ReferenceType()
            {
                Value = assetAccount.Id
            };

            return dataService.Add<Item>(newItem);
        }

        private Account CreateAccount(DataService dataService, AccountTypeEnum type, AccountSubTypeEnum subType) {
            // We are creating new Account by type and subtype which we will use for new inventory
            // The Account name should be unique
            // Following lines are just object creation, to create this account in QBO it should follow by the service call
            Random random = new Random();
            Account newAccount = new Account
            {
                
                Name = "My "+type.ToString()+ random.Next(), // Dont forget to change the name before running the code
                AccountType = type,
                AccountSubType = subType.ToString(),
                AccountTypeSpecified = true,
                SubAccountSpecified = true
            };
            // Following line will create incomeAccount in quickbooks with Name= My Income Account
            return dataService.Add<Account>(newAccount);
        }

        private Invoice CreateInvoice(DataService dataService,ServiceContext serviceContext, Item item)
        {
            //Initialize an Invoice object
            Invoice invoice = new Invoice();
            //invoice.Deposit = new Decimal(0.00);
            //invoice.DepositSpecified = true;

            //Invoice is always created for a customer so lets retrieve reference to a customer and set it in Invoice
            QueryService<Customer> querySvc = new QueryService<Customer>(serviceContext);
            Customer customer = querySvc.ExecuteIdsQuery("SELECT * FROM Customer WHERE CompanyName like 'Amy%'").FirstOrDefault();
            invoice.CustomerRef = new ReferenceType()
            {
                Value = customer.Id
            };


            List<Line> lineList = new List<Line>();
            Line line = new Line();
            line.Description = "Description";
            line.Amount = new Decimal(100.00);
            line.AmountSpecified = true;
            lineList.Add(line);
            invoice.Line = lineList.ToArray();

            SalesItemLineDetail salesItemLineDetail = new SalesItemLineDetail();
            salesItemLineDetail.Qty = new Decimal(1.0);
            salesItemLineDetail.QtySpecified = true;
            salesItemLineDetail.ItemRef = new ReferenceType()
            {
                Value = item.Id
            };
            line.AnyIntuitObject = salesItemLineDetail;

            line.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
            line.DetailTypeSpecified = true;
            
            //Set other properties such as Total Amount, Due Date, Email status and Transaction Date
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
            return dataService.Add<Invoice>(invoice);
        }

        private Item QueryItemByName(ServiceContext serviceContext, string itemName) {
            QueryService<Item> querySvcItem = new QueryService<Item>(serviceContext);
            //As item name is unique we can query and get single item
            Item item = querySvcItem.ExecuteIdsQuery("SELECT * FROM Item WHERE Name = '"+itemName+"'").FirstOrDefault();
            return item;

        }
    }
}
