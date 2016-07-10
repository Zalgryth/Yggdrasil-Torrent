using System;
using System.Collections.Generic;
using System.Linq;

namespace YggdrasilTorrent.Core
{
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

			handleTorrentKey("announce", false, obj => Announce = (string) obj);
			handleTorrentKey("announce-list", true, obj => ParseAnnounceList((List<object>) obj));
			handleTorrentKey("creation date", true, obj => CreationDate = new DateTime(1970, 1, 1).AddSeconds((long) obj).ToLocalTime());
			handleTorrentKey("comment", true, obj => Comment = (string) obj);
			handleTorrentKey("created by", true, obj => CreatedBy = (string) obj);
			handleTorrentKey("encoding", true, obj => Encoding = (string) obj);

			Dictionary<string, object> info = null;
			handleTorrentKey("info", false, obj => info = (Dictionary<string, object>) obj);
			var handleInfoKey = new Action<string, bool, Action<object>>((key, optional, method) => handleKey(info, key, optional, method));
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
				AnnounceList.Add(tier.Shuffle().Cast<string>().ToList());
			}
		}
	}
}
