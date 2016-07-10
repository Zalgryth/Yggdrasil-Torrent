using System;
using System.Collections.Generic;

namespace YggdrasilTorrent.Core
{
	public static class Extensions
	{
		/// <summary>
		/// Fisher-Yates shuffle implementation from http://stackoverflow.com/a/1262619.
		/// Modified to also allow a return value.
		/// </summary>
		/// <typeparam name="T">Type of object stored in list.</typeparam>
		/// <param name="list">The list to shuffle.</param>
		/// <returns>The shuffled list (which was also modified).</returns>
		public static IList<T> Shuffle<T>(this IList<T> list)
		{
			var rng = new Random();

			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}

			return list;
		}
	}
}
