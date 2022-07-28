using MongoDB.Driver;
using System;

namespace EncryptionStandalone.EncryptionHelpers
{
    public class MongoDbProvider
    {
        public IMongoDatabase Database { get; }

        public MongoDbProvider(DatabaseConfiguration configuration)
        {
            try
            {
                if (configuration.DatabaseName == null)
                    throw new InvalidOperationException($"{nameof(configuration.DatabaseName)} missing.");
                var client = CreateClient(configuration);
                Database = client.GetDatabase(configuration.DatabaseName);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to instanciate the Mongo database provider.", e);
            }
        }

        public static IMongoClient CreateClient(DatabaseConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (configuration.ConnectionUrl == null)
                throw new InvalidOperationException($"{nameof(configuration.ConnectionUrl)} missing.");

            var mongoClientSettings = configuration.CreateMongoClientSettings();


            return new MongoClient(mongoClientSettings);
        }
    }
}
