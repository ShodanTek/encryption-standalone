using EncryptionStandalone.EncryptionHelpers;
using MongoDB.Driver.Encryption;
using System;
using System.Collections.Generic;

namespace EncryptionStandalone
{
    class DebugConfigs
    {
        public static DatabaseConfiguration GetDatabaseConfiguration()
        {
            DatabaseConfiguration dbConfig = new DatabaseConfiguration();
            dbConfig.ConnectionUrl = "";
            dbConfig.DatabaseName = "App";
            dbConfig.Username = "";
            dbConfig.Password = "";
            return dbConfig;
        }

        public static EncryptOptions GetEncryptOptions(Guid dataKeyId)
        {
            return new EncryptOptions(
                algorithm: EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                keyId: dataKeyId
            );
        }

        public static List<ConfigurationItem> GetKmsOptions()
        {
            var kmsOptions = new List<ConfigurationItem>();
            
            kmsOptions.Add(new ConfigurationItem("privateKey", ConfigurationItemType.Base64StringToByteArray, ""));
            kmsOptions.Add(new ConfigurationItem("email", ConfigurationItemType.String, ""));

            return kmsOptions;
        }

        public static List<ConfigurationItem> GetDataKeyOptions()
        {
            var datakeyOptions = new List<ConfigurationItem>();

            datakeyOptions.Add(new ConfigurationItem("projectId", ConfigurationItemType.String, ""));
            datakeyOptions.Add(new ConfigurationItem("keyName", ConfigurationItemType.String, ""));
            datakeyOptions.Add(new ConfigurationItem("keyRing", ConfigurationItemType.String, ""));
            datakeyOptions.Add(new ConfigurationItem("location", ConfigurationItemType.String, ""));

            return datakeyOptions;
        }

        public static DatabaseConfiguration GetDatabaseEncryptionConfiguration()
        {
            DatabaseConfiguration dbConfig = new DatabaseConfiguration();
            dbConfig.ConnectionUrl = "";
            dbConfig.DatabaseName = "EncVault";
            dbConfig.Username = "";
            dbConfig.Password = "";
            dbConfig.KmsProvider = "gcp";

            dbConfig.KmsOptions = GetKmsOptions();
            dbConfig.DataKeyOptions = GetDataKeyOptions();

            return dbConfig;
        }
    }
}
