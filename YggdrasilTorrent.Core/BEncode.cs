using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YggdrasilTorrent.Core
{
	/// <summary>
	/// Instantiates a decoder for a BEncode-formatted byte array.
	/// </summary>
	public class BEncode
	{
		private byte[] fileContents;
		private int index;

		private const byte control_char_dictionary_start = (byte) 'd';
		private const byte control_char_dictionary_end = (byte) 'e';
		private const byte control_char_list_start = (byte) 'l';
		private const byte control_char_list_end = (byte) 'e';
		private const byte control_char_long_start = (byte) 'i';
		private const byte control_char_long_end = (byte) 'e';
		private const byte control_char_string_split = (byte) ':';

		public BEncode(byte[] fileContents)
		{
			this.fileContents = fileContents;
		}

		public object DecodeObject()
		{
			switch (fileContents[index])
			{
				case control_char_dictionary_start:
					return DecodeDictionary();
				case control_char_list_start:
					return DecodeList();
				case control_char_long_start:
					return DecodeLong();
				default:
					int n;
					// Check for final case: a number, which represents a length of bytes.
					if (!int.TryParse(Encoding.UTF8.GetString(new[] { fileContents[index] }), out n))
						throw new BEncodeException("Invalid control character.");
					return DecodeString();
			}
		}

		private Dictionary<string, object> DecodeDictionary()
		{
			index++; // Skip start character
			var dictionary = new Dictionary<string, object>();
			while (fileContents[index] != control_char_dictionary_end)
			{
				dictionary.Add(DecodeString(), DecodeObject());
			}
			index++; // Skip end character
			return dictionary;
		}

		private List<object> DecodeList()
		{
			index++; // Skip start character
			var list = new List<object>();

			while (fileContents[index] != control_char_list_end)
			{
				list.Add(DecodeObject());
			}

			// Skip the end character.
			index++;
			return list;
		}

		private long DecodeLong(byte endCharacter = control_char_long_end)
		{
			index++; // Skip start character
			int endIndex;
			for (endIndex = index; fileContents[endIndex] != endCharacter; endIndex++)
				;

			var numberString = Encoding.UTF8.GetString(new ArraySegment<byte>(fileContents, index, endIndex - index).ToArray());

			index = endIndex + 1;
			return Convert.ToInt64(numberString);
		}

		private string DecodeString()
		{
			index--; // Circumvent the character skipping in DecodeLong
			var length = (int) DecodeLong(control_char_string_split);

			var returnString = Encoding.UTF8.GetString(new ArraySegment<byte>(fileContents, index, length).ToArray());
			index += length;
			return returnString;
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
