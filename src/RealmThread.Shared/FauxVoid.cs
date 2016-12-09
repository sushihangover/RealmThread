
namespace SushiHangover
{
	public sealed class FauxVoid
	{
		FauxVoid() { }
		readonly static FauxVoid nothing = new FauxVoid();
		public static FauxVoid AtAll { get { return nothing; } }
	}

}
