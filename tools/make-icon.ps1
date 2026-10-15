$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
$inputPath=Join-Path $repoRoot 'assets/voltorb-icon.png'
$outputPath=Join-Path $repoRoot 'assets/voltorb.ico'
Add-Type -AssemblyName System.Drawing
$sizes=@(16,24,32,48,64,128,256)
$images=[Collections.Generic.List[byte[]]]::new()
$source=[Drawing.Bitmap]::new($inputPath)
try{
    foreach($size in $sizes){
        $bitmap=[Drawing.Bitmap]::new($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try{
            $graphics=[Drawing.Graphics]::FromImage($bitmap)
            try{
                $graphics.Clear([Drawing.Color]::Transparent)
                $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.DrawImage($source,[Drawing.Rectangle]::new(0,0,$size,$size))
            }finally{$graphics.Dispose()}
            $stream=[IO.MemoryStream]::new()
            try{$bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png);$images.Add($stream.ToArray())}finally{$stream.Dispose()}
        }finally{$bitmap.Dispose()}
    }
}finally{$source.Dispose()}
$file=[IO.File]::Create($outputPath);$writer=[IO.BinaryWriter]::new($file)
try{
    $writer.Write([UInt16]0);$writer.Write([UInt16]1);$writer.Write([UInt16]$sizes.Count)
    $offset=6+16*$sizes.Count
    for($i=0;$i -lt $sizes.Count;$i++){
        $dim=if($sizes[$i] -eq 256){[byte]0}else{[byte]$sizes[$i]}
        $writer.Write($dim);$writer.Write($dim);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([UInt16]1);$writer.Write([UInt16]32);$writer.Write([UInt32]$images[$i].Length);$writer.Write([UInt32]$offset)
        $offset+=$images[$i].Length
    }
    foreach($image in $images){$writer.Write($image)}
}finally{$writer.Dispose();$file.Dispose()}
Write-Output $outputPath
