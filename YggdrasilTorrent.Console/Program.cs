using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using YggdrasilTorrent.Core;

namespace YggdrasilTorrent.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			// Load a sample torrent file
			var fileContents = File.ReadAllBytes(@"insert-sample-torrent-path-here.torrent");

			// Get an object dictionary of the file contents
			var objects = BEncode.DecodeObject(new DataIndex(fileContents));

			// Parse the contents of the BEncode decoding
			var torrentInfo = new Torrent(objects);

			// Load the contents of the downloaded file
			var dlContents = File.ReadAllBytes(@"insert-sample-downloaded-file-here");

			// Compute the hash of the real downloaded file
			var sha1 = SHA1.Create();
			var sha1Hash = sha1.ComputeHash(dlContents.Take((int)torrentInfo.TorrentInfo.PieceLength).ToArray());

			if (sha1Hash.SequenceEqual(torrentInfo.TorrentInfo.Pieces.First()))
			{
				// If this evaluates to true, the following has been verified:
				// - The BEncode decoding worked correctly.
				// - The piece length was read in accurately.
				// - The pieces list was read in correctly.
				// - The SHA1 computation we just computed is the same as the first set of bytes in the pieces section of the torrent file.
			}
		}
	}
}
