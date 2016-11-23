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
            await context.PostAsync("Welcome to Contoso Bank's Bot! Please enter your name.");
            context.Wait(InputUsername);
        }

        public virtual async Task InputUsername(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            username = msg.Text;
            PromptDialog.Confirm(
                    context,
                    confirmUsername,
                    "Is " + username + " your name?",
                    "Didn't get that!",
                    promptStyle: PromptStyle.Auto);
        }
        public async Task confirmUsername(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                string endOutput = "Hi, " + username + "! How may I help you?";
                await context.PostAsync(endOutput);
                context.Wait(jobs);
            }
            else
            {
                username = "";
                await context.PostAsync("Please enter your name.");
                context.Wait(InputUsername);
            }

        }

        public async Task jobs(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            if (msg.Text == "get accounts")
            {
                List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);

                //Check if user has any accounts
                if (userBankAcc.Count == 0)
                {
                    await context.PostAsync("No accounts available.");
                }
                //Else get the accounts
                else
                {
                    string endOutput = "Here are your accounts, " + username + ": \n\n";
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
            //Get user's bank accounts
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);
            int newAccNo = userBankAcc.Count + 1;

            //Create new account in db
            BankAccount account = new BankAccount()
            {
                owner = username,
                accountNo = newAccNo,
                balance = Double.Parse(msg.Text),
                date = DateTime.Now
            };
            await AzureManager.AzureManagerInstance.CreateAccount(account);
            string endOutput = "Owner: " + account.owner + " \n\n New Account " + account.accountNo + " created at " + account.date;
            await context.PostAsync(endOutput);
            context.Wait(jobs);
        }

        public async Task addBalance(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";

            //Get user's bank accounts
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);

            //Check if user has any accounts
            if (userBankAcc.Count == 0)
            {
                endOutput = "No accounts available.";
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

            //Get user's bank accounts
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);

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

            //Get user's bank accounts
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);

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
