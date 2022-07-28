using MongoDB.Driver;
using System.Collections.Generic;

namespace EncryptionStandalone.EncryptionHelpers
{
    public class DatabaseConfiguration
    {
        public string? ConnectionUrl { get; set; }
        public string? DatabaseName { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? KmsProvider { get; set; }
        public IEnumerable<ConfigurationItem>? KmsOptions { get; set; }
        public IEnumerable<ConfigurationItem>? DataKeyOptions { get; set; }        
    }
}
