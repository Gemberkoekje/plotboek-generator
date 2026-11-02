using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

public sealed class ReadJpegDimensions_specs
{
    [Test]
    public void Returns_zero_for_empty_array()
    {
        var result = DocxTemplateEngine.ReadJpegDimensions([]);

        result.Should().Be((0, 0));
    }

    [Test]
    public void Returns_zero_for_single_byte()
    {
        var result = DocxTemplateEngine.ReadJpegDimensions([0xFF]);

        result.Should().Be((0, 0));
    }

    [Test]
    public void Returns_zero_when_no_SOF_marker_found()
    {
        // Valid JPEG SOI marker but only padding 0xFF bytes, no SOF
        var data = new byte[] { 0xFF, 0xD8, 0xFF, 0xFF, 0xFF, 0xFF };

        var result = DocxTemplateEngine.ReadJpegDimensions(data);

        result.Should().Be((0, 0));
    }

    [Test]
    public void Parses_dimensions_from_SOF0_marker()
    {
        // SOI (FF D8) + SOF0 marker (FF C0) with length and dimensions
        // SOF0 structure after marker: length(2) + precision(1) + height(2) + width(2)
        var data = new byte[]
        {
            0xFF, 0xD8,             // SOI
            0xFF, 0xC0,             // SOF0 marker
            0x00, 0x11,             // length = 17
            0x08,                   // precision
            0x01, 0x00,             // height = 256
            0x02, 0x00,             // width = 512
        };

        var result = DocxTemplateEngine.ReadJpegDimensions(data);

        result.Should().Be((512, 256));
    }

    [Test]
    public void Parses_dimensions_from_SOF2_marker()
    {
        // SOI (FF D8) + SOF2 marker (FF C2) — progressive
        var data = new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xC2,
            0x00, 0x11,
            0x08,
            0x00, 0x64,             // height = 100
            0x00, 0xC8,             // width = 200
        };

        var result = DocxTemplateEngine.ReadJpegDimensions(data);

        result.Should().Be((200, 100));
    }

    [Test]
    public void Skips_non_SOF_markers_to_find_SOF()
    {
        // SOI + APP0 marker (FF E0, 4 bytes payload) + SOF0
        var data = new byte[]
        {
            0xFF, 0xD8,             // SOI
            0xFF, 0xE0,             // APP0 marker
            0x00, 0x04,             // APP0 length = 4 (includes length bytes)
            0x00, 0x00,             // APP0 payload
            0xFF, 0xC0,             // SOF0 marker
            0x00, 0x11,             // SOF length
            0x08,                   // precision
            0x00, 0x50,             // height = 80
            0x00, 0xA0,             // width = 160
        };

        var result = DocxTemplateEngine.ReadJpegDimensions(data);

        result.Should().Be((160, 80));
    }

    [Test]
    public void Skips_padding_FF_bytes()
    {
        // SOI + extra FF padding + SOF0
        var data = new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xFF,             // padding 0xFF
            0xC0,                   // SOF0 (after padding skip)
            0x00, 0x11,
            0x08,
            0x00, 0x30,             // height = 48
            0x00, 0x40,             // width = 64
        };

        var result = DocxTemplateEngine.ReadJpegDimensions(data);

        result.Should().Be((64, 48));
    }

    [Test]
    public void Returns_zero_when_data_truncated_before_dimensions()
    {
        // SOI + SOF0 marker but data too short to contain dimensions
        var data = new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xC0,
            0x00, 0x11,
            0x08,
            0x01,                   // only 1 byte of height, truncated
        };

        var result = DocxTemplateEngine.ReadJpegDimensions(data);

        result.Should().Be((0, 0));
    }

    [Test]
    public void Returns_zero_when_non_FF_byte_encountered()
    {
        // SOI followed by a non-0xFF byte where a marker is expected
        var data = new byte[]
        {
            0xFF, 0xD8,
            0x00, 0xC0,             // invalid: starts with 0x00 instead of 0xFF
        };

        var result = DocxTemplateEngine.ReadJpegDimensions(data);

        result.Should().Be((0, 0));
    }

    [Test]
    public void Returns_zero_when_marker_length_exceeds_data()
    {
        // SOI + non-SOF marker with a length that goes past end of data
        var data = new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xE0,             // APP0 marker
            0xFF, 0xFF,             // length = 65535 (way past end)
        };

        var result = DocxTemplateEngine.ReadJpegDimensions(data);

        result.Should().Be((0, 0));
    }
}


