using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class UtilityTest
{
	[Test]
	public void ClampToByte_Single_TransparentWithinRange ()
	{
		const float MIN = byte.MinValue;
		const float MAX = byte.MaxValue;
		for (float i = MIN; i <= MAX; i++) {
			byte clamped = Utility.ClampToByte (i);
			float convertedBack = clamped;
			Assert.AreEqual (i, convertedBack);
		}
	}

	[TestCase (-1f)]
	[TestCase (-0.1f)]
	[TestCase (float.MinValue)]
	public void ClampToByte_Single_LessThanMinBecomesMin (float n)
	{
		byte clamped = Utility.ClampToByte (n);
		Assert.AreEqual (clamped, byte.MinValue);
	}

	[TestCase (256f)]
	[TestCase (255.1f)]
	[TestCase (float.MaxValue)]
	public void ClampToByte_Single_MoreThanMaxBecomesMax (float n)
	{
		byte clamped = Utility.ClampToByte (n);
		Assert.AreEqual (clamped, byte.MaxValue);
	}

	[Test]
	public void ClampToByte_Double_TransparentWithinRange ()
	{
		const double MIN = byte.MinValue;
		const double MAX = byte.MaxValue;
		for (double i = MIN; i <= MAX; i++) {
			byte clamped = Utility.ClampToByte (i);
			double convertedBack = clamped;
			Assert.AreEqual (i, convertedBack);
		}
	}

	[TestCase (-1d)]
	[TestCase (-0.1d)]
	[TestCase (double.MinValue)]
	public void ClampToByte_Double_LessThanMinBecomesMin (double n)
	{
		byte clamped = Utility.ClampToByte (n);
		Assert.AreEqual (clamped, byte.MinValue);
	}

	[TestCase (256d)]
	[TestCase (255.1d)]
	[TestCase (double.MaxValue)]
	public void ClampToByte_Double_MoreThanMaxBecomesMax (double n)
	{
		byte clamped = Utility.ClampToByte (n);
		Assert.AreEqual (clamped, byte.MaxValue);
	}

	[Test]
	public void ClampToByte_Int32_TransparentWithinRange ()
	{
		const int MIN = byte.MinValue;
		const int MAX = byte.MaxValue;
		for (int i = MIN; i <= MAX; i++) {
			byte clamped = Utility.ClampToByte (i);
			double convertedBack = clamped;
			Assert.AreEqual (i, convertedBack);
		}
	}

	[TestCase (-1)]
	[TestCase (int.MinValue)]
	public void ClampToByte_Int32_LessThanMinBecomesMin (int n)
	{
		byte clamped = Utility.ClampToByte (n);
		Assert.AreEqual (clamped, byte.MinValue);
	}

	[TestCase (256)]
	[TestCase (int.MaxValue)]
	public void ClampToByte_Int32_MoreThanMaxBecomesMax (int n)
	{
		byte clamped = Utility.ClampToByte (n);
		Assert.AreEqual (clamped, byte.MaxValue);
	}
}
