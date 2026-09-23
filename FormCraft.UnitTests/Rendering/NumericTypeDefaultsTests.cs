namespace FormCraft.UnitTests.Rendering;

/// <summary>
/// Pins <see cref="NumericTypeDefaults{TValue}"/> for every numeric type the two adapters render
/// today (#389) — one shared implementation instead of four private copies.
/// </summary>
public class NumericTypeDefaultsTests
{
    [Fact]
    public void Int_Should_Resolve_Its_Own_Range_And_A_Step_Of_One()
    {
        NumericTypeDefaults<int>.Min.ShouldBe(int.MinValue);
        NumericTypeDefaults<int>.Max.ShouldBe(int.MaxValue);
        NumericTypeDefaults<int>.Step.ShouldBe(1);
    }

    [Fact]
    public void Decimal_Should_Resolve_Its_Own_Range_And_A_Step_Of_Zero_Point_Zero_One()
    {
        NumericTypeDefaults<decimal>.Min.ShouldBe(decimal.MinValue);
        NumericTypeDefaults<decimal>.Max.ShouldBe(decimal.MaxValue);
        NumericTypeDefaults<decimal>.Step.ShouldBe(0.01m);
    }

    [Fact]
    public void Double_Should_Resolve_Its_Own_Range_And_A_Step_Of_Zero_Point_One()
    {
        NumericTypeDefaults<double>.Min.ShouldBe(double.MinValue);
        NumericTypeDefaults<double>.Max.ShouldBe(double.MaxValue);
        NumericTypeDefaults<double>.Step.ShouldBe(0.1d);
    }

    [Fact]
    public void Float_Should_Resolve_Its_Own_Range_And_A_Step_Of_Zero_Point_One()
    {
        NumericTypeDefaults<float>.Min.ShouldBe(float.MinValue);
        NumericTypeDefaults<float>.Max.ShouldBe(float.MaxValue);
        NumericTypeDefaults<float>.Step.ShouldBe(0.1f);
    }

    [Fact]
    public void Long_Should_Resolve_Its_Own_Range_And_A_Step_Of_One()
    {
        NumericTypeDefaults<long>.Min.ShouldBe(long.MinValue);
        NumericTypeDefaults<long>.Max.ShouldBe(long.MaxValue);
        NumericTypeDefaults<long>.Step.ShouldBe(1L);
    }

    [Fact]
    public void Short_Should_Resolve_Its_Own_Range_And_A_Step_Of_One()
    {
        NumericTypeDefaults<short>.Min.ShouldBe(short.MinValue);
        NumericTypeDefaults<short>.Max.ShouldBe(short.MaxValue);
        NumericTypeDefaults<short>.Step.ShouldBe((short)1);
    }

    [Fact]
    public void Byte_Should_Resolve_Its_Own_Range_And_A_Step_Of_One()
    {
        NumericTypeDefaults<byte>.Min.ShouldBe(byte.MinValue);
        NumericTypeDefaults<byte>.Max.ShouldBe(byte.MaxValue);
        NumericTypeDefaults<byte>.Step.ShouldBe((byte)1);
    }
}
