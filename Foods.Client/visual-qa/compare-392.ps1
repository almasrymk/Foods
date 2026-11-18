Add-Type -AssemblyName System.Drawing
$json = 'C:\Users\AlmasryMk\.cursor\browser-logs\cdp-response-Page.captureScreenshot-2026-11-09T13-49-50-133Z.json'
$outDir = 'D:\Work\MK\Source\Foods-02-001\Backend\Foods\Foods.Client\visual-qa'
$raw = Get-Content -Raw $json | ConvertFrom-Json
$implPath = Join-Path $outDir 'current.png'
[IO.File]::WriteAllBytes($implPath, [Convert]::FromBase64String($raw.data))
$impl = New-Object System.Drawing.Bitmap $implPath
$ref = New-Object System.Drawing.Bitmap (Join-Path $outDir 'reference-scaled.png')
$w = 392
$h = [Math]::Min($impl.Height, $ref.Height)
$side = New-Object System.Drawing.Bitmap (($w * 2) + 16), $h
$g = [System.Drawing.Graphics]::FromImage($side)
$g.Clear([System.Drawing.Color]::FromArgb(246, 239, 228))
$g.DrawImage($ref, (New-Object System.Drawing.Rectangle 0, 0, $w, $h), (New-Object System.Drawing.Rectangle 0, 0, $w, $h), [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($impl, (New-Object System.Drawing.Rectangle ($w + 16), 0, $w, $h), (New-Object System.Drawing.Rectangle 0, 0, $w, $h), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$side.Save((Join-Path $outDir 'side-by-side.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$side.Dispose()
$overlay = New-Object System.Drawing.Bitmap $w, $h
$og = [System.Drawing.Graphics]::FromImage($overlay)
$og.DrawImage($impl, (New-Object System.Drawing.Rectangle 0, 0, $w, $h), (New-Object System.Drawing.Rectangle 0, 0, $w, $h), [System.Drawing.GraphicsUnit]::Pixel)
$cm = New-Object System.Drawing.Imaging.ColorMatrix
$cm.Matrix33 = 0.5
$ia = New-Object System.Drawing.Imaging.ImageAttributes
$ia.SetColorMatrix($cm)
$og.DrawImage($ref, (New-Object System.Drawing.Rectangle 0, 0, $w, $h), 0, 0, $w, $h, [System.Drawing.GraphicsUnit]::Pixel, $ia)
$og.Dispose()
$overlay.Save((Join-Path $outDir 'overlay.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$overlay.Dispose()
$diff = New-Object System.Drawing.Bitmap $w, $h
$rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
$a = $ref.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$b = $impl.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$c = $diff.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$bytes = $h * $a.Stride
$ba = New-Object byte[] $bytes
$bb = New-Object byte[] $bytes
$bc = New-Object byte[] $bytes
[Runtime.InteropServices.Marshal]::Copy($a.Scan0, $ba, 0, $bytes)
[Runtime.InteropServices.Marshal]::Copy($b.Scan0, $bb, 0, $bytes)
$sum = 0L; $count = 0L
for ($i = 0; $i -lt $bytes; $i += 4) {
  $d = [Math]::Abs([int]$ba[$i] - $bb[$i]) + [Math]::Abs([int]$ba[$i+1] - $bb[$i+1]) + [Math]::Abs([int]$ba[$i+2] - $bb[$i+2])
  $sum += $d; $count++
  $v = [Math]::Min(255, [int]($d / 1.5))
  $bc[$i] = $v; $bc[$i+1] = $v; $bc[$i+2] = $v; $bc[$i+3] = 255
}
[Runtime.InteropServices.Marshal]::Copy($bc, 0, $c.Scan0, $bytes)
$ref.UnlockBits($a); $impl.UnlockBits($b); $diff.UnlockBits($c)
$diff.Save((Join-Path $outDir 'difference.png'), [System.Drawing.Imaging.ImageFormat]::Png)
"impl $($impl.Width)x$($impl.Height) ref $($ref.Width)x$($ref.Height) mean $([Math]::Round($sum / $count / 3, 2))"
$diff.Dispose(); $ref.Dispose(); $impl.Dispose()
