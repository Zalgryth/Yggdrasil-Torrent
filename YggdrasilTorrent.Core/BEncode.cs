using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YggdrasilTorrent.Core
{
	/// <summary>
	/// Instantiates a decoder for a BEncode-formatted byte array.
	/// </summary>
	public static class BEncode
	{
		private const byte control_char_dictionary_start = (byte) 'd';
		private const byte control_char_dictionary_end = (byte) 'e';
		private const byte control_char_list_start = (byte) 'l';
		private const byte control_char_list_end = (byte) 'e';
		private const byte control_char_long_start = (byte) 'i';
		private const byte control_char_long_end = (byte) 'e';
		private const byte control_char_string_split = (byte) ':';
		
		public static object DecodeObject(DataIndex data)
		{
			var currentChar = data.GetNext();
			switch (currentChar)
			{
				case control_char_dictionary_start:
					return DecodeDictionary(data);
				case control_char_list_start:
					return DecodeList(data);
				case control_char_long_start:
					return DecodeLong(data);
				default:
					int n;
					// Check for final case: a number, which represents a length of bytes.
					if (!int.TryParse(Encoding.UTF8.GetString(new[] { currentChar }), out n))
						throw new BEncodeException("Invalid control character.");
					data.Index--; // This is a special circumstance that requires the "control" character to be parsed.
					return DecodeString(data);
			}
		}

		private static Dictionary<string, object> DecodeDictionary(DataIndex data)
		{
			var dictionary = new Dictionary<string, object>();
			DecodeLoop(data, control_char_dictionary_end, () => dictionary.Add(Encoding.UTF8.GetString(DecodeString(data)), DecodeObject(data)));
			return dictionary;
		}

		private static List<object> DecodeList(DataIndex data)
		{
			var list = new List<object>();
			DecodeLoop(data, control_char_list_end, () => list.Add(DecodeObject(data)));
			return list;
		}

		private static void DecodeLoop(DataIndex data, byte endByte, Action action)
		{
			while (data.Get() != endByte)
			{
				action();
			}
			data.Index++;
		}

		private static long DecodeLong(DataIndex data, byte endByte = control_char_long_end)
		{
			var numberString = Encoding.UTF8.GetString(data.GetBytesUntil(endByte));
			
			return Convert.ToInt64(numberString);
		}

		private static byte[] DecodeString(DataIndex data)
		{
			var length = (int) DecodeLong(data, control_char_string_split);
			
			return data.GetBytes(length);
		}

		public static byte[] EncodeObject(object obj)
		{
			var bytes = new List<byte>();

			if (obj is Dictionary<string, object>)
				bytes.AddRange(EncodeDictionary((Dictionary<string, object>)obj));
			if (obj is List<object>)
				bytes.AddRange(EncodeList((List<object>)obj));
			if (obj is long)
				bytes.AddRange(EncodeLong((long)obj));
			if (obj is string)
				bytes.AddRange(EncodeString(Encoding.UTF8.GetBytes((string) obj)));
			if (obj is byte[])
				bytes.AddRange(EncodeString((byte[])obj));

			return bytes.ToArray();
		}

		private static byte[] EncodeDictionary(Dictionary<string, object> obj)
		{
			var bytes = new List<byte>();

			bytes.Add(control_char_dictionary_start);
			foreach (var lol in obj)
			{
				bytes.AddRange(EncodeString(Encoding.UTF8.GetBytes(lol.Key)));
				bytes.AddRange(EncodeObject(lol.Value));
			}
			bytes.Add(control_char_dictionary_end);

			return bytes.ToArray();
		}
		private static byte[] EncodeList(List<object> obj)
		{
			var bytes = new List<byte>();

			bytes.Add(control_char_list_start);
			foreach (var lol in obj)
			{
				bytes.AddRange(EncodeObject(lol));
			}
			bytes.Add(control_char_list_end);

			return bytes.ToArray();
		}
		private static byte[] EncodeLong(long obj)
		{
			var bytes = new List<byte>();

			bytes.Add(control_char_long_start);
			bytes.AddRange(Encoding.UTF8.GetBytes(obj.ToString()));
			bytes.Add(control_char_long_end);

			return bytes.ToArray();
		}
		private static byte[] EncodeString(byte[] obj)
		{
			var bytes = new List<byte>();

			bytes.AddRange(Encoding.UTF8.GetBytes(obj.Length.ToString()));
			bytes.Add(control_char_string_split);
			bytes.AddRange(obj);

			return bytes.ToArray();
		}
	}

	/// <summary>
	/// A general exception thrown when decoding a BEncode-formatted string fails.
	/// </summary>
	public class BEncodeException : Exception
	{
		public BEncodeException(string message) : base(message) { }
	}
}
