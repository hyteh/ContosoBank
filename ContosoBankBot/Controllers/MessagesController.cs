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
                //Initialize objects
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                var userMessage = activity.Text;
                string endOutput = "Hello, welcome to Contoso Bank's Bot!";

                //Say hi to start bot
                if (userData.GetProperty<bool>("FirstTime"))
                {
                    endOutput = "Hello again!";
                }

                else
                {
                    userData.SetProperty<bool>("FirstTime", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "User data cleared";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    
                }
                
                //Check accounts
                if (userMessage.ToLower().Equals("get account"))
                {
                    List<BankAccount> bankAccount = await AzureManager.AzureManagerInstance.GetAccount();
                    bool isEmpty = bankAccount.Count == 0;

                    //Check if db is empty
                    if (isEmpty)
                    {
                        endOutput = "No accounts available.";
                    }
                    //Else search if db has same account name
                    else
                    {
                        foreach (BankAccount a in bankAccount)
                        {
                            endOutput = "ID: " + a.ID + "\n\n Owner name: " + a.accountName + "\n\n Balance: " + a.balance + "\n\n Account created at: " + a.date + "\n\n";
                        }
                    }
                   
                }

                //Create account
                var input = userMessage.Split();
                if (input.Length == 3 && userMessage.ToLower().Contains("create account"))
                {
                    List<BankAccount> bankAccount = await AzureManager.AzureManagerInstance.GetAccount();
                    bool isEmpty = bankAccount.Count==0;
                    //Check if db is empty
                    if (isEmpty){
                        BankAccount account = new BankAccount()
                        {
                            accountName = input[2].Trim(),
                            date = DateTime.Now
                        };
                        await AzureManager.AzureManagerInstance.CreateAccount(account);
                        endOutput = "New account created at " + account.date;
                    }
                    //Else search if db has same account name
                    else
                    {
                        foreach (BankAccount a in bankAccount)
                        {
                            if (a.accountName == input[2].Trim())
                            {
                                endOutput = "Account already exists.";
                            }
                            else
                            {
                                BankAccount account = new BankAccount()
                                {
                                    accountName = input[2].Trim(),
                                    date = DateTime.Now
                                };
                                await AzureManager.AzureManagerInstance.CreateAccount(account);
                                endOutput = "New account created at " + account.date;
                            }
                        }
                    }
                }

                //Add balance
                if (userMessage.ToLower().Contains("add balance"))
                {
                    if (input.Length == 4)
                    {
                        List<BankAccount> bankAccount = await AzureManager.AzureManagerInstance.GetAccount();
                        foreach (BankAccount a in bankAccount)
                        {
                            if (input[2].Trim().ToLower() == a.accountName.ToLower()){
                                a.balance = a.balance + Int32.Parse(input[3]);
                                await AzureManager.AzureManagerInstance.UpdateBalance(a);
                                endOutput = "Balance updated! \n\n New Balance = " + a.balance;
                            }
                        }
                    }
                    else
                    {
                        endOutput = "Please insert account name and balance amount.";
                    }
                }


                //Subtract balance
                if (userMessage.ToLower().Contains("remove balance"))
                {
                    if (input.Length == 4)
                    {
                        List<BankAccount> bankAccount = await AzureManager.AzureManagerInstance.GetAccount();
                        foreach (BankAccount a in bankAccount)
                        {
                            if (input[2].Trim().ToLower() == a.accountName.ToLower())
                            {
                                if (a.balance - Int32.Parse(input[3]) <= 0){
                                    endOutput = "Not enough balance to remove.";
                                }
                                else
                                {
                                    a.balance = a.balance - Int32.Parse(input[3]);
                                    await AzureManager.AzureManagerInstance.UpdateBalance(a);
                                    endOutput = "Balance updated! \n\n New Balance = " + a.balance;
                                }
                            }
                        }
                    }
                    else
                    {
                        endOutput = "Please insert the correct account name and balance amount.";
                    }
                }

                //Check currency rate or stock API

                // return our reply to the user
                Activity reply = activity.CreateReply(endOutput);
                await connector.Conversations.ReplyToActivityAsync(reply);
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