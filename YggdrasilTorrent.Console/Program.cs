using System;
using System.IO;
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
		}
	}
}
