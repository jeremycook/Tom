using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Tom.Helpers
{
    /// <summary>
    /// Symmetric data encryption and decryption using <see cref="AesCryptoServiceProvider"/>.
    /// </summary>
    public class SymmetricEncryption
    {
        /// <summary>
        /// Keys can be 16, 24, or 32 bytes long.
        /// </summary>
        public static readonly int[] ValidKeyLengths = new[] { 16, 24, 32 };

        public enum KeyLength
        {
            K128 = 16,
            K192 = 24,
            K256 = 32,
        }

        /// <summary>
        /// Create a key that can be passed into the constructor, and used for
        /// encrypting and decrypting data.
        /// </summary>
        /// <param name="keyLength"></param>
        /// <returns></returns>
        public static byte[] CreateKey(KeyLength keyLength = KeyLength.K128)
        {
            using (var aes = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[(int)keyLength];
                aes.GetBytes(bytes);
                return bytes;
            }
        }

        private readonly byte[] Key;

        /// <summary>
        /// Constructs a symmetric data encryptor with <paramref name="key"/>
        /// in the format "0 255 0 ...".
        /// </summary>
        /// <param name="key">
        /// Expected format is "0 255 0 ..."
        /// </param>
        public SymmetricEncryption(string key)
            : this(key.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(k => Convert.ToByte(k)).ToArray())
        { }

        /// <summary>
        /// Constructs a symmetric data encryptor with <paramref name="key"/>.
        /// </summary>
        /// <param name="key"></param>
        public SymmetricEncryption(byte[] key)
        {
            if (!ValidKeyLengths.Contains(key.Length))
            {
                throw new ArgumentException("The key byte array must contain 16, 24, or 32 bytes.", "key");
            }

            Key = key;
        }

        /// <summary>
        /// Decrypts <paramref name="encryptedData"/>.
        /// </summary>
        /// <param name="encryptedData">
        /// The first byte is the length of the IV. The next bytes are the IV.
        /// All subsequent bytes are the encrypted data.
        /// </param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] encryptedData)
        {
            int ivLength = (int)encryptedData.First();
            byte[] iv = encryptedData.Skip(1).Take(ivLength).ToArray();
            byte[] encrypted = encryptedData.Skip(1 + ivLength).ToArray();

            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = Key;
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encrypted, 0, encrypted.Length);
                    cs.FlushFinalBlock();

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Encrypts <paramref name="clearData"/> with a unique IV.
        /// </summary>
        /// <param name="clearData"></param>
        /// <returns>
        /// The first byte is the length of the IV. The next bytes are the IV.
        /// All subsequent bytes are the encrypted data.
        /// </returns>
        public byte[] Encrypt(byte[] clearData)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = Key;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(clearData, 0, clearData.Length);
                    cs.FlushFinalBlock();

                    byte[] result = new byte[1 + aes.IV.Length + ms.Length];
                    Array.Copy(new[] { (byte)aes.IV.Length }, result, 1);
                    Array.Copy(aes.IV, 0, result, 1, aes.IV.Length);
                    Array.Copy(ms.ToArray(), 0, result, 1 + aes.IV.Length, ms.Length);

                    return result;
                }
            }
        }
    }
}
