using System;

namespace Woffler.Primitives
{
	public class TrackManifest
	{
		public string Name { get; set; }
		public string Artist { get; set; }
		public string Album { get; set; }
		public string Url { get; set; }
		public string AlbumArtUrl { get; set; }
		public DateTime? ListenTime { get; set; }
	}
}
