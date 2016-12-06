using Realms;

namespace SushiHangover.Tests
{

	public class KeyValueRecord : RealmObject
	{
		[PrimaryKey]
		public string Key { get; set; }
		public byte[] Value { get; set; }
	}

}
