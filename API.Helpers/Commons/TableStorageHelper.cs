using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;

namespace API.Helpers.Commons
{
    internal sealed class TableStorageHelper
    {
        private static readonly string IntegracionesLogsStorageAccount = "DefaultEndpointsProtocol=https;AccountName=integracioneslogs;AccountKey=Ee/JA11SkrRClw/c5npVbdRWToU8cinPHXrooGsx3RhIK9gs84mLTS2orz1+ullABd7dfIlXaU6rRYVvg/KiPw==;EndpointSuffix=core.windows.net;";

        private static readonly CloudTableClient IntegracionesLogsStorageAccountClient = null;

        private readonly CloudTableClient Client = null;

        static TableStorageHelper()
        {
            if (!string.IsNullOrWhiteSpace(IntegracionesLogsStorageAccount))
            {
                IntegracionesLogsStorageAccountClient = CloudStorageAccount.Parse(IntegracionesLogsStorageAccount).CreateCloudTableClient();
            }
        }

        public TableStorageHelper(Storage storage)
        {
            switch (storage)
            {
                case Storage.IntegracionesLogs:
                    Client = IntegracionesLogsStorageAccountClient;
                    break;
            }
        }

        public void Upsert(TableEntity entity, string tableName)
        {
            if (entity == null)
            {
                throw new Exception("entity must not be null or empty");
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new Exception("tableName must not be null or empty");
            }

            CloudTable table = Client.GetTableReference(tableName);

            if (!table.Exists())
            {
                table.CreateIfNotExistsAsync();
            }

            TableOperation insertOperation = TableOperation.InsertOrMerge(entity);

            table.Execute(insertOperation);

        }

        public List<T> GetData<T>(string tableName, string partitionKey) where T : ITableEntity, new()
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new Exception("partitionKey must not be null or empty");
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new Exception("tableName must not be null or empty");
            }

            CloudTable table = Client.GetTableReference(tableName);

            if (!table.Exists())
            {
                table.CreateIfNotExists();
            }

            var condition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);

            var query = new TableQuery<T>().Where(condition);

            return table.ExecuteQuery(query).ToList();
        }

        public T GetData<T>(string tableName, string partitionKey, string rowKey) where T : ITableEntity
        {
            if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
            {
                throw new Exception("PartitionKey or RowKey must not be null or empty");
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new Exception("tableName must not be null or empty");
            }

            CloudTable table = Client.GetTableReference(tableName);

            if (!table.Exists())
            {
                table.CreateIfNotExists();
            }

            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            TableResult retrievedResult = table.Execute(retrieveOperation);

            return (T)retrievedResult.Result;
        }
    }
    internal enum Storage
    {
        IntegracionesLogs
    }
}