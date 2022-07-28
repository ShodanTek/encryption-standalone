using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncryptionStandalone.EncryptionHelpers
{
    public class ConfigurationItem
    {
        public string? Key { get; set; }
        public ConfigurationItemType? Type { get; set; }
        public string? Value { get; set; }

        public ConfigurationItem(string key, ConfigurationItemType type, string value)
        {
            Key = key;
            Type = type;
            Value = value;
        }

        public object? GetTypedValue()
        {
            return Type switch
            {
                ConfigurationItemType.String => Value,
                ConfigurationItemType.Base64StringToByteArray => Value != null ? Convert.FromBase64String(Value) : null,
                _ => throw new InvalidOperationException($"Unsupported configuration item type '{Type}'.")
            };
        }
    }
}
