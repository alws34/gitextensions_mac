using System.Globalization;
using Avalonia;
using Avalonia.Media;
using GitUI.Avalonia.Converters;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.Converters;

[TestFixture]
public class BasicConvertersTests
{
    [TestCase(true, true)]
    [TestCase(false, false)]
    [TestCase(null, false)]
    public void BoolToVisibility_Convert_MapsOnlyTrueToVisible(object? value, bool expected)
    {
        object result = BoolToVisibilityConverter.Instance.Convert(
            value,
            typeof(bool),
            parameter: null,
            CultureInfo.InvariantCulture);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(null, false)]
    public void InverseBool_Convert_MapsOnlyFalseToTrue(object? value, bool expected)
    {
        object result = InverseBoolConverter.Instance.Convert(
            value,
            typeof(bool),
            parameter: null,
            CultureInfo.InvariantCulture);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void NullToVisibility_Convert_MapsNullToFalse()
    {
        object nullResult = NullToVisibilityConverter.Instance.Convert(
            value: null,
            typeof(bool),
            parameter: null,
            CultureInfo.InvariantCulture);

        object nonNullResult = NullToVisibilityConverter.Instance.Convert(
            "value",
            typeof(bool),
            parameter: null,
            CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(nullResult, Is.False);
            Assert.That(nonNullResult, Is.True);
        });
    }

    [Test]
    public void BoolToFontWeight_Convert_MapsTrueToBold()
    {
        object bold = BoolToFontWeightConverter.Instance.Convert(
            true,
            typeof(FontWeight),
            parameter: null,
            CultureInfo.InvariantCulture);

        object normal = BoolToFontWeightConverter.Instance.Convert(
            false,
            typeof(FontWeight),
            parameter: null,
            CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(bold, Is.EqualTo(FontWeight.Bold));
            Assert.That(normal, Is.EqualTo(FontWeight.Normal));
        });
    }

    [Test]
    public void DepthToMargin_Convert_UsesSixteenPixelsPerDepth()
    {
        object result = DepthToMarginConverter.Instance.Convert(
            3,
            typeof(Thickness),
            parameter: null,
            CultureInfo.InvariantCulture);

        Assert.That(result, Is.EqualTo(new Thickness(48, 0, 0, 0)));
    }
}
