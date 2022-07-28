using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncryptionStandalone.EncryptionHelpers
{
    public static class MongoExtensions
    {
        public static MongoClientSettings CreateMongoClientSettings(this DatabaseConfiguration configuration)
        {
            var mongoUrl = new MongoUrl(configuration.ConnectionUrl);
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
            if (configuration.Username != null && configuration.Password != null)
            {
                mongoClientSettings.Credential = MongoCredential.CreateCredential("admin", configuration.Username, configuration.Password);
            }
            return mongoClientSettings;
        }
    }
}
