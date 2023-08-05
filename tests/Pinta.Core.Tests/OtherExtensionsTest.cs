using System.Linq;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class OtherExtensionsTest
{
	[Test]
	public void ToReadOnlyCollection_SecondInvocationReturnsSelf ()
	{
		var source = Enumerable.Range (0, 10);
		var materialized1 = source.ToReadOnlyCollection ();
		var materialized2 = materialized1.ToReadOnlyCollection ();
		Assert.AreSame (materialized1, materialized2);
	}
}
