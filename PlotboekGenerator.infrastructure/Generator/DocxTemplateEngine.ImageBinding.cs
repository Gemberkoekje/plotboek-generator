using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;

using Blip = DocumentFormat.OpenXml.Drawing.Blip;

namespace PlotboekGenerator.infrastructure.Generator;

public static partial class DocxTemplateEngine
{
    /// <summary>
    /// Finds SDT elements whose tags resolve to <c>byte[]</c> on the model and
    /// replaces the embedded image in the picture content control with the provided bytes.
    /// </summary>
    private static void BindImageSdtElements(OpenXmlElement body, object model, MainDocumentPart mainPart)
    {
        foreach (var sdt in body.Descendants<SdtElement>().ToList())
        {
            var tag = GetTagValue(sdt);
            if (string.IsNullOrWhiteSpace(tag) || IsSpecialTag(tag))
                continue;

            var value = ResolvePath(model, tag);
            if (value is not byte[] imageBytes || imageBytes.Length == 0)
                continue;

            ReplaceImageInSdt(sdt, imageBytes, mainPart);
        }
    }

    /// <summary>
    /// Replaces the image inside a picture content control SDT with the given bytes.
    /// </summary>
    private static void ReplaceImageInSdt(SdtElement sdt, byte[] imageBytes, MainDocumentPart mainPart)
    {
        var blip = sdt.Descendants<Blip>().FirstOrDefault();
        if (blip is null)
            return;

        var contentType = DetectImageContentType(imageBytes);
        var newPart = mainPart.AddImagePart(contentType);
        using (var ms = new MemoryStream(imageBytes))
        {
            newPart.FeedData(ms);
        }

        blip.Embed = mainPart.GetIdOfPart(newPart);

        // Adjust the extent to preserve the uploaded image's aspect ratio
        AdjustImageExtent(sdt, imageBytes);

        // Remove the placeholder marker so the picture renders normally
        var pr = sdt.SdtProperties;
        pr?.RemoveAllChildren<ShowingPlaceholder>();
    }

    /// <summary>
    /// Adjusts the inline extent (<c>wp:extent</c>) and transform extent (<c>a:ext</c>)
    /// so the uploaded image keeps its aspect ratio while fitting inside the box the
    /// template reserved for it. Scaling to the height alone would let a landscape
    /// image grow far wider than the text column.
    /// </summary>
    private static void AdjustImageExtent(SdtElement sdt, byte[] imageBytes)
    {
        var (imgWidth, imgHeight) = ReadImageDimensions(imageBytes);
        if (imgWidth <= 0 || imgHeight <= 0)
            return;

        // wp:extent - the display size of the inline drawing (EMUs)
        var inlineExtent = sdt.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>().FirstOrDefault();
        if (inlineExtent?.Cy?.Value is not > 0 || inlineExtent.Cx?.Value is not > 0)
            return;

        var boxCx = inlineExtent.Cx.Value;
        var boxCy = inlineExtent.Cy.Value;

        // Fit inside the reserved box: the tighter of the two dimensions wins.
        var scale = Math.Min((double)boxCx / imgWidth, (double)boxCy / imgHeight);
        var newCx = Math.Max(1L, (long)(imgWidth * scale));
        var newCy = Math.Max(1L, (long)(imgHeight * scale));

        inlineExtent.Cx = newCx;
        inlineExtent.Cy = newCy;

        // a:ext inside a:xfrm - the transform extent must match
        foreach (var ext in sdt.Descendants<DocumentFormat.OpenXml.Drawing.Extents>())
        {
            ext.Cx = newCx;
            ext.Cy = newCy;
        }
    }

    /// <summary>
    /// Reads the pixel dimensions from image file bytes by parsing the format header.
    /// Supports JPEG, PNG, GIF, and BMP.
    /// </summary>
    private static (int Width, int Height) ReadImageDimensions(byte[] data)
    {
        // PNG: width/height in IHDR chunk at bytes 16-23 (big-endian)
        if (data.Length >= 24 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            var w = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
            var h = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
            return (w, h);
        }

        // JPEG: scan for SOF marker
        if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xD8)
        {
            return ReadJpegDimensions(data);
        }

        // GIF: width at 6-7, height at 8-9 (little-endian)
        if (data.Length >= 10 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
        {
            var w = data[6] | (data[7] << 8);
            var h = data[8] | (data[9] << 8);
            return (w, h);
        }

        // BMP: width at 18-21, height at 22-25 (little-endian, height may be negative)
        if (data.Length >= 26 && data[0] == 0x42 && data[1] == 0x4D)
        {
            var w = data[18] | (data[19] << 8) | (data[20] << 16) | (data[21] << 24);
            var h = data[22] | (data[23] << 8) | (data[24] << 16) | (data[25] << 24);
            return (w, Math.Abs(h));
        }

        return (0, 0);
    }

    /// <summary>
    /// Reads JPEG dimensions by scanning for a Start of Frame (SOF) marker.
    /// </summary>
    internal static (int Width, int Height) ReadJpegDimensions(byte[] data)
    {
        var pos = 2;
        while (pos < data.Length - 1)
        {
            if (data[pos] != 0xFF)
                break;

            var marker = data[pos + 1];

            // Skip padding 0xFF bytes
            if (marker == 0xFF)
            {
                pos++;
                continue;
            }

            if (IsSofMarker(marker))
            {
                if (pos + 9 > data.Length)
                    break;
                var h = (data[pos + 5] << 8) | data[pos + 6];
                var w = (data[pos + 7] << 8) | data[pos + 8];
                return (w, h);
            }

            // Skip to next marker
            if (pos + 3 >= data.Length)
                break;
            var length = (data[pos + 2] << 8) | data[pos + 3];
            pos += 2 + length;
        }

        return (0, 0);
    }

    /// <summary>
    /// Returns <c>true</c> if the JPEG marker byte is a Start of Frame (SOF) marker.
    /// SOF markers: C0-C3, C5-C7, C9-CB, CD-CF.
    /// </summary>
    private static bool IsSofMarker(byte marker)
        => marker is(>= 0xC0 and <= 0xC3) or(>= 0xC5 and <= 0xC7) or(>= 0xC9 and <= 0xCB) or(>= 0xCD and <= 0xCF);

    /// <summary>
    /// Detects the image format from the first bytes and returns the corresponding MIME content type.
    /// Defaults to <c>"image/png"</c> when the format cannot be determined.
    /// </summary>
    private static string DetectImageContentType(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";

        if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return "image/png";

        if (data.Length >= 3 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
            return "image/gif";

        if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
            return "image/bmp";

        return "image/png";
    }
}
