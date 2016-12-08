using System;
using System.Runtime.Serialization;

namespace SushiHangover
{
	/// <summary>
	/// Realm thread not in transaction.
	/// </summary>
	class RealmThreadNotInTransaction : RealmThreadException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThreadNotInTransaction"/> class.
		/// </summary>
		public RealmThreadNotInTransaction()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThreadNotInTransaction"/> class.
		/// </summary>
		/// <param name="message">Message.</param>
		public RealmThreadNotInTransaction(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThreadNotInTransaction"/> class.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="innerException">Inner exception.</param>
		public RealmThreadNotInTransaction(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}