using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using ContosoBankBot.DataModels;

namespace ContosoBankBot
{
    [Serializable]
    public class ConsotoBankBotDialog : IDialog<object>
    {
        string username = "";
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            await context.PostAsync("Welcome to Contoso Bank's Bot! Please enter your username or create a new user account.");
            context.Wait(InputUsername);
        }

        public virtual async Task InputUsername(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            if (msg.Text.Contains("create new user account"))
            {
                string endOutput = "Please enter username, password and name to be created (in that order).";
                await context.PostAsync(endOutput);
                context.Wait(createNewAccount);
            }
            else
            {
                username = msg.Text;
                PromptDialog.Confirm(
                    context,
                    confirmUsername,
                    "Is " + username + " your username?",
                    "Didn't get that!",
                    promptStyle: PromptStyle.Auto);
            }
        }

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

        public virtual async Task InputPassword(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            string password = msg.Text;
            List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);

            //Check if user has any accounts
            if (userAcc.Count == 0)
            {
                PromptDialog.Confirm(
                    context,
                    checkNewAccount,
                    "Invalid username. Create new user account?",
                    "Didn't get that!",
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
                        "Wrong password. Change username?",
                        "Didn't get that!",
                        promptStyle: PromptStyle.Auto);
                    }
                }
            }
        }

        public async Task checkNewAccount(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                string endOutput = "Please enter username, password and name to be created (in that order).";
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

        public async Task checkWrongPassword(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                username = "";
                await context.PostAsync("Please enter your username or create new user account.");
                context.Wait(InputUsername);
            }
            else
            {
                await context.PostAsync("Please enter your password.");
                context.Wait(InputPassword);
            }
        }

        public virtual async Task createNewAccount(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";
            username = input[0];
            string password = input[1];
            string name = "";
            //Get name
            for(int i = 2; i<input.Length; i++)
            {
                name += input[i];
            }
            List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);

            //Check if user has any accounts
            if (userAcc.Count != 0) //Username already exist
            {
                endOutput = "Username already exists. Please enter another one";
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

        public async Task jobs(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            if (msg.Text == "get accounts")
            {
                List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username); //Get user account 
                List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

                //Check if user has any accounts
                if (userBankAcc.Count == 0)
                {
                    await context.PostAsync("No accounts available. What else can I do for you?");
                }
                //Else get the accounts
                else
                {
                    string endOutput = "Here are your accounts, " + userAcc[0].Name + ": \n\n";
                    foreach (BankAccount a in userBankAcc)
                    {
                        endOutput += "Account Number: " + a.accountNo + "\n\r Balance: $" + a.balance + "\n\r Account created at: " + a.date + "\n\n";
                    }
                    await context.PostAsync(endOutput);
                }
                context.Wait(jobs);
            }
            if (msg.Text == "create account")
            {
                await context.PostAsync("Please enter the amount to insert into new account.");
                context.Wait(createAccount);
            }

            if (msg.Text == "add balance")
            {
                await context.PostAsync("Please enter the account number and amount to insert into the account.");
                context.Wait(addBalance);
            }
            if (msg.Text == "withdraw balance")
            {
                await context.PostAsync("Please enter the account number and amount to insert into the account.");
                context.Wait(withdrawBalance);
            }
           
            if (msg.Text == "delete account")
            {
                await context.PostAsync("Please enter the account number to delete.");
                context.Wait(deleteAccount);
            }

        }

        public async Task createAccount(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username); //Get user account 
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts
            int newAccNo = userBankAcc.Count + 1;

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
            context.Wait(jobs);
        }

        public async Task addBalance(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";
            
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

            //Check if user has any accounts
            if (userBankAcc.Count == 0)
            {
                endOutput = "No accounts available. What else can I do for you?";
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
                            endOutput = "Please insert the correct account number";
                        }
                    }
                }
                else
                {
                    endOutput = "Please insert the account number and balance amount.";
                }
            }
            await context.PostAsync(endOutput);
            context.Wait(jobs);
        }

        public async Task withdrawBalance(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";

            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

            //Check if user has any accounts
            if (userBankAcc.Count == 0)
            {
                endOutput = "No accounts available.";
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
                            endOutput = "Please insert the correct account number.";
                        }
                    }
                }
                else
                {
                    endOutput = "Please insert the account number and balance amount.";
                }
            }
            await context.PostAsync(endOutput);
            context.Wait(jobs);
        }

        public async Task deleteAccount(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";

            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

            //Check if user has any accounts
            if (userBankAcc.Count == 0)
            {
                endOutput = "No accounts available.";
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
                        endOutput = "Please insert the correct account number.";
                    }
                }
            }
            await context.PostAsync(endOutput);
            context.Wait(jobs);
        }

        /* TODO: 
         *  clear/reset/logout
         *  currency exchange API call
         *  cards
         */

    }
}
