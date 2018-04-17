using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Intuit.Ipp.DataService;
using System.Collections.ObjectModel;
using Intuit.Ipp.QueryFilter;

namespace MvcCodeFlowClientManual.Controllers
{
    public class AccountingController : AppController
    {
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> AccountingWorkflow()
        {
            //Make QBO api calls using .Net SDK
            if (Session["realmId"] != null)
            {
                string realmId = Session["realmId"].ToString();

                try
                {  
                    //Initialize ServiceContext
                    ServiceContext serviceContext = base.IntializeContext(realmId);
                    // Create Journal Entry by creating 2 accounts(Bank, Credit Card);
                    //Adding the Bill using Dataservice object
                    DataService service = new DataService(serviceContext);
                    JournalEntry journalEntryRequest = CreateJournalEntry(serviceContext);
                    JournalEntry journalEntryResponse = service.Add<JournalEntry>(journalEntryRequest);

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
        /// <summary>
        /// Generates GUID and gives it back
        /// </summary>
        /// <returns></returns>
        internal static string GetGuid()
        {
            return Guid.NewGuid().ToString("N");
        }

        
        /// <summary>
        /// creates Journal Entries by creating/updating Bank & Credit Card accounts
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static JournalEntry CreateJournalEntry(ServiceContext context)
        {
            //Initializing the Dataservice object with ServiceContext
            DataService service = new DataService(context);

            //Create JournalEntry
            JournalEntry journalEntry = new JournalEntry();
            journalEntry.Adjustment = true;
            journalEntry.AdjustmentSpecified = true;

            journalEntry.DocNumber = "DocNumber" + GetGuid().Substring(0, 5);
            journalEntry.TxnDate = DateTime.UtcNow.Date;
            journalEntry.TxnDateSpecified = true;


            List<Line> lineList = new List<Line>();

            // Create/Update Bank Account Line
            Line debitLine = new Line();
            debitLine.Description = "nov portion of rider insurance";
            debitLine.Amount = new Decimal(100.00);
            debitLine.AmountSpecified = true;
            debitLine.DetailType = LineDetailTypeEnum.JournalEntryLineDetail;
            debitLine.DetailTypeSpecified = true;
            JournalEntryLineDetail journalEntryLineDetail = new JournalEntryLineDetail();
            journalEntryLineDetail.PostingType = PostingTypeEnum.Debit;
            journalEntryLineDetail.PostingTypeSpecified = true;

            //Find or create account

            Account typeOfAccount = null;
            AccountTypeEnum accountType = AccountTypeEnum.Bank;
            string accountTypeName = "Bank";

            //Find existing account by accounttype
            QueryService<Account> querySvc = new QueryService<Account>(context);
            Account existingAccount = querySvc.ExecuteIdsQuery("select * from account where accounttype='" + accountTypeName + "'").FirstOrDefault();

            if (existingAccount != null)
            {
                if (existingAccount.AccountType == accountType && existingAccount.Classification == AccountClassificationEnum.Asset && existingAccount.status != EntityStatusEnum.SyncError)
                {
                    typeOfAccount = existingAccount;
                }
            }

            if (typeOfAccount == null)
            {
                Account account = new Account();

                String guid = GetGuid();
                account.Name = "Name_";

                account.FullyQualifiedName = account.Name;

                account.Classification = AccountClassificationEnum.Asset;
                account.ClassificationSpecified = true;
                account.AccountType = accountType;
                account.AccountTypeSpecified = true;

                if (accountType != AccountTypeEnum.Expense && accountType != AccountTypeEnum.AccountsPayable && accountType != AccountTypeEnum.AccountsReceivable)
                {

                }

                account.CurrencyRef = new ReferenceType()
                {
                    name = "United States Dollar",
                    Value = "USD"
                };

                //Adding the Bill using Dataservice object
                Account addedAccount = service.Add<Account>(account);
                typeOfAccount = addedAccount;
            }

            Account bankAccount = typeOfAccount;

            journalEntryLineDetail.AccountRef = new ReferenceType() { type = Enum.GetName(typeof(objectNameEnumType), objectNameEnumType.Account), name = bankAccount.Name, Value = bankAccount.Id };
            debitLine.AnyIntuitObject = journalEntryLineDetail;
            lineList.Add(debitLine);

            // Create/Update Credit Card Account
            Line creditLine = new Line();
            creditLine.Description = "nov portion of rider insurance";
            creditLine.Amount = new Decimal(100.00);
            creditLine.AmountSpecified = true;
            creditLine.DetailType = LineDetailTypeEnum.JournalEntryLineDetail;
            creditLine.DetailTypeSpecified = true;
            JournalEntryLineDetail journalEntryLineDetailCredit = new JournalEntryLineDetail();
            journalEntryLineDetailCredit.PostingType = PostingTypeEnum.Credit;
            journalEntryLineDetailCredit.PostingTypeSpecified = true;
            //Find or create account

            accountType = AccountTypeEnum.CreditCard;
            accountTypeName = "Credit Card";
            //Find existing account by accounttype

            querySvc = new QueryService<Account>(context);
            existingAccount = null;
            existingAccount = querySvc.ExecuteIdsQuery("select * from account where accounttype='" + accountTypeName + "'").FirstOrDefault();

            if (existingAccount != null)
            {
                if (existingAccount.AccountType == accountType && existingAccount.Classification == AccountClassificationEnum.Liability && existingAccount.status != EntityStatusEnum.SyncError)
                {
                    typeOfAccount = existingAccount;
                }
            }

            if (typeOfAccount == null)
            {
                Account account = new Account();

                String guid = GetGuid();
                account.Name = "Name_";

                account.FullyQualifiedName = account.Name;

                account.Classification = AccountClassificationEnum.Liability;
                account.ClassificationSpecified = true;
                account.AccountType = accountType;
                account.AccountTypeSpecified = true;

                if (accountType != AccountTypeEnum.Expense && accountType != AccountTypeEnum.AccountsPayable && accountType != AccountTypeEnum.AccountsReceivable)
                {

                }

                account.CurrencyRef = new ReferenceType()
                {
                    name = "United States Dollar",
                    Value = "USD"
                };

                //Adding the Bill using Dataservice object
                Account addedAccount = service.Add<Account>(account);
                typeOfAccount = addedAccount;
            }

            Account assetAccount = typeOfAccount;

            journalEntryLineDetailCredit.AccountRef = new ReferenceType() { type = Enum.GetName(typeof(objectNameEnumType), objectNameEnumType.Account), name = assetAccount.Name, Value = assetAccount.Id };
            creditLine.AnyIntuitObject = journalEntryLineDetailCredit;
            lineList.Add(creditLine);

            journalEntry.Line = lineList.ToArray();

            return journalEntry;
        }
    }
}