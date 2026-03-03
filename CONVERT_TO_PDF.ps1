# Convert Markdown to PDF using Microsoft Word
param(
    [string]$MarkdownFile = "HECATEONCORE_PROJECT_OVERVIEW.md",
    [string]$OutputPdf = "$env:USERPROFILE\Desktop\HecateonCore_Project_Overview.pdf"
)

Write-Host "Converting Markdown to PDF..." -ForegroundColor Cyan

try {
    # Read the markdown file
    $mdContent = Get-Content $MarkdownFile -Raw
    
    # Create a temporary HTML file with better styling
    $htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 900px;
            margin: 40px auto;
            padding: 20px;
        }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        h2 { color: #34495e; border-bottom: 2px solid #95a5a6; padding-bottom: 5px; margin-top: 30px; }
        h3 { color: #7f8c8d; }
        code {
            background-color: #f4f4f4;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Consolas', 'Courier New', monospace;
        }
        pre {
            background-color: #f4f4f4;
            padding: 15px;
            border-radius: 5px;
            overflow-x: auto;
        }
        pre code {
            background-color: transparent;
            padding: 0;
        }
        blockquote {
            border-left: 4px solid #3498db;
            padding-left: 20px;
            color: #7f8c8d;
            font-style: italic;
        }
        a { color: #3498db; text-decoration: none; }
        a:hover { text-decoration: underline; }
        ul, ol { padding-left: 30px; }
        li { margin: 5px 0; }
        table {
            border-collapse: collapse;
            width: 100%;
            margin: 20px 0;
        }
        th, td {
            border: 1px solid #ddd;
            padding: 12px;
            text-align: left;
        }
        th {
            background-color: #3498db;
            color: white;
        }
        tr:nth-child(even) {
            background-color: #f9f9f9;
        }
        .emoji {
            font-size: 1.2em;
        }
    </style>
</head>
<body>
"@

    # Simple markdown to HTML conversion
    $htmlBody = $mdContent `
        -replace '### (.*)', '<h3>$1</h3>' `
        -replace '## (.*)', '<h2>$1</h2>' `
        -replace '# (.*)', '<h1>$1</h1>' `
        -replace '^\* (.*)', '<li>$1</li>' `
        -replace '^\- (.*)', '<li>$1</li>' `
        -replace '\*\*(.*?)\*\*', '<strong>$1</strong>' `
        -replace '\*(.*?)\*', '<em>$1</em>' `
        -replace '`([^`]+)`', '<code>$1</code>' `
        -replace '\[([^\]]+)\]\(([^\)]+)\)', '<a href="$2">$1</a>' `
        -replace '\n\n', '</p><p>' `
        -replace '---', '<hr>'
    
    $htmlBody = "<p>$htmlBody</p>"
    $htmlBody = $htmlBody -replace '<p></p>', ''
    
    $htmlContent += $htmlBody
    $htmlContent += @"
</body>
</html>
"@

    # Save HTML file
    $tempHtml = "$env:TEMP\HecateonCore_Overview_Temp.html"
    $htmlContent | Out-File -FilePath $tempHtml -Encoding UTF8
    
    Write-Host "Created temporary HTML file: $tempHtml" -ForegroundColor Green
    
    # Try to use Word to convert to PDF
    try {
        $word = New-Object -ComObject Word.Application
        $word.Visible = $false
        
        Write-Host "Opening document in Word..." -ForegroundColor Yellow
        $doc = $word.Documents.Open($tempHtml)
        
        Write-Host "Converting to PDF..." -ForegroundColor Yellow
        $doc.SaveAs([ref]$OutputPdf, [ref]17) # 17 = wdFormatPDF
        
        $doc.Close()
        $word.Quit()
        
        [System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
        
        Write-Host "`n✅ SUCCESS! PDF created at:" -ForegroundColor Green
        Write-Host "   $OutputPdf" -ForegroundColor Cyan
        
        # Clean up temp file
        Remove-Item $tempHtml -ErrorAction SilentlyContinue
        
        # Open the PDF
        Start-Process $OutputPdf
        
    } catch {
        Write-Host "`n⚠️ Word automation failed. Creating standalone HTML instead..." -ForegroundColor Yellow
        
        # Copy HTML to desktop instead
        $htmlOutput = "$env:USERPROFILE\Desktop\HecateonCore_Project_Overview.html"
        Copy-Item $tempHtml $htmlOutput -Force
        
        Write-Host "`n✅ HTML file created at:" -ForegroundColor Green
        Write-Host "   $htmlOutput" -ForegroundColor Cyan
        Write-Host "`nYou can open this in a browser and use Print → Save as PDF" -ForegroundColor Yellow
        
        Start-Process $htmlOutput
    }
    
} catch {
    Write-Host "`n❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nTrying alternative method..." -ForegroundColor Yellow
    
    # Fallback: Just copy the markdown file to desktop
    $mdOutput = "$env:USERPROFILE\Desktop\HecateonCore_Project_Overview.md"
    Copy-Item $MarkdownFile $mdOutput -Force
    Write-Host "✅ Markdown file copied to: $mdOutput" -ForegroundColor Green
    Write-Host "You can use an online converter or VS Code to convert to PDF" -ForegroundColor Yellow
}

Write-Host "`nDone!" -ForegroundColor Green
