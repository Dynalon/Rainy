using System;
using NUnit.Framework;
using Rainy.Db;
using System.Linq;
using Rainy.Crypto;

namespace Rainy.Tests
{
	[TestFixture]
	public class CryptoTests
	{
		private bool isOnlyLowercaseHexChars (string text)
		{
			char[] hex_chars = new char[] {'a','b','c','d','e','f'};
			var arr = text.ToCharArray ();
			return arr.All (c => char.IsNumber (c) || hex_chars.Contains (c));
		}

		[Test]
		public void TestKeyToStringGeneration ()
		{
			var rng = new System.Security.Cryptography.RNGCryptoServiceProvider ();

			var key = rng.Create256BitLowerCaseHexKey ();

			Assert.AreEqual (64, key.Length);
			Assert.That (isOnlyLowercaseHexChars (key));
		}
		[Test]
		public void TestKeyStringToBytes ()
		{
			var key = "fdc6e6227bd83d807c0cf6a5ce6df303b4d580b672611a0a7676fd95d7525ec1";
			var key_bytes = new byte[] { 253, 198, 230, 34, 123, 216, 61, 128, 124, 12, 246, 165, 206, 109, 243, 3, 180, 213, 128, 182, 114, 97, 26, 10, 118, 118, 253, 149, 215, 82, 94, 193 };

			var bytes = key.ToByteArray ();
			Assert.AreEqual (key_bytes, bytes);
		}

		[Test]
		public void CreateCryptoFieldsForNewUser ()
		{
			var u = new DBUser ();
			u.Username = "johndoe";
			u.CreateCryptoFields ("foobar");

			Assert.GreaterOrEqual (u.EncryptedMasterKey.Length, 32);
			Assert.That (u.EncryptedMasterKey.Length % 2 == 0);
		}

		[Test]
		public void GetPlaintextMasterKeyReturns256Bit ()
		{
			var u = new DBUser ();
			u.Username = "johndoe";
			var password = "foobar123";
			u.CreateCryptoFields (password);
			var master_key = u.GetPlaintextMasterKey (password);
			Assert.AreEqual(32, master_key.Length);
		}
		[Test]
		public void GetPlaintextMasterKeyReturnsSameKeyForSamePassword ()
		{
			var u = new DBUser ();
			u.Username = "johndoe";
			var password = "foobar123";
			u.CreateCryptoFields (password);
			var key1 = u.GetPlaintextMasterKey (password);
			var key2 = u.GetPlaintextMasterKey (password);

			Assert.AreEqual (key1, key2);
			Assert.AreEqual (key1.ToHexString (), key2.ToHexString ());
		}

		[Test]
		public void PasswordHashHasCorrectLength ()
		{
			var u = new DBUser ();
			u.Username = "johndoe";
			var password = "foobar123";
			u.CreateCryptoFields (password);

			Assert.AreEqual (64, u.PasswordHash.Length);
		}

		[Test]
		public void BasicEncryptAndDecrypt ()
		{
			var u = new DBUser ();
			u.Username = "johndoe";
			var password = "Asdf1234öäü%&";

			u.CreateCryptoFields (password);
			var test_string = "The quick brown fox jumps over the lazy dog.";

			var master_key = u.GetPlaintextMasterKey (password);

			byte[] encrypted_bytes = u.EncryptUnicodeString (master_key, test_string);
			string decrypted_string = u.DecryptUnicodeString (master_key, encrypted_bytes);

			Assert.AreEqual (test_string, decrypted_string);
		}

		[Test]
		public void EncryptDecryptWithHexRepresentation ()
		{
			var u = new DBUser ();
			u.Username = "johndoe";
			var password = "Asdf1234öäü%&";

			u.CreateCryptoFields (password);
			var master_key = u.GetPlaintextMasterKey (password);
			var key = master_key.ToHexString ();
			var test_string = "The quick brown fox jumps over the lazy dog.";

			byte[] encrypted_bytes = u.EncryptUnicodeString (master_key, test_string);
			string encrypted_string = encrypted_bytes.ToHexString ();
			string decrypted_string = u.DecryptUnicodeString (master_key, encrypted_string.ToByteArray ());

			Assert.AreEqual (test_string, decrypted_string);

		}
	}
}

