Add-Type -AssemblyName System.Drawing
$json = 'C:\Users\AlmasryMk\.cursor\browser-logs\cdp-response-Page.captureScreenshot-2026-11-09T13-32-44-613Z.json'
$outDir = 'D:\Work\MK\Source\Foods-02-001\Backend\Foods\Foods.Client\visual-qa'
$implPath = Join-Path $outDir 'impl-460.png'
$raw = Get-Content -Raw $json | ConvertFrom-Json
[IO.File]::WriteAllBytes($implPath, [Convert]::FromBase64String($raw.data))

$refPath = 'C:\Users\AlmasryMk\.cursor\projects\d-Work-MK-Source-Foods-02-001-Backend-Foods\assets\d318d7c5-3129-4d4a-8c26-0ca167ac79cd-e19a6e20-769d-492c-8b31-7b65c33c8fd5.jpg'
$ref = New-Object System.Drawing.Bitmap $refPath
$impl = New-Object System.Drawing.Bitmap $implPath

# Drop the phone status bar (y 0-40) and scale the design to the 460 CSS viewport.
$src = New-Object System.Drawing.Rectangle 0, 40, 460, 984
$scaled = New-Object System.Drawing.Bitmap 460, 1024
$g = [System.Drawing.Graphics]::FromImage($scaled)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($ref, (New-Object System.Drawing.Rectangle 0, 0, 460, 1024), $src, [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$ref.Dispose()

$side = New-Object System.Drawing.Bitmap 940, 1024
$sg = [System.Drawing.Graphics]::FromImage($side)
$sg.Clear([System.Drawing.Color]::FromArgb(246, 239, 228))
$sg.DrawImage($scaled, 0, 0)
$sg.DrawImage($impl, 480, 0)
$sg.Dispose()
$side.Save((Join-Path $outDir 'side-by-side.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$side.Dispose()

$overlay = New-Object System.Drawing.Bitmap 460, 1024
$og = [System.Drawing.Graphics]::FromImage($overlay)
$og.DrawImage($impl, 0, 0)
$cm = New-Object System.Drawing.Imaging.ColorMatrix
$cm.Matrix33 = 0.45
$ia = New-Object System.Drawing.Imaging.ImageAttributes
$ia.SetColorMatrix($cm)
$og.DrawImage($scaled, (New-Object System.Drawing.Rectangle 0, 0, 460, 1024), 0, 0, 460, 1024, [System.Drawing.GraphicsUnit]::Pixel, $ia)
$og.Dispose()
$overlay.Save((Join-Path $outDir 'overlay.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$overlay.Dispose()

$diff = New-Object System.Drawing.Bitmap 460, 1024
$rect = New-Object System.Drawing.Rectangle 0, 0, 460, 1024
$a = $scaled.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$b = $impl.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$c = $diff.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$bytes = 1024 * $a.Stride
$ba = New-Object byte[] $bytes
$bb = New-Object byte[] $bytes
$bc = New-Object byte[] $bytes
[Runtime.InteropServices.Marshal]::Copy($a.Scan0, $ba, 0, $bytes)
[Runtime.InteropServices.Marshal]::Copy($b.Scan0, $bb, 0, $bytes)
$sum = 0L
$count = 0L
for ($i = 0; $i -lt $bytes; $i += 4) {
  $d = [Math]::Abs($ba[$i] - $bb[$i]) + [Math]::Abs($ba[$i+1] - $bb[$i+1]) + [Math]::Abs($ba[$i+2] - $bb[$i+2])
  $sum += $d
  $count++
  $v = [Math]::Min(255, [int]($d / 1.5))
  $bc[$i] = $v; $bc[$i+1] = $v; $bc[$i+2] = $v; $bc[$i+3] = 255
}
[Runtime.InteropServices.Marshal]::Copy($bc, 0, $c.Scan0, $bytes)
$scaled.UnlockBits($a); $impl.UnlockBits($b); $diff.UnlockBits($c)
$diff.Save((Join-Path $outDir 'difference.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$diff.Dispose(); $scaled.Dispose(); $impl.Dispose()
$mean = [Math]::Round($sum / $count / 3, 2)
"mean channel delta $mean"
