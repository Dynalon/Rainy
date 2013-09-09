using System.Security.Cryptography;
using System.IO;
using Rainy.Db;
using System;

namespace Rainy.Crypto
{
	public static class CryptoHelperNote
	{
		public static void Decrypt (this DBNote note, DBUser user, string master_key)
		{
			var per_note_key = note.EncryptedKey.DecryptWithKey (master_key, user.MasterKeySalt);
			byte[] b_key = per_note_key.ToByteArray ();
			byte[] b_note_text = note.Text.ToByteArray ();

			note.Text = user.DecryptUnicodeString (b_key, b_note_text);
		}
	}
	public static class CryptoHelperDBUser
	{
		public static bool UpdatePassword (this DBUser db_user, string password)
		{
			if (string.IsNullOrEmpty (db_user.PasswordSalt))
				throw new ArgumentException("Salt must be set", "db_user");

			// TODO update required keys?
			var hash = db_user.ComputePasswordHash (password);
			if (hash != db_user.PasswordHash) {
				db_user.PasswordHash = hash;
				return true;
			}
			// same password, do nothing
			return false;

		}
		public static void CreateCryptoFields (this DBUser db_user, string password)
		{
			if (string.IsNullOrEmpty (password))
				throw new ArgumentNullException ("password");

			var rng = new RNGCryptoServiceProvider ();

			var salt = rng.Create256BitLowerCaseHexKey ();
			db_user.PasswordSalt = salt.Substring (0, 32);
			db_user.MasterKeySalt = salt.Substring (32, 32);

			db_user.UpdatePassword (password);

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
			var pbkdf2 = new Rfc2898DeriveBytes(password, db_user.PasswordSalt.ToByteArray (), 1000);
			byte[] hash_bytes = pbkdf2.GetBytes (32);
			return hash_bytes.ToHexString ();
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
		

		public static byte[] EncryptString (this DBUser user, byte[] key, string plaintext)
		{
			byte[] encrypted;
			// TODO pass in string as argument instead of byte
			string hexkey = key.ToHexString ();
			string iv = user.MasterKeySalt;

			encrypted = plaintext.EncryptWithKey (hexkey, iv).ToByteArray ();
			return encrypted;
		}

		public static string DecryptUnicodeString (this DBUser user, byte[] key, byte[] ciphertext)
		{
			var hexkey = key.ToHexString ();
			string hexcipher = ciphertext.ToHexString ();
			string iv = user.MasterKeySalt;

			string plaintext = hexcipher.DecryptWithKey (hexkey, iv);
			return plaintext;
		}
	}

	public static class CryptoHelper
	{
		// should be used to encrypt a key with another key
		public static string EncryptWithKey (this string plaintext, string hexkey, string iv)
		{
			byte[] key = hexkey.ToByteArray ();
			byte[] byte_iv = iv.ToByteArray ();
			var aes = new AesManaged ();

			if (key.Length != 16 && key.Length != 20 && key.Length != 32)
				throw new Exception ("Key must be 128, 192 or 256 bits");

			ICryptoTransform encryptor = aes.CreateEncryptor (key, byte_iv);
			// Create the streams used for encryption. 
			using (MemoryStream msEncrypt = new MemoryStream())
			{
				using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
				{
					using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
					{
						//Write all data to the stream.
						swEncrypt.Write(plaintext);
					}
					var encrypted = msEncrypt.ToArray();
					return encrypted.ToHexString ();
				}
			}
		}

		public static string DecryptWithKey (this string ciphertext, string hexkey, string iv)
		{
			if (iv == null)
				throw new ArgumentNullException ("iv");
			if (hexkey == null)
				throw new ArgumentNullException ("hexkey");
			if (ciphertext == null)
				throw new ArgumentNullException ("ciphertext");

			string plaintext;
			byte[] key = hexkey.ToByteArray ();
			byte[] byte_iv = iv.ToByteArray ();
			byte[] byte_cipher = ciphertext.ToByteArray ();

			if (key.Length != 16 && key.Length != 20 && key.Length != 32)
				throw new Exception ("Key must be 128, 192 or 256 bits");

			// Create a decrytor to perform the stream transform.
			var aes = new AesManaged () {
				Key = key,
				IV = byte_iv
			};

			ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV); 

			// Create the streams used for decryption. 
			using (MemoryStream msDecrypt = new MemoryStream(byte_cipher))
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
