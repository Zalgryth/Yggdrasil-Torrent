using System;
using System.Collections.Generic;
using System.Linq;

namespace YggdrasilTorrent.Core
{
	/// <summary>
	/// Contains the information required to create a file based on the pieces.
	/// </summary>
	public class FileInfo
	{
		/// <summary>
		/// Length of the file in bytes.
		/// </summary>
		public long Length { get; set; }
		/// <summary>
		/// [Optional]
		/// A 32-character hexadecimal string corresponding to the MD5 sum of the file.
		/// Not part of the BitTorrent spec, but sometimes this will ne included.
		/// </summary>
		public string Md5sum { get; set; }
		/// <summary>
		/// If there are multiple files, this will contain the 
		/// </summary>
		public List<string> Path { get; set; }
	}

	/// <summary>
	/// Contains all of the information contained in the "info" dictionary.
	/// </summary>
	public class TorrentInfo
	{
		/// <summary>
		/// Number of bytes in each piece.
		/// </summary>
		public long PieceLength { get; set; }
		/// <summary>
		/// String consisting of the concatenation of all 20-byte SHA1 hash values, one per piece (byte string, i.e. not urlencoded).
		/// </summary>
		public List<byte[]> Pieces { get; set; }
		/// <summary>
		/// [Optional]
		/// If this is true, the client MUST publish its presence to get other peers ONLY via the trackers explicitly described in the metainfo file. 
		/// If this field is false or null, the client may obtain peer from other means, e.g. PEX peer exchange, dht. 
		/// Here, "private" may be read as "no external peer source".
		/// </summary>
		public bool? Private { get; set; }

		/// <summary>
		/// In Single File Mode, this is the file name.
		/// In Multiple File Mode, this is the name of the directory in which to store all the files.
		/// This is purely advisory.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The list of files to be saved.
		/// </summary>
		public List<FileInfo> FileInfos { get; set; }
	}


	/// <summary>
	/// Created according to file structure provided in https://wiki.theory.org/BitTorrentSpecification.
	/// </summary>
	public class Torrent
	{
		/// <summary>
		/// The announce URL of the tracker.
		/// </summary>
		public string Announce { get; set; }
		/// <summary>
		/// [Optional]
		/// This is an extention to the official specification, offering backwards-compatibility.
		/// </summary>
		public List<List<string>> AnnounceList { get; set; }
		/// <summary>
		/// [Optional]
		/// The creation time of the torrent.
		/// </summary>
		public DateTime? CreationDate { get; set; }
		/// <summary>
		/// [Optional]
		/// Free-form textual comments of the author.
		/// </summary>
		public string Comment { get; set; }
		/// <summary>
		/// [Optional]
		/// Name and version of the program used to create the torrent file.
		/// </summary>
		public string CreatedBy { get; set; }
		/// <summary>
		/// [Optional]
		/// The string encoding format used to generate the pieces part of the info dictionary in the .torrent metafile
		/// </summary>
		public string Encoding { get; set; }

		/// <summary>
		/// Contains all of the information contained in the "info" dictionary.
		/// </summary>
		public TorrentInfo TorrentInfo { get; set; }

		/// <summary>
		/// Creates an empty torrent object.
		/// </summary>
		public Torrent()
		{

		}

		/// <summary>
		/// Create a torrent file based on deserialized BEncode-formatted data.
		/// </summary>
		/// <param name="stuff"></param>
		public Torrent(object torrentInfo)
		{
			var handleKey = new Action<Dictionary<string, object>, string, bool, Action<object>>((dictionary, key, optional, method) =>
			{
				if (dictionary.ContainsKey(key))
				{
					method(dictionary[key]);
				}
				else if (!optional) // It doesn't contain the key but it wasn't optional!
				{
					throw new KeyNotFoundException(string.Format("The required key '{0}' could not be found in the dictionary.", key));
				}
			});
			var torrentInfoDictionary = (Dictionary<string, object>) torrentInfo;
			var handleTorrentKey = new Action<string, bool, Action<object>>((key, optional, method) => handleKey(torrentInfoDictionary, key, optional, method));
			var bytesToString = new Func<byte[], string>(bytes => System.Text.Encoding.UTF8.GetString(bytes));

			handleTorrentKey("announce", false, obj => Announce = bytesToString((byte[])obj));
			handleTorrentKey("announce-list", true, obj => ParseAnnounceList((List<object>) obj));
			handleTorrentKey("creation date", true, obj => CreationDate = new DateTime(1970, 1, 1).AddSeconds((long) obj).ToLocalTime());
			handleTorrentKey("comment", true, obj => Comment = bytesToString((byte[]) obj));
			handleTorrentKey("created by", true, obj => CreatedBy = bytesToString((byte[]) obj));
			handleTorrentKey("encoding", true, obj => Encoding = bytesToString((byte[]) obj));

			Dictionary<string, object> info = null;
			handleTorrentKey("info", false, obj => info = (Dictionary<string, object>) obj);
			var handleInfoKey = new Action<string, bool, Action<object>>((key, optional, method) => handleKey(info, key, optional, method));
			TorrentInfo = new TorrentInfo();
			handleInfoKey("piece length", false, obj => TorrentInfo.PieceLength = (long) obj);
			handleInfoKey("pieces", false, obj => ParsePieces((byte[]) obj));
			handleInfoKey("private", true, obj => TorrentInfo.Private = (long) obj == 1);
			handleInfoKey("name", false, obj => TorrentInfo.Name = bytesToString((byte[]) obj));

			TorrentInfo.FileInfos = new List<FileInfo>();

			if (info.ContainsKey("files"))
			{
				// Multiple file mode
				var files = (List<object>) info["files"];
				foreach (var file in files)
				{
					var fileDictionary = (Dictionary<string, object>) file;
					var fileInfo = new FileInfo();
					handleKey(fileDictionary, "length", false, obj => fileInfo.Length = (long) obj);
					handleKey(fileDictionary, "md5sum", true, obj => fileInfo.Md5sum = bytesToString((byte[]) obj));
					handleKey(fileDictionary, "path", false, obj => fileInfo.Path = ((List<object>) obj).Select(i => bytesToString((byte[])i)).ToList());
					TorrentInfo.FileInfos.Add(fileInfo);
				}
			}
			else
			{
				// Single file mode
				var fileInfo = new FileInfo();
				handleInfoKey("length", false, obj => fileInfo.Length = (long) obj);
				handleInfoKey("md5sum", true, obj => fileInfo.Md5sum = bytesToString((byte[]) obj));
				TorrentInfo.FileInfos.Add(fileInfo);
			}
		}

		/// <summary>
		/// Properly parses announce-list data. According to the spec, requires shuffling per tier.
		/// </summary>
		/// <param name="announceList"></param>
		private void ParseAnnounceList(List<object> announceList)
		{
			AnnounceList = new List<List<string>>();
			foreach (var tierObj in announceList)
			{
				var tier = (List<object>) tierObj;
				AnnounceList.Add(tier.Shuffle().Select(i => System.Text.Encoding.UTF8.GetString((byte[])i)).ToList());
			}
		}

		private void ParsePieces(byte[] pieces)
		{
			TorrentInfo.Pieces = new List<byte[]>();
			for (var i = 0; i < pieces.Length; i += 20)
			{
				TorrentInfo.Pieces.Add(pieces.Skip(i).Take(20).ToArray());
			}
		}
	}
}
