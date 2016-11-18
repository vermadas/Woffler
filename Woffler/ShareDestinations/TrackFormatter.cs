using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woffler.Primitives;

namespace Woffler.ShareDestinations
{
	public class TrackFormatter
	{
		public TrackFormatter(string formatString, string userName)
		{
			_formatString = formatString;
			_userName = userName;
		}

		public string Format(TrackManifest manifest )
		{
			return _formatString.Replace(TrackFormatterWildcards.Artist, manifest.Artist)
				.Replace(TrackFormatterWildcards.Track, manifest.Name)
				.Replace(TrackFormatterWildcards.Album, manifest.Album)
				.Replace(TrackFormatterWildcards.UserName, _userName)
				.Replace(TrackFormatterWildcards.TrackUrl, manifest.Url)
				.Replace(TrackFormatterWildcards.AlbumArtUrl, manifest.AlbumArtUrl);
//				.Replace(TrackFormatterWildcards.ListenTime, manifest.ListenTime);
		}
		private readonly string _formatString;
		private readonly string _userName;
	}

	public sealed class TrackFormatterWildcards
	{
		public const string Artist = "%A";
		public const string Track = "%T";
		public const string Album = "%L";
		public const string UserName = "%N";
		public const string TrackUrl = "%U";
		public const string AlbumArtUrl = "%P";
		public const string ListenTime = "%D";
	}
}
