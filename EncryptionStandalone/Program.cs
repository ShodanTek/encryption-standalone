using System;
using System.IO;
using System.Text;
using System.Threading;
using EncryptionStandalone.EncryptionHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.GridFS;

namespace EncryptionStandalone
{
    class Program
    {
        static void Main(string[] args)
        {
            TestEncryptedFileData(false);
            Console.WriteLine("########################################################");
            TestEncryptedFileData(true);
        }

        private static ObjectId TestEncryptedFileData(bool shouldTriggerFail)
        {
            //---Init block---
            var databaseConfiguration = DebugConfigs.GetDatabaseConfiguration();
            var databaseEncryptionConfiguration = DebugConfigs.GetDatabaseEncryptionConfiguration();
            var mongoClient = MongoDbProvider.CreateClient(databaseConfiguration);
            EncryptionProvider encryptionProvider = new EncryptionProvider(databaseEncryptionConfiguration);
            var database = mongoClient.GetDatabase("App");
            var encryptOptions = DebugConfigs.GetEncryptOptions(encryptionProvider.DataKeyId);

            var vaultClient = MongoDbProvider.CreateClient(databaseEncryptionConfiguration);
            var clientEncryption = EncryptionProvider.GetClientEncryption(databaseEncryptionConfiguration, vaultClient);
            var testString = "123456789";
            var bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = "settingsenc",
                ChunkSizeBytes = 128, // 1MB
            });
            bucket.Drop();
            //---Init block---

            var encryptedTestString = clientEncryption.Encrypt(testString, encryptOptions, CancellationToken.None);
            var fileId = InsertEncFile(bucket, encryptedTestString);

            if (shouldTriggerFail)
            {
                Console.WriteLine("Initializing a new client encryption that won't be used to encrypt/decrypt before rewrap");
                clientEncryption = EncryptionProvider.GetClientEncryption(databaseEncryptionConfiguration, vaultClient);
            }

            encryptionProvider.RewrapKeys(databaseEncryptionConfiguration, vaultClient, clientEncryption);
                        
            var downloadedString = DownloadEncFile(bucket, fileId, clientEncryption);
            Console.WriteLine("Asserting decrypted string is equal to original after rewrap");
            Assert.AreEqual(testString, downloadedString);
            Console.WriteLine("Assertion done for rewrapped key decrypted string");
            return fileId;
        }

        private static ObjectId InsertEncFile(IGridFSBucket bucket, BsonBinaryData testData)
        {
            var uploadStream = bucket.OpenUploadStream("test1.txt");
            MemoryStream ms = new MemoryStream(testData.Bytes);
            ms.CopyTo(uploadStream);
            uploadStream.Close();

            Console.WriteLine("Uploaded test string");
            return uploadStream.Id;
        }

        private static string DownloadEncFile(IGridFSBucket bucket, ObjectId fileId, ClientEncryption clientEncryption)
        {
            var options = new GridFSDownloadOptions
            {
                Seekable = true
            };
            var fileStream = bucket.OpenDownloadStream(fileId, options);

            using var reader = new BinaryReader(fileStream);
            var encryptedData = new BsonBinaryData(reader.ReadBytes((int)fileStream.Length), BsonBinarySubType.Encrypted);
            var data = clientEncryption.Decrypt(encryptedData, CancellationToken.None);
            string decodedString;

            if (data.IsBsonBinaryData)
            {
                //For Mongo 2.17.0
                byte[] decryptedBuffer = data.AsBsonBinaryData.Bytes;
                decodedString = Encoding.ASCII.GetString(decryptedBuffer, 0, (int)decryptedBuffer.Length);
            }
            else
            {   //For Mongo 2.13.2
                decodedString = data.AsString;
            }

            Console.WriteLine("Downloaded and decoded string");
            Console.WriteLine(decodedString);
            return decodedString;
        }
    }
}