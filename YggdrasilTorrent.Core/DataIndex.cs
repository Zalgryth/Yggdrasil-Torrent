using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YggdrasilTorrent.Core
{
	/// <summary>
	/// Functionality related to a byte array and a pointer to that array.
	/// </summary>
	public class DataIndex
	{
		/// <summary>
		/// The data to be navigated.
		/// </summary>
		public byte[] Data { get; private set; }

		/// <summary>
		/// The current index. Defaults to 0.
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// Create a new DataIndex and start the index at 0.
		/// </summary>
		/// <param name="data">The data to navigate.</param>
		public DataIndex(byte[] data)
		{
			Data = data;
			Index = 0;
		}

		/// <summary>
		/// Get the current byte and increment the pointer.
		/// </summary>
		/// <returns>The current byte.</returns>
		public byte GetNext()
		{
			Index++;
			return Data[Index - 1];
		}

		/// <summary>
		/// Get the current byte.
		/// </summary>
		/// <returns>The current byte.</returns>
		public byte Get()
		{
			return Data[Index];
		}

		/// <summary>
		/// Gets a number of bytes as an array and sets the pointer after the requested data.
		/// </summary>
		/// <param name="length">The number of bytes to retrieve.</param>
		/// <returns>A byte array of the requested length.</returns>
		public byte[] GetBytes(int length)
		{
			var bytes = new ArraySegment<byte>(Data, Index, length).ToArray();
			Index += length;
			return bytes;
		}

		/// <summary>
		/// Gets all the bytes until the end byte provided is reached. Sets the pointer to one past the end character.
		/// </summary>
		/// <param name="endByte">The byte to end on.</param>
		/// <returns>A byte array containing all the bytes until but not including the end byte.</returns>
		public byte[] GetBytesUntil(byte endByte)
		{
			int endIndex;
			for (endIndex = Index; Data[endIndex] != endByte; endIndex++);

			var bytes = GetBytes(endIndex - Index);
			Index++;
			return bytes;
		}
	}
}
