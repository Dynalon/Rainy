using System.Security.Cryptography;
using System.IO;
using Rainy.Db;
using System;
using System.Text;

namespace Rainy.Crypto
{
	public static class CryptoHelper
	{
		public static void CreateCryptoFields (this DBUser db_user, string password)
		{
			var rng = new RNGCryptoServiceProvider ();

			var salt = rng.Create256BitLowerCaseHexKey ();
			db_user.PasswordSalt = salt.Substring (0, 32);
			db_user.MasterKeySalt = salt.Substring (32, 32);

			db_user.PasswordHash = db_user.ComputePasswordHash (password);

			// generate master key - always fix and will sustain password changes
			string master_key = rng.Create256BitLowerCaseHexKey ();
			var pw_key = db_user.DeriveKeyFromPassword (password);

			// now encrypt the cleartext masterkey with the password-derived key
			using (var aes = new AesManaged ()) {
				ICryptoTransform encryptor = aes.CreateEncryptor(pw_key, db_user.MasterKeySalt.ToByteArray ());
				// Create the streams used for encryption. 
				using (MemoryStream msEncrypt = new MemoryStream())
				{
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
						{

							//Write all data to the stream.
							swEncrypt.Write(master_key);
						}
						var encrypted = msEncrypt.ToArray();
						db_user.EncryptedMasterKey = encrypted.ToHexString ();
					}
				}
			}
		}

		public static string ComputePasswordHash (this DBUser db_user, string password)
		{
			SHA256 sha256 = SHA256Managed.Create();
			byte[] password_bytes = new UnicodeEncoding ().GetBytes (db_user.PasswordSalt + ":" + password);
			byte[] hashed = sha256.ComputeHash (password_bytes);
			return hashed.ToHexString ();
		}

		public static byte[] DeriveKeyFromPassword (this DBUser user, string password)
		{
			// the master key is encrypted with a key that is derived from the users password
			var pbkdf2 = new Rfc2898DeriveBytes(password, user.MasterKeySalt.ToByteArray (), 1000);
			byte[] pw_key = pbkdf2.GetBytes (32);
			return pw_key;
		}

		public static byte[] GetPlaintextMasterKey (this DBUser user, string password)
		{
			var pw_key = user.DeriveKeyFromPassword (password);

			var aes = new AesManaged ();
			// Create a decrytor to perform the stream transform.
			ICryptoTransform decryptor = aes.CreateDecryptor (pw_key, user.MasterKeySalt.ToByteArray ());

			// Create the streams used for decryption. 
			string plaintext;
			using (MemoryStream msDecrypt = new MemoryStream (user.EncryptedMasterKey.ToByteArray ()))
			{
				using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
				{
					using (StreamReader srDecrypt = new StreamReader(csDecrypt))
					{
						// Read the decrypted bytes from the decrypting stream 
						// and place them in a string.
						plaintext = srDecrypt.ReadToEnd();
					}
				}
			}
			return plaintext.ToByteArray ();
		}

		public static byte[] EncryptUnicodeString (this DBUser user, string password, string plaintext)
		{
			byte[] encrypted;
			var master_key = user.GetPlaintextMasterKey (password);

			var aes = new AesManaged ();
			aes.Key = master_key;
			aes.IV = user.MasterKeySalt.ToByteArray ();

			ICryptoTransform encryptor = aes.CreateEncryptor (aes.Key, aes.IV);

			using (MemoryStream msEncrypt = new MemoryStream ())
			{
				using (CryptoStream csEncrypt = new CryptoStream (msEncrypt, encryptor, CryptoStreamMode.Write))
				{
					using (StreamWriter swEncrypt = new StreamWriter (csEncrypt))
					{
						//Write all data to the stream.
						swEncrypt.Write (plaintext);
					}
					encrypted = msEncrypt.ToArray ();
				}
			}
			return encrypted;
		}

		public static string DecryptUnicodeString (this DBUser user, string password, byte[] ciphertext)
		{
			string plaintext;
			// Create a decrytor to perform the stream transform.
			var aes = new AesManaged ();
			var master_key = user.GetPlaintextMasterKey (password);

			aes.Key = master_key;
			aes.IV = user.MasterKeySalt.ToByteArray ();

			ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV); 

			// Create the streams used for decryption. 
			using (MemoryStream msDecrypt = new MemoryStream(ciphertext))
			{
				using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
				{
					using (StreamReader srDecrypt = new StreamReader(csDecrypt))
					{

						// Read the decrypted bytes from the decrypting stream 
						// and place them in a string.
						plaintext = srDecrypt.ReadToEnd();
					}
				}
			}
			return plaintext;
		}

		public static string ToHexString (this byte[] bytes)
		{
			return BitConverter.ToString (bytes).Replace ("-", "").ToLower ();
		}

		public static byte[] ToByteArray (this string text)
		{
			int num_chars = text.Length;
			byte[] bytes = new byte[num_chars/2];
			for (int i=0; i < num_chars; i+=2)
				bytes[i/2] = Convert.ToByte(text.Substring(i, 2), 16);
			return bytes;
		}
		public static string Create256BitLowerCaseHexKey(this RNGCryptoServiceProvider crypto)
		{
			byte[] randBytes = new byte[32];
			crypto.GetBytes (randBytes);
			return randBytes.ToHexString ();
		}
	}
}
