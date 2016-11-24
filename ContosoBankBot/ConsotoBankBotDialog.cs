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
            context.Wait(Greeting);
        }

        private async Task Greeting(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument as Activity;
            Activity replyToConversation = message.CreateReply("Welcome to Contoso Bank's Bot!");
            replyToConversation.Recipient = message.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<CardImage> cardImages = new List<CardImage>();
            cardImages.Add(new CardImage(url: "http://4seizoenen-public.sharepoint.com/SiteAssets/logo/Contoso-Blue.png"));

            List<CardAction> cardButtons = new List<CardAction>();
            CardAction cardButton1 = new CardAction()
            {
                Type = "imBack",
                Title = "Enter username",
                Value = "Enter username",
            };
            CardAction cardButton2 = new CardAction()
            {
                Type = "imBack",
                Title = "Create new user account",
                Value = "create new user account"
            };

            cardButtons.Add(cardButton1);
            cardButtons.Add(cardButton2);
            HeroCard plCard = new HeroCard(text: "Please select one of the following", buttons: cardButtons, images: cardImages);
            Attachment plAttachment = plCard.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);
            await context.PostAsync(replyToConversation);
            context.Wait(StartMessage);
        }

        /*Log in or create new user account*/
        public virtual async Task StartMessage(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;

            //Check if user clears
            if (msg.Text.Equals("clear"))
            {
                username = "";
                await context.PostAsync("Cleared!");
                context.Wait(Greeting);
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

            else if (msg.Text.Contains("username"))
            {
                await context.PostAsync("Please enter your username.");
                context.Wait(InputUsername);
            }
            else
            {
                await context.PostAsync("Sorry, didn't get that! Try again?");
                context.Wait(Greeting);
            }
        }

        /*Entering username*/
        public async Task InputUsername(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            username = msg.Text;
            PromptDialog.Confirm(
                context,
                confirmUsername,
                "Is " + username + " your username?",
                "Sorry, didn't get that! Is " + username + " your username?",
                promptStyle: PromptStyle.Auto);
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
                await context.PostAsync("Enter any key to continue.");
                context.Wait(Greeting);
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
                context.Wait(Greeting);
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
                            string endOutput = "Login successful. \n\n Hi, " + a.Name + "! How may I help you? (Enter any key to continue)";
                            await context.PostAsync(endOutput);
                            context.Wait(promptJobs);
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
                await context.PostAsync("Enter any key to continue.");
                context.Wait(Greeting);
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
                await context.PostAsync("Enter any key to continue.");
                context.Wait(Greeting);
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
                context.Wait(Greeting);
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
                        endOutput = "Account created! Hi, " + account.Name + "! How may I help you? (Enter any key to continue)";
                        await context.PostAsync(endOutput);
                        context.Wait(promptJobs);
                    }
                }
            }
        }

        /*List of services card*/
        public async Task promptJobs(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument as Activity;
            Activity replyToConversation = message.CreateReply("Here's the list of available services");
            replyToConversation.Recipient = message.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<CardImage> cardImages = new List<CardImage>();
            cardImages.Add(new CardImage(url: "http://4seizoenen-public.sharepoint.com/SiteAssets/logo/Contoso-Blue.png"));

            List<CardAction> cardButtons = new List<CardAction>();
            CardAction cardButton1 = new CardAction()
            {
                Type = "imBack",
                Title = "Check bank account(s)",
                Value = "Check bank account(s)",
            };
            CardAction cardButton2 = new CardAction()
            {
                Type = "imBack",
                Title = "Create new bank account",
                Value = "Create new bank account"
            };
            CardAction cardButton3 = new CardAction()
            {
                Type = "imBack",
                Title = "Deposit into bank account",
                Value = "Deposit into bank account"
            };
            CardAction cardButton4 = new CardAction()
            {
                Type = "imBack",
                Title = "Withdraw from bank account",
                Value = "Withdraw from bank account"
            };
            CardAction cardButton5 = new CardAction()
            {
                Type = "imBack",
                Title = "Delete bank account",
                Value = "Delete bank account"
            };
            CardAction cardButton6 = new CardAction()
            {
                Type = "imBack",
                Title = "Get currency exchange rates",
                Value = "Get currency exchange rates"
            };

            CardAction cardButton7 = new CardAction()
            {
                Type = "imBack",
                Title = "Log out",
                Value = "Log out"
            };

            CardAction cardButton8 = new CardAction()
            {
                Type = "call",
                Title = "Call bank",
                Value = "tel:0123456789"
            };

            cardButtons.Add(cardButton1);
            cardButtons.Add(cardButton2);
            cardButtons.Add(cardButton3);
            cardButtons.Add(cardButton4);
            cardButtons.Add(cardButton5);
            cardButtons.Add(cardButton6);
            cardButtons.Add(cardButton7);
            cardButtons.Add(cardButton8);

            HeroCard plCard = new HeroCard(text: "Please select one of the following", buttons: cardButtons, images: cardImages);
            Attachment plAttachment = plCard.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);
            await context.PostAsync(replyToConversation);
            context.Wait(jobs);
        }

        /* JOBS! */
        public async Task jobs(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;

            //Check if user clears
            if (msg.Text.Equals("clear"))
            {
                username = "";
                await context.PostAsync("Cleared!");
                context.Wait(Greeting);
            }

            else
            {
                /**Get user's bank accounts*/
                if (msg.Text.ToLower().Contains("check"))
                {
                    List<BankAccount> userAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username); //Get user account 
                    List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts
                    
                    var message = await argument as Activity;
                    Activity replyToConversation = message.CreateReply("");
                    replyToConversation.Recipient = message.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    //Check if user has any accounts -- fail
                    if (userBankAcc.Count == 0)
                    {
                        /*Displayed in thumbnail card*/ 
                        //Image
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: "https://cdn0.iconfinder.com/data/icons/small-n-flat/24/678069-sign-error-128.png"));
                        //Button
                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction cardButton1 = new CardAction()
                        {
                            Type = "imBack",
                            Title = "Continue",
                            Value = "Ok",
                        };
                        cardButtons.Add(cardButton1);
                        ThumbnailCard plCard = new ThumbnailCard()
                        {
                            Title = "No accounts available",
                            Buttons = cardButtons,
                            Images = cardImages
                        };
                        Attachment plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                    }

                    //Else get the bank accounts -- success
                    else
                    {   //Display as receipt cards
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: "http://4seizoenen-public.sharepoint.com/SiteAssets/logo/Contoso-Blue.png"));

                        //Buttons
                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction cardButton1 = new CardAction()
                        {
                            Type = "imBack",
                            Title = "Continue",
                            Value = "Ok",
                        };
                        cardButtons.Add(cardButton1);

                        //Accounts
                        List<ReceiptItem> accounts = new List<ReceiptItem>();
                        foreach (BankAccount a in userBankAcc)
                        {
                            ReceiptItem item = new ReceiptItem()
                            {
                                Title = "Account number: " + a.accountNo,
                                Subtitle = "Owner: " + userAcc[0].Name,
                                Text = "\n Balance: $" + a.balance.ToString() + "\n" + "Created at: " + a.date,
                                Image = new CardImage(url: "https://cdn4.iconfinder.com/data/icons/pretty_office_3/128/sign-up.png")
                            };
                            accounts.Add(item);
                        }

                        ReceiptCard plCard = new ReceiptCard()
                        {
                            Title = "Here are your account(s), " + userAcc[0].Name + ":\n\n",
                            Buttons = cardButtons,
                            Items = accounts,
                        };
                        Attachment plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                    }
                    await context.PostAsync(replyToConversation);
                    context.Wait(promptJobs);
                }

                /**Create new bank account for user*/
                else if (msg.Text.ToLower().Contains("create"))
                {
                    await context.PostAsync("Please enter the amount to be inserted into the new account.");
                    context.Wait(createAccount);
                }

                /**Add balance to the user's bank account */
                else if (msg.Text.ToLower().Contains("deposit"))
                {
                    await context.PostAsync("Please enter the account number and amount to be inserted into the account (in that order).");
                    context.Wait(addBalance);
                }

                /**Withdraw balance from the user's bank account */
                else if (msg.Text.ToLower().Contains("withdraw"))
                {
                    await context.PostAsync("Please enter the account number and amount to be inserted into the account (in that order).");
                    context.Wait(withdrawBalance);
                }

                /**Delete user's bank account */
                else if (msg.Text.ToLower().Contains("delete"))
                {
                    await context.PostAsync("Please enter the account number to be deleted.");
                    context.Wait(deleteAccount);
                }

                /**Check currency rates*/
                else if (msg.Text.ToLower().Contains("rates"))
                    {
                    await context.PostAsync("Please enter the base currency and exchange currency (in that order, e.g. NZD MYR).");
                    context.Wait(checkRate);
                }

                /**Log out from user's account*/
                else if (msg.Text.ToLower().Contains("log out"))
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
                    context.Wait(promptJobs);
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
                context.Wait(Greeting);
            }
            else
            {
                await context.PostAsync("What else can I do for you? Enter any key to continue.");
                context.Wait(promptJobs);
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

            //Display as thumbnail cards
            var message = await argument as Activity;
            Activity replyToConversation = message.CreateReply("");
            replyToConversation.Recipient = message.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<CardImage> cardImages = new List<CardImage>();
            cardImages.Add(new CardImage(url: "https://cdn0.iconfinder.com/data/icons/small-n-flat/24/678134-sign-check-128.png"));

            //Buttons
            List<CardAction> cardButtons = new List<CardAction>();
            CardAction cardButton1 = new CardAction()
            {
                Type = "imBack",
                Title = "Continue",
                Value = "Ok",
            };
            cardButtons.Add(cardButton1);
            ThumbnailCard plCard = new ThumbnailCard(text: "Owner: " + userAcc[0].Name + " \n\n New Account " + account.accountNo + " successfully created at " + account.date + ".", 
                                                    buttons: cardButtons, 
                                                    images: cardImages);
            Attachment plAttachment = plCard.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);
            await context.PostAsync(replyToConversation);
            context.Wait(promptJobs);
        }

        /*Adding balance to the user's bank account */
        public async Task addBalance(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            bool success = false;
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";
            var message = await argument as Activity;
            Activity replyToConversation = message.CreateReply("");
            replyToConversation.Recipient = message.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

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
                        if (accountNo == a.accountNo) //--success
                        {
                            a.balance = a.balance + balance;
                            await AzureManager.AzureManagerInstance.UpdateBalance(a);

                            //Display result as thumbnail cards
                            List<CardImage> cardImages = new List<CardImage>();
                            cardImages.Add(new CardImage(url: "https://cdn0.iconfinder.com/data/icons/small-n-flat/24/678134-sign-check-128.png"));

                            //Buttons
                            List<CardAction> cardButtons = new List<CardAction>();
                            CardAction cardButton1 = new CardAction()
                            {
                                Type = "imBack",
                                Title = "Continue",
                                Value = "Ok",
                            };
                            cardButtons.Add(cardButton1);
                            ThumbnailCard plCard = new ThumbnailCard(text: "Balance updated! $" + balance + " added to Account " + a.accountNo + "\n\n New Balance = $" + a.balance,
                                                                    buttons: cardButtons,
                                                                    images: cardImages);
                            Attachment plAttachment = plCard.ToAttachment();
                            replyToConversation.Attachments.Add(plAttachment);
                            success = true;
                            break;
                        }
                        else //--fail
                        {
                            endOutput = "Wrong input. Please try again.";
                        }
                    }
                }
                else //--fail
                {
                    endOutput = "Wrong input. Please try again.";
                }
            }
            if (!success)
            {
                /*Displayed in thumbnail card*/
                //Image
                List<CardImage> cardImages = new List<CardImage>();
                cardImages.Add(new CardImage(url: "https://cdn0.iconfinder.com/data/icons/small-n-flat/24/678069-sign-error-128.png"));
                //Button
                List<CardAction> cardButtons = new List<CardAction>();
                CardAction cardButton1 = new CardAction()
                {
                    Type = "imBack",
                    Title = "Continue",
                    Value = "Ok",
                };
                cardButtons.Add(cardButton1);
                ThumbnailCard plCard = new ThumbnailCard()
                {
                    Title = endOutput,
                    Buttons = cardButtons,
                    Images = cardImages
                };
                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
            }
            await context.PostAsync(replyToConversation);
            context.Wait(promptJobs);
        }

        /*Withdrawing balance from the user's bank account */
        public async Task withdrawBalance(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            bool success = false;
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";
            var message = await argument as Activity;
            Activity replyToConversation = message.CreateReply("");
            replyToConversation.Recipient = message.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

            //Check if user has any accounts
            if (userBankAcc.Count == 0) //--fail
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
                            if (a.balance - balance <= 0) //--fail
                            {
                                endOutput = "Not enough balance to remove.";
                            }
                            else //--success
                            {
                                a.balance = a.balance - balance;
                                await AzureManager.AzureManagerInstance.UpdateBalance(a);

                                //Display result as thumbnail cards
                                List<CardImage> cardImages = new List<CardImage>();
                                cardImages.Add(new CardImage(url: "https://cdn0.iconfinder.com/data/icons/small-n-flat/24/678134-sign-check-128.png"));

                                //Buttons
                                List<CardAction> cardButtons = new List<CardAction>();
                                CardAction cardButton1 = new CardAction()
                                {
                                    Type = "imBack",
                                    Title = "Continue",
                                    Value = "Ok",
                                };
                                cardButtons.Add(cardButton1);
                                ThumbnailCard plCard = new ThumbnailCard(text: "Balance updated!$" + balance + " withdrawn from Account " + a.accountNo + "\n\n New Balance = $" + a.balance,
                                                                        buttons: cardButtons,
                                                                        images: cardImages);
                                Attachment plAttachment = plCard.ToAttachment();
                                replyToConversation.Attachments.Add(plAttachment);
                                success = true;
                                break;
                            }
                        }
                        else //--fail
                        {
                            endOutput = "Wrong input. Please try again.";
                        }
                    }
                }
                else//--fail
                {
                    endOutput = "Wrong input. Please try again.";
                }
            }
            if (!success)
            {
                /*Displayed in thumbnail card*/
                //Image
                List<CardImage> cardImages = new List<CardImage>();
                cardImages.Add(new CardImage(url: "https://cdn0.iconfinder.com/data/icons/small-n-flat/24/678069-sign-error-128.png"));
                //Button
                List<CardAction> cardButtons = new List<CardAction>();
                CardAction cardButton1 = new CardAction()
                {
                    Type = "imBack",
                    Title = "Continue",
                    Value = "Ok",
                };
                cardButtons.Add(cardButton1);
                ThumbnailCard plCard = new ThumbnailCard()
                {
                    Title = endOutput,
                    Buttons = cardButtons,
                    Images = cardImages
                };
                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
            }
            await context.PostAsync(replyToConversation);
            context.Wait(promptJobs);
        }

        /*Deleting user's bank account */
        public async Task deleteAccount(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            bool success = false;
            var msg = await argument;
            var input = msg.Text.Split();
            string endOutput = "";
            var message = await argument as Activity;
            Activity replyToConversation = message.CreateReply("");
            replyToConversation.Recipient = message.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserBankAccount(username); //Get user's bank accounts

            //Check if user has any accounts
            if (userBankAcc.Count == 0) //--fail
            {
                endOutput = "No accounts available.";
            }
            else 
            {
                //Deleting account
                int accountNo = Int32.Parse(input[0]);
                foreach (BankAccount a in userBankAcc)
                {
                    if (accountNo == a.accountNo) //--success
                    {
                        await AzureManager.AzureManagerInstance.DeleteAccount(a);

                        //Display result as thumbnail cards
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: "https://cdn0.iconfinder.com/data/icons/small-n-flat/24/678134-sign-check-128.png"));

                        //Buttons
                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction cardButton1 = new CardAction()
                        {
                            Type = "imBack",
                            Title = "Continue",
                            Value = "Ok",
                        };
                        cardButtons.Add(cardButton1);
                        ThumbnailCard plCard = new ThumbnailCard(text: "Deleted: Account " + a.accountNo,
                                                                buttons: cardButtons,
                                                                images: cardImages);
                        Attachment plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                        success = true;
                        break;
                    }
                    else //--fail
                    {
                        endOutput = "Wrong input. Please try again.";
                    }
                }
            }
            if (!success)
            {
                /*Displayed in thumbnail card*/
                //Image
                List<CardImage> cardImages = new List<CardImage>();
                cardImages.Add(new CardImage(url: "https://cdn0.iconfinder.com/data/icons/small-n-flat/24/678069-sign-error-128.png"));
                //Button
                List<CardAction> cardButtons = new List<CardAction>();
                CardAction cardButton1 = new CardAction()
                {
                    Type = "imBack",
                    Title = "Continue",
                    Value = "Ok",
                };
                cardButtons.Add(cardButton1);
                ThumbnailCard plCard = new ThumbnailCard()
                {
                    Title = endOutput,
                    Buttons = cardButtons,
                    Images = cardImages
                };
                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
            }
            await context.PostAsync(replyToConversation);
            context.Wait(promptJobs);
        }

        /**Check currency rates API call*/
        public async Task checkRate(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await argument;
            var input = msg.Text.Split();
            
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

                //Display result as thumbnail cards
                var message = await argument as Activity;
                Activity replyToConversation = message.CreateReply("");
                replyToConversation.Recipient = message.From;
                replyToConversation.Type = "message";
                replyToConversation.Attachments = new List<Attachment>();
                List<CardImage> cardImages = new List<CardImage>();
                cardImages.Add(new CardImage(url: "https://cdn2.iconfinder.com/data/icons/banking-icons-vector/595/Banking_00016_A-128.png"));

                //Buttons
                List<CardAction> cardButtons = new List<CardAction>();
                CardAction cardButton1 = new CardAction()
                {
                    Type = "imBack",
                    Title = "Continue",
                    Value = "Ok",
                };
                cardButtons.Add(cardButton1);
                ThumbnailCard plCard = new ThumbnailCard(text: "Base currency: " + baseCurrency + "\n\n, Exchange rate: " + exchangeCurrency + " " + value.ToString(),
                                                        buttons: cardButtons,
                                                        images: cardImages);
                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
                await context.PostAsync(replyToConversation);
                context.Wait(promptJobs);
            }
            else
            {
                 await context.PostAsync("Wrong input. Please enter the correct base currency and exchange currency (in that order, e.g. NZD MYR).");
                 context.Wait(checkRate);
            }
        }
    }
}
