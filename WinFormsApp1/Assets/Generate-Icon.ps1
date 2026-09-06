Add-Type -AssemblyName System.Drawing
$iconSizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$iconImages = @()
foreach ($iconSize in $iconSizes) {
    $bitmap = [System.Drawing.Bitmap]::new($iconSize, $iconSize)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $inset = $iconSize * 0.035
    $edge = $iconSize - 2 * $inset
    $corner = $iconSize * 0.44
    $shape = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $shape.AddArc($inset, $inset, $corner, $corner, 180, 90)
    $shape.AddArc($inset + $edge - $corner, $inset, $corner, $corner, 270, 90)
    $shape.AddArc($inset + $edge - $corner, $inset + $edge - $corner, $corner, $corner, 0, 90)
    $shape.AddArc($inset, $inset + $edge - $corner, $corner, $corner, 90, 90)
    $shape.CloseFigure()
    $background = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#77e6bf'))
    $foreground = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#101820'))
    $graphics.FillPath($background, $shape)
    $font = [System.Drawing.Font]::new('Segoe UI', $iconSize * 0.70, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $graphics.DrawString('Q', $font, $foreground, [System.Drawing.RectangleF]::new(0, -$iconSize * 0.018, $iconSize, $iconSize), $format)
    $buffer = [System.IO.MemoryStream]::new()
    $bitmap.Save($buffer, [System.Drawing.Imaging.ImageFormat]::Png)
    $iconImages += ,$buffer.ToArray()
    if ($iconSize -eq 256) { $bitmap.Save((Join-Path $PSScriptRoot 'QuickTools.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $buffer.Dispose(); $format.Dispose(); $font.Dispose(); $background.Dispose(); $foreground.Dispose(); $shape.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
$file = [System.IO.File]::Create((Join-Path $PSScriptRoot 'QuickTools.ico'))
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$iconSizes.Count)
    $offset = 6 + 16 * $iconSizes.Count
    for ($index = 0; $index -lt $iconSizes.Count; $index++) {
        $dimension = if ($iconSizes[$index] -eq 256) { 0 } else { $iconSizes[$index] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$iconImages[$index].Length); $writer.Write([uint32]$offset)
        $offset += $iconImages[$index].Length
    }
    foreach ($iconImage in $iconImages) { $writer.Write([byte[]]$iconImage) }
} finally { $writer.Dispose(); $file.Dispose() }
