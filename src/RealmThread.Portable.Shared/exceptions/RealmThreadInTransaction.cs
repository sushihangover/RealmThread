using System;
using System.Runtime.Serialization;

namespace SushiHangover
{
	/// <summary>
	/// Realm thread in transaction.
	/// </summary>
	class RealmThreadInTransaction : RealmThreadException
	{
		public RealmThreadInTransaction()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThreadInTransaction"/> class.
		/// </summary>
		/// <param name="message">Message.</param>
		public RealmThreadInTransaction(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThreadInTransaction"/> class.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="innerException">Inner exception.</param>
		public RealmThreadInTransaction(string message, Exception innerException) : base(message, innerException)
		{
		}

	}
}