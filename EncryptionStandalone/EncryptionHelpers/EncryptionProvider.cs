using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EncryptionStandalone.EncryptionHelpers
{
    public class EncryptionProvider
    {
        private const string KeyVaultCollectionName = "KVault";

        public Guid DataKeyId { get; }
        public string? KmsProvider { get; set; }
        public IEnumerable<ConfigurationItem>? KmsOptions { get; set; }
        public IEnumerable<ConfigurationItem>? DataKeyOptions { get; set; }


        public EncryptionProvider(DatabaseConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var vaultDbClient = MongoDbProvider.CreateClient(configuration);
            var Client = GetClientEncryption(configuration, vaultDbClient);
            DataKeyId = GetDataKey(configuration, vaultDbClient, Client);
        }

        public static Dictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders(DatabaseConfiguration configuration)
        {
            try
            {
                var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
                var options = new Dictionary<string, object>();
                if (configuration.KmsOptions != null)
                {
                    foreach (var kmsOption in configuration.KmsOptions)
                    {
                        var key = kmsOption.Key ?? throw new InvalidOperationException($"'{nameof(kmsOption.Key)}' is expected.");
                        var value = kmsOption.GetTypedValue() ?? throw new InvalidOperationException($"'{nameof(kmsOption.Value)}' is expected.");
                        options.Add(key, value);
                    }
                }
                kmsProviders.Add(configuration.KmsProvider ?? throw new InvalidOperationException($"'{nameof(configuration.KmsProvider)}' is expected."), options);
                return kmsProviders;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to read '{nameof(configuration.KmsOptions)}' from configuration.", e);
            }
        }

        public static ClientEncryption GetClientEncryption(
            DatabaseConfiguration configuration,
            IMongoClient vaultDbClient)
        {
            var kmsProviders = GetKmsProviders(configuration);
            var clientEncryptionOptions = new ClientEncryptionOptions(
              keyVaultClient: vaultDbClient,
              keyVaultNamespace: new CollectionNamespace(configuration.DatabaseName, KeyVaultCollectionName),
              kmsProviders: kmsProviders
            );
            return new ClientEncryption(clientEncryptionOptions);
        }

        public static DataKeyOptions GetDataKeyOptions(DatabaseConfiguration configuration)
        {
            Optional<BsonDocument> masterKey = default;
            if (configuration.DataKeyOptions != null)
            {
                var options = configuration.DataKeyOptions
                    .Select(o =>
                        new KeyValuePair<string, object>(
                            key: o.Key ?? throw new InvalidOperationException($"'{nameof(o.Key)}' is expected."),
                            value: o.GetTypedValue() ?? throw new InvalidOperationException($"'{nameof(o.Value)}' is expected.")
                        ))
                    .ToList();
                masterKey = new BsonDocument(options);
            }
            return new DataKeyOptions(masterKey: masterKey);
        }

        public static Guid GetDataKey(DatabaseConfiguration configuration,
            IMongoClient vaultDbClient, ClientEncryption clientEncryption)
        {
            var vaultDatabase = vaultDbClient.GetDatabase(configuration.DatabaseName);
            var keyVaultCollection = vaultDatabase.GetCollection<BsonDocument>(KeyVaultCollectionName);
            var kmsProvider = configuration.KmsProvider ?? throw new InvalidOperationException($"'{nameof(configuration.KmsProvider)}' is expected.");
            var dataKeyDocument = keyVaultCollection.Find($"{{\"masterKey.provider\": \"{kmsProvider}\"}}").SingleOrDefault();
            var dataKey = dataKeyDocument?.GetValue("_id")?.AsNullableGuid;
            if (dataKey == null)
            {
                var dataKeyOptions = GetDataKeyOptions(configuration);
                dataKey = clientEncryption.CreateDataKey(kmsProvider, dataKeyOptions, CancellationToken.None);
            }
            return dataKey ?? throw new InvalidOperationException("Should not be null.");
        }

        public void RewrapKeys(DatabaseConfiguration configuration,
            IMongoClient client, ClientEncryption clientEncryption)
        {
            var keyvaultNamespace = CollectionNamespace.FromFullName("EncVault.KVault");
            var kmsProviderName = configuration.KmsProvider;

            var keyVaultCollection = client.GetDatabase(keyvaultNamespace.DatabaseNamespace.DatabaseName).GetCollection<BsonDocument>(keyvaultNamespace.CollectionName);
            var dataKeyDocument = keyVaultCollection.Find($"{{\"masterKey.provider\": \"{kmsProviderName}\"}}").SingleOrDefault();
            Console.WriteLine(dataKeyDocument?.GetValue("_id")?.AsNullableGuid);

            // rewrap
            FilterDefinition<BsonDocument> filter = dataKeyDocument;
            var dataKeyOptions = GetDataKeyOptions(configuration);

            var rewrapManyDataKeyOptions = new RewrapManyDataKeyOptions(kmsProviderName, dataKeyOptions.MasterKey);
            RewrapManyDataKeyResult result = clientEncryption.RewrapManyDataKey(filter, rewrapManyDataKeyOptions);
            Console.WriteLine(result);
        }
    }

}
