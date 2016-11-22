using ContosoBankBot.DataModels;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ContosoBankBot
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<BankAccount> bankAccountTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("https://msacontosobank.azurewebsites.net");
            this.bankAccountTable = this.client.GetTable<BankAccount>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task CreateAccount(BankAccount account)
        {
            await this.bankAccountTable.InsertAsync(account);
        }

        public async Task<List<BankAccount>> GetAccount()
        {
            return await this.bankAccountTable.ToListAsync();
        }
      
        public async Task<List<BankAccount>> GetUserAccount(string username)
        {
            return await this.bankAccountTable
                .Where(BankAccount => BankAccount.owner == username)
                .ToListAsync();
        }
        public async Task UpdateBalance(BankAccount account)
        {
            await this.bankAccountTable.UpdateAsync(account);
        }

        public async Task DeleteAccount(BankAccount account)
        {
            await this.bankAccountTable.DeleteAsync(account);
        }
    }
}