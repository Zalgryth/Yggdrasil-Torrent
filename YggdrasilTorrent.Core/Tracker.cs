using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace YggdrasilTorrent.Core
{
	/// <summary>
	/// This class deals with communicating with the tracker.
	/// </summary>
	public class Tracker
	{
		/// <summary>
		/// Make a request to the announce URL.
		/// </summary>
		/// <param name="peerId">This client's peer ID.</param>
		/// <param name="torrent">The torrent information.</param>
		public static void AnnounceRequest(string peerId, Torrent torrent)
		{
			var builder = new UriBuilder(torrent.Announce);
			var query = HttpUtility.ParseQueryString(builder.Query);
			
			query["peer_id"] = peerId;
			query["port"] = "26644";
			query["uploaded"] = "0";
			query["downloaded"] = "0";
			query["left"] = torrent.TorrentInfo.FileInfos.Sum(i => i.Length).ToString();
			query["compact"] = "1";
			query["event"] = "started";

			// Add info hash manually since we had to manually encode it
			var info_hash = Encoding.UTF8.GetString(WebUtility.UrlEncodeToBytes(torrent.InfoHash, 0, 20));
			builder.Query = string.Format("{0}&info_hash={1}", query.ToString(), info_hash);

			var announceRequest = new WebClient();
			announceRequest.Proxy = null;
			var result = announceRequest.DownloadData(builder.Uri);
			var announceResults = BEncode.DecodeObject(new DataIndex(result));
		}
	}
}
