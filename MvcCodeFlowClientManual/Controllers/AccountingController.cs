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

                    //Create Joirnal request
                    JournalEntry journalEntryRequest = CreateJournalEntry(serviceContext);

                    // Make a QBO Journal Api call
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
        /// creates Journal Entries by creating/updating Bank & Credit Card accounts
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static JournalEntry CreateJournalEntry(ServiceContext context)
        {
            //Initializing the Dataservice object with ServiceContext
            DataService service = new DataService(context);

            //Create JournalEntry Request
            JournalEntry journalEntry = new JournalEntry();
            journalEntry.Adjustment = true;
            journalEntry.AdjustmentSpecified = true;
            journalEntry.DocNumber = "Tharak_DocNumber" + Guid.NewGuid().ToString("N").Substring(0, 5);
            journalEntry.TxnDate = DateTime.UtcNow.Date;
            journalEntry.TxnDateSpecified = true;

            // creating lines for a JournalEntry
            List<Line> lineList = new List<Line>();

            // Create debit line
            Line debitLine = new Line();
            debitLine.Description = "April portion of rider insurance";
            debitLine.Amount = new Decimal(100.00);
            debitLine.AmountSpecified = true;
            debitLine.DetailType = LineDetailTypeEnum.JournalEntryLineDetail;
            debitLine.DetailTypeSpecified = true;

            #region Create Bank Account
            //Find or create account
            Account debitAccount = null;
            //Find existing account by accounttype from database
            QueryService<Account> querySvc = new QueryService<Account>(context);
            Account existingAccount = querySvc.ExecuteIdsQuery("select * from account where accounttype='Bank'").FirstOrDefault();

            // Update Account 
            if (existingAccount != null)
            {
                if (existingAccount.AccountType == AccountTypeEnum.Bank && existingAccount.Classification == AccountClassificationEnum.Asset && existingAccount.status != EntityStatusEnum.SyncError)
                {
                    debitAccount = existingAccount;
                }
            }
            // Create new Account
            if (debitAccount == null)
            {
                debitAccount = new Account();
                debitAccount.Name = "Tharak_" + Guid.NewGuid().ToString("N");
                debitAccount.FullyQualifiedName = debitAccount.Name;
                debitAccount.Classification = AccountClassificationEnum.Asset;
                debitAccount.ClassificationSpecified = true;
                debitAccount.AccountType = AccountTypeEnum.Bank;
                debitAccount.AccountTypeSpecified = true;
                debitAccount.CurrencyRef = new ReferenceType()
                {
                    name = "United States Dollar",
                    Value = "USD"
                };

                // Calling create account service api call
                Account addedAccount = service.Add<Account>(debitAccount);
                debitAccount = addedAccount;
            }
            #endregion

            JournalEntryLineDetail journalEntryLineDetail = new JournalEntryLineDetail();
            journalEntryLineDetail.PostingType = PostingTypeEnum.Debit;
            journalEntryLineDetail.PostingTypeSpecified = true;
            journalEntryLineDetail.AccountRef = new ReferenceType() { type = Enum.GetName(typeof(objectNameEnumType), objectNameEnumType.Account), name = debitAccount.Name, Value = debitAccount.Id };
            debitLine.AnyIntuitObject = journalEntryLineDetail;
            lineList.Add(debitLine);

            #region Create CreditCard Account
            // Create Credit Card line
            Line creditLine = new Line();
            creditLine.Description = "April portion of rider insurance";
            creditLine.Amount = new Decimal(100.00);
            creditLine.AmountSpecified = true;
            creditLine.DetailType = LineDetailTypeEnum.JournalEntryLineDetail;
            creditLine.DetailTypeSpecified = true;

            //Find or create account
            Account creditAccount = null;
            //Find existing account by accounttype
            querySvc = new QueryService<Account>(context);
            existingAccount = null;
            existingAccount = querySvc.ExecuteIdsQuery("select * from account where accounttype='Credit Card'").FirstOrDefault();

            if (existingAccount != null)
            {
                if (existingAccount.AccountType == AccountTypeEnum.CreditCard && existingAccount.Classification == AccountClassificationEnum.Liability && existingAccount.status != EntityStatusEnum.SyncError)
                {
                    //Existing account
                    creditAccount = existingAccount;
                }
            }
            // New Credit Card Account
            if (creditAccount == null)
            {
                creditAccount = new Account();
                creditAccount.Name = "Tharak_" + Guid.NewGuid().ToString("N");
                creditAccount.FullyQualifiedName = creditAccount.Name;
                creditAccount.Classification = AccountClassificationEnum.Liability;
                creditAccount.ClassificationSpecified = true;
                creditAccount.AccountType = AccountTypeEnum.CreditCard;
                creditAccount.AccountTypeSpecified = true;
                creditAccount.CurrencyRef = new ReferenceType()
                {
                    name = "United States Dollar",
                    Value = "USD"
                };

                //Creating new Credit Card account by calling API call
                Account addedAccount = service.Add<Account>(creditAccount);
                creditAccount = addedAccount;
            }
            #endregion

            JournalEntryLineDetail journalEntryLineDetailCredit = new JournalEntryLineDetail();
            journalEntryLineDetailCredit.PostingType = PostingTypeEnum.Credit;
            journalEntryLineDetailCredit.PostingTypeSpecified = true;
            journalEntryLineDetailCredit.AccountRef = new ReferenceType() { type = Enum.GetName(typeof(objectNameEnumType), objectNameEnumType.Account), name = creditAccount.Name, Value = creditAccount.Id };
            creditLine.AnyIntuitObject = journalEntryLineDetailCredit;
            lineList.Add(creditLine);

            // Added both Bank & Credit Card Lines to journal
            journalEntry.Line = lineList.ToArray();

            //Return the journal request
            return journalEntry;
        }
    }
}