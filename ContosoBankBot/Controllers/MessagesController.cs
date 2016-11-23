using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using ContosoBankBot.DataModels;
using ContosoBankBot.Models;
using static ContosoBankBot.Models.CurrencyExchange;
using System.Reflection;
using Microsoft.Bot.Builder.Dialogs;

namespace ContosoBankBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new ConsotoBankBotDialog());
                /*Initialize objects*/
                /*ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                
                var userMessage = activity.Text;
                string endOutput = "";

                //Greeting
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    endOutput = "Hello again!";
                }
                else
                {
                    endOutput = "Welcome to Consoto Bank's Bot!";
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                //Clear
                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "User data cleared.";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    
                }

                //Get username
                if (userMessage.ToLower().Contains("name"))
                {
                    string name = userMessage.Split()[1];
                    userData.SetProperty<string>("username", name);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    endOutput = "Hello, " + userData.GetProperty<string>("username") + "!";
                }
                
                var input = userMessage.Split();
                string username = userData.GetProperty<string>("username");

                if (username == null)
                {
                    endOutput += "\n\n Please enter a username.";
                }
                else
                {
                    //Create account
                    if (input.Length == 3 && userMessage.ToLower().Contains("create account"))
                    {
                        //Get user's bank accounts
                        List<BankAccount> userBankAcc = await AzureManager.AzureManagerInstance.GetUserAccount(username);
                        int newAccNo = userBankAcc.Count + 1;

                        //Create new account in db
                        BankAccount account = new BankAccount()
                        {
                            owner = username,
                            accountNo = newAccNo,
                            balance = Double.Parse(input[2]),
                            date = DateTime.Now
                        };
                        await AzureManager.AzureManagerInstance.CreateAccount(account);
                        endOutput = "Owner: " + account.owner + " \n\n New Account " + account.accountNo + " created at " + account.date;
                    }

                    //Check account
                    if (userMessage.ToLower().Contains("get account"))
                    {
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
                            endOutput = "Here are your accounts, " + username + ": \n\n";
                            foreach (BankAccount a in userBankAcc)
                            {
                                endOutput += "Account Number: " + a.accountNo + "\n\r Balance: $" + a.balance + "\n\r Account created at: " + a.date + "\n\n";
                            }
                        }
                    }

                    //Add balance
                    if (userMessage.ToLower().Contains("add balance"))
                    {
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
                            if (input.Length == 4)
                            {
                                foreach (BankAccount a in userBankAcc)
                                {
                                    int accountNo = Int32.Parse(input[2]);
                                    double balance = Double.Parse(input[3]);
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
                    }

                    //Withdraw balance
                    if (userMessage.ToLower().Contains("withdraw balance"))
                    {
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
                            if (input.Length == 4)
                            {
                                int accountNo = Int32.Parse(input[2]);
                                double balance = Double.Parse(input[3]);
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
                    }

                    //Delete account
                    if (input.Length == 3 && userMessage.ToLower().Contains("delete account"))
                    {
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
                            int accountNo = Int32.Parse(input[2]);
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
                    }
                }
                
                //Check currency rate API
                if (input.Length == 4 && userMessage.ToLower().Contains("currency rate"))
                {
                    CurrencyExchange.RootObject rootObject;
                    HttpClient client = new HttpClient();
                    string baseCurrency = input[2].ToUpper();
                    string exchangeCurrency = input[3].ToUpper();

                    string result = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + baseCurrency + "&symbols=" + exchangeCurrency));
                    rootObject = JsonConvert.DeserializeObject<CurrencyExchange.RootObject>(result);
                    
                    PropertyInfo prop = typeof(Rates).GetProperty(input[3].ToUpper());
                    
                    var value = prop.GetValue(rootObject.rates, null);
                    endOutput = "Base currency: " + baseCurrency + "\n\n Exchange rate: " + exchangeCurrency + " " + value.ToString();
                }

                if (userMessage.ToLower().Equals("msa"))
                {
                    Activity replyToConversation = activity.CreateReply("MSA information");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://cdn2.iconfinder.com/data/icons/ios-7-style-metro-ui-icons/512/MetroUI_iCloud.png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "heyyyy",
                        Type = "postBack",
                        Title = "MSA Website"
                    };
                    cardButtons.Add(plButton);
                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = "Visit MSA",
                        Subtitle = "The MSA Website is here",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                // return our reply to the user
                Activity reply = activity.CreateReply(endOutput);
                await connector.Conversations.ReplyToActivityAsync(reply);*/
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}