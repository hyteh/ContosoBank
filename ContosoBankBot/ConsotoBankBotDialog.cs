using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using ContosoBankBot.DataModels;
using ContosoBankBot.Models;
using System.Net.Http;
using Newtonsoft.Json;
using System.Reflection;
using static ContosoBankBot.Models.CurrencyExchange;

namespace ContosoBankBot
{
    [Serializable]
    public class ConsotoBankBotDialog : IDialog<object>
    {
        private string username = "";
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        /*Greeting*/
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            await context.PostAsync("Welcome to Contoso Bank's Bot! Please enter your username or create a new user account.");
            context.Wait(InputUsername);
        }

        /*Log in or create new user account*/
        public virtual async Task InputUsername(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;

            //Check if user clears
            if (msg.Text.Equals("clear"))
            {
                username = "";
                await context.PostAsync("Cleared!");
                context.Wait(MessageReceivedAsync);
            }

            else if (msg.Text.Contains("create new user account"))
            {
                PromptDialog.Confirm(
                    context,
                    checkNewAccount,
                    "Create a new user account?",
                    "Sorry, didn't get that! Create a new user account?",
                    promptStyle: PromptStyle.Auto);
            }
            
            else
            {
                username = msg.Text;
                PromptDialog.Confirm(
                    context,
                    confirmUsername,
                    "Is " + username + " your username?",
                    "Sorry, didn't get that! Is " + username + " your username?",
                    promptStyle: PromptStyle.Auto);
            }
        }

        /*Check: if correct username or not - y/n */
        public async Task confirmUsername(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                string endOutput = "Please enter your password.";
                await context.PostAsync(endOutput);
                context.Wait(InputPassword);
            }
            else
            {
                username = "";
                await context.PostAsync("Please enter your username or create new user account.");
                context.Wait(InputUsername);
            }

        }

        /*Password - check if valid username, else check password*/
        public virtual async Task InputPassword(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            string password = msg.Text;

            //Check if user clears
            if (msg.Text.Equals("clear"))
            {
                username = "";
                await context.PostAsync("Cleared!");
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);
                //Check if user has any accounts
                if (userAcc.Count == 0)
                {
                    PromptDialog.Confirm(
                        context,
                        checkNewAccount,
                        "Invalid username. Create a new user account?",
                        "Sorry, didn't get that! Invalid username. Create a new user account?",
                        promptStyle: PromptStyle.Auto);
                }
                else
                {
                    foreach (BankAccount a in userAcc)
                    {
                        if (a.password == password)
                        {
                            string endOutput = "Login successful. \n\n Hi, " + a.Name + "! How may I help you?";
                            await context.PostAsync(endOutput);
                            context.Wait(jobs);
                            break;
                        }
                        else
                        {
                            PromptDialog.Confirm(
                            context,
                            checkWrongPassword,
                            "Wrong password. Try again?",
                            "Sorry, didn't get that! Wrong password. Try again?",
                            promptStyle: PromptStyle.Auto);
                        }
                    }
                }
            }
        }

        /*Check: if want to create new user account or not - y/n */
        public async Task checkNewAccount(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                string endOutput = "Please enter your username, password and name to be created (in that order).";
                await context.PostAsync(endOutput);
                context.Wait(createNewAccount);
            }
            else
            {
                username = "";
                await context.PostAsync("Please enter your username or create new user account.");
                context.Wait(InputUsername);
            }

        }

        /*Check: if entered wrong password or want to change username - y/n */
        public async Task checkWrongPassword(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                await context.PostAsync("Please re-enter your password.");
                context.Wait(InputPassword);
            }
            else
            {
                username = "";
                await context.PostAsync("Please enter your username or create new user account.");
                context.Wait(InputUsername);
            }
        }

        /*Create new user account*/
        public virtual async Task createNewAccount(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";

            //Check if user clears
            if (msg.Text.Equals("clear"))
            {
                username = "";
                await context.PostAsync("Cleared!");
                context.Wait(MessageReceivedAsync);
            }

            else
            {
                if (input.Length < 3) //wrong inputs 
                {
                    endOutput = "Wrong input. Please re-enter your username, password and name to be created (in that order).";
                    await context.PostAsync(endOutput);
                    context.Wait(createNewAccount);
                }
                else
                {
                    username = input[0];
                    string password = input[1];
                    string name = input[2];
                    //Get name
                    if (input.Length > 3)
                    {
                        for (int i = 3; i < input.Length; i++)
                        {
                            name += " " + input[i];
                        }
                    }

                    List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);

                    //Check if user has any accounts
                    if (userAcc.Count != 0) //Username already exist
                    {
                        endOutput = "Username already exists. Please re-enter a different username, password and name to be created (in that order).";
                        await context.PostAsync(endOutput);
                        context.Wait(createNewAccount);
                    }
                    else
                    {
                        BankAccount account = new BankAccount()
                        {
                            partitionKey = "user",
                            username = username,
                            password = password,
                            Name = name
                        };
                        await AzureManager.AzureManagerInstance.CreateAccount(account);
                        endOutput = "Account created! Hi, " + account.Name + "! How may I help you?";
                        await context.PostAsync(endOutput);
                        context.Wait(jobs);
                    }
                }
            }
        }

        /* TASKS! */
        public async Task jobs(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;

            //Check if user clears
            if (msg.Text.Equals("clear"))
            {
                username = "";
                await context.PostAsync("Cleared!");
                context.Wait(MessageReceivedAsync);
            }

            else
            {
                /**Get user's bank accounts*/
                if (msg.Text == "get accounts")
                {
                    List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username); //Get user account 
                    List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

                    //Check if user has any accounts
                    if (userBankAcc.Count == 0)
                    {
                        await context.PostAsync("No accounts available.");
                    }
                    //Else get the bank accounts
                    else
                    {
                        string endOutput = "Here are your accounts, " + userAcc[0].Name + ": \n\n";
                        foreach (BankAccount a in userBankAcc)
                        {
                            endOutput += "Account Number: " + a.accountNo + "\n\r Balance: $" + a.balance + "\n\r Account created at: " + a.date + "\n\n";
                        }
                        await context.PostAsync(endOutput);
                    }
                    await context.PostAsync("What else can I do for you?");
                    context.Wait(jobs);
                }

                /**Create new bank account for user*/
                else if (msg.Text == "create account")
                {
                    await context.PostAsync("Please enter the amount to be inserted into the new account.");
                    context.Wait(createAccount);
                }

                /**Add balance to the user's bank account */
                else if (msg.Text == "add balance")
                {
                    await context.PostAsync("Please enter the account number and amount to be inserted into the account (in that order).");
                    context.Wait(addBalance);
                }

                /**Withdraw balance from the user's bank account */
                else if (msg.Text == "withdraw balance")
                {
                    await context.PostAsync("Please enter the account number and amount to be inserted into the account (in that order).");
                    context.Wait(withdrawBalance);
                }

                /**Delete user's bank account */
                else if (msg.Text == "delete account")
                {
                    await context.PostAsync("Please enter the account number to be deleted.");
                    context.Wait(deleteAccount);
                }

                /**Check currency rates*/
                else if (msg.Text == "currency rates")
                {
                    await context.PostAsync("Please enter the base currency and exchange currency (in that order, e.g. NZD MYR).");
                    context.Wait(checkRate);
                }

                /**Log out from user's account*/
                else if (msg.Text == "log out")
                {
                    PromptDialog.Confirm(
                            context,
                            checkLogout,
                            "Are you sure you want to log out?",
                            "Sorry, didn't get that! Are you sure you want to log out?",
                            promptStyle: PromptStyle.Auto);
                }

                /**No such job or wrong input*/
                else
                {
                    await context.PostAsync("Sorry, didn't get that! Please try again.");
                    context.Wait(jobs);
                }
            }
        }

        /*Check: if want to logout or not*/
        public async Task checkLogout(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                username = "";
                await context.PostAsync("Logged out!");
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                await context.PostAsync("What else can I do for you?");
                context.Wait(jobs);
            }
        }

        /*Creating new bank account for user*/
        public async Task createAccount(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username); //Get user account 
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts
            Random randomNo = new Random();
            int newAccNo = randomNo.Next(1,100);

            //Create new account in db
            BankAccount account = new BankAccount()
            {
                partitionKey = "account",
                username = username,
                accountNo = newAccNo,
                balance = Double.Parse(msg.Text),
                date = DateTime.Now
            };
            await AzureManager.AzureManagerInstance.CreateAccount(account);
            string endOutput = "Owner: " + userAcc[0].Name + " \n\n New Account " + account.accountNo + " successfully created at " + account.date + ".";
            await context.PostAsync(endOutput);
            await context.PostAsync("What else can I do for you?");
            context.Wait(jobs);
        }

        /*Adding balance to the user's bank account */
        public async Task addBalance(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";
            
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

            //Check if user has any accounts
            if (userBankAcc.Count == 0)
            {
                endOutput = "No accounts available";
            }
            //Else get the accounts
            else
            {
                //Adding balance
                if (input.Length == 2)
                {
                    foreach (BankAccount a in userBankAcc)
                    {
                        int accountNo = Int32.Parse(input[0]);
                        double balance = Double.Parse(input[1]);
                        if (accountNo == a.accountNo)
                        {
                            a.balance = a.balance + balance;
                            await AzureManager.AzureManagerInstance.UpdateBalance(a);
                            endOutput = "Balance updated! $" + balance + " added to Account " + a.accountNo + "\n\n New Balance = $" + a.balance;
                            break;
                        }
                        else
                        {
                            endOutput = "Wrong input. Please enter the correct account number and balance (in that order) next time.";
                        }
                    }
                }
                else
                {
                    endOutput = "Wrong input. Please enter the account number and balance amount (in that order) next time.";
                }
            }
            await context.PostAsync(endOutput);
            await context.PostAsync("What else can I do for you?");
            context.Wait(jobs);
        }

        /*Withdrawing balance from the user's bank account */
        public async Task withdrawBalance(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";

            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

            //Check if user has any accounts
            if (userBankAcc.Count == 0)
            {
                endOutput = "No accounts available";
            }
            else
            {
                //Removing balance
                if (input.Length == 2)
                {
                    int accountNo = Int32.Parse(input[0]);
                    double balance = Double.Parse(input[1]);
                    foreach (BankAccount a in userBankAcc)
                    {
                        if (accountNo == a.accountNo)
                        {
                            if (a.balance - balance <= 0)
                            {
                                endOutput = "Not enough balance to remove.";
                            }
                            else
                            {
                                a.balance = a.balance - balance;
                                await AzureManager.AzureManagerInstance.UpdateBalance(a);
                                endOutput = "Balance updated! $" + balance + " withdrawn from Account " + a.accountNo + "\n\n New Balance = $" + a.balance;
                                
                            }
                            break;
                        }
                        else
                        {
                            endOutput = "Wrong input. Please enter the correct account number and balance (in that order) next time.";
                        }
                    }
                }
                else
                {
                   endOutput = "Wrong input. Please enter the account number and balance amount (in that order) next time.";
                }
            }
            await context.PostAsync(endOutput);
            await context.PostAsync("What else can I do for you?");
            context.Wait(jobs);
        }

        /*Deleting user's bank account */
        public async Task deleteAccount(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";

            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

            //Check if user has any accounts
            if (userBankAcc.Count == 0)
            {
                endOutput = "No accounts available";
            }
            else
            {
                //Deleting account
                int accountNo = Int32.Parse(input[0]);
                foreach (BankAccount a in userBankAcc)
                {
                    if (accountNo == a.accountNo)
                    {
                        await AzureManager.AzureManagerInstance.DeleteAccount(a);
                        endOutput = "Deleted: Account " + a.accountNo;
                        break;
                    }
                    else
                    {
                        endOutput = "Wrong input. Please enter the correct account number next time.";
                    }
                }
            }
            await context.PostAsync(endOutput);
            await context.PostAsync("What else can I do for you?");
            context.Wait(jobs);
        }

        /**Check currency rates API call*/
        public async Task checkRate(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";
            
            if (input.Length == 2)
            {
                CurrencyExchange.RootObject rootObject;
                HttpClient client = new HttpClient();
                string baseCurrency = input[0].ToUpper();
                string exchangeCurrency = input[1].ToUpper();

                string result = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + baseCurrency + "&symbols=" + exchangeCurrency));
                rootObject = JsonConvert.DeserializeObject<CurrencyExchange.RootObject>(result);

                PropertyInfo prop = typeof(Rates).GetProperty(exchangeCurrency);

                var value = prop.GetValue(rootObject.rates, null);
                endOutput = "Base currency: " + baseCurrency + "\n\n Exchange rate: " + exchangeCurrency + " " + value.ToString();
                await context.PostAsync(endOutput);
                await context.PostAsync("What else can I do for you?");
                context.Wait(jobs);
            }
            else
            {
                 await context.PostAsync("Wrong input. Please enter the correct base currency and exchange currency (in that order, e.g. NZD MYR).");
                 context.Wait(checkRate);
            }
        }


        /* TODO: 
         *  currency exchange API call
         *  cards
         */

    }
}
