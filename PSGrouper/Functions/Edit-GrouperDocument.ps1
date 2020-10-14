<#
    .SYNOPSIS
        Allows editing of document JSON

    .DESCRIPTION
        Opens a window for editing a Grouper document as JSON. Validate button validates the
        document in the editor and prints errors to the console. By pressing Next the JSON
        is validated and converted to a Grouper document object and written to the pipeline.
        Skip button discards the current document and loads the next document in the pipeline.
        Finally Cancel will discard the current document and stop pipeline processing.
        The same happens when the window is Closed by the user.

    .PARAMETER InputObject
        Grouper document or database document entry

    .INPUTS
        (see InputObject)

    .OUTPUTS
        Grouper document (if 'Next' button is pressed. 'Skip' och 'Cancel' will not produce any output)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | Edit-GrouperDocument | Update-GrouperDocument
#>
function Edit-GrouperDocument
{
    param (
        [Parameter(Mandatory=$true,ValueFromPipeline=$true,Position=0)]
        [object]
        $InputObject
    )

    begin {
      if (-not (CheckApi)) {
        break
      }
      [xml]$xaml =
@"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Document Editor" Height="650" Width="570">
        <DockPanel LastChildFill="False">
        <StackPanel Orientation="Horizontal" DockPanel.Dock="Top">
            <Button x:Name="Next" Content="Next" Margin="5,5,5,5" />
            <Button x:Name="Skip" Content="Skip" Margin="5,5,5,5" />
            <Button x:Name="Cancel" Content="Cancel" Margin="5,5,5,5" />
            <Rectangle VerticalAlignment="Stretch" Fill="Gray" Width="1"  Margin="6, 0, 6, 0" />
            <Button x:Name="Validate" Content="Validate" Margin="5,5,5,5" />
            <Rectangle VerticalAlignment="Stretch" Fill="Gray" Width="1"  Margin="6, 0, 6, 0" />
            <Button x:Name="Diff" Content="Diff" Margin="5,5,5,5" />
            <Rectangle VerticalAlignment="Stretch" Fill="Gray" Width="1"  Margin="6, 0, 6, 0" />
            <Button x:Name="InsertGuid" Content="Insert GUID" Margin="5,5,5,5" Padding="3,0,3,0" />
            <Button x:Name="InsertMemberSource" Content="Insert source:" Margin="5,5,5,5" Padding="3,0,3,0" />
            <ComboBox x:Name="MemberSource" Margin="0,5,5,5">
                <ComboBoxItem Content="Static" IsSelected="True" />
                <ComboBoxItem Content="Personalsystem" />
                <ComboBoxItem Content="Elevregister" />
                <ComboBoxItem Content="OnPremAD - Filter" />
                <ComboBoxItem Content="OnPremAD - Group" />
                <ComboBoxItem Content="AzureAD" />
                <ComboBoxItem Content="EXO" />
                <ComboBoxItem Content="MetaDirectory" />
            </ComboBox>
            <Button x:Name="InsertRule" Content="Insert rule" Margin="5,5,5,5" Padding="3,0,3,0" />
        </StackPanel>
        <RichTextBox x:Name="JsonContent" FontFamily="Consolas" AcceptsTab="True">
            <RichTextBox.Resources>
                <Style TargetType="{x:Type Paragraph}">
                    <Setter Property="Margin" Value="3" />
                </Style>
            </RichTextBox.Resources>
        </RichTextBox>
    </DockPanel>
</Window>
"@

        $memberSources = @{}

        $memberSources["Static"] =
@"
    {
      "source": "Static",
      "action": "Include",
      "rules": [
        {
          "name": "Upn",
          "value": ""
        }
      ]
    }
"@
        $memberSources["Personalsystem"] =
@"
    {
      "source": "Personalsystem",
      "action": "Include",
      "rules": [
        {
          "name": "Organisation",
          "value": ""
        },
        {
          "name": "IncludeManager",
          "value": true
        }
      ]
    }
"@
        $memberSources["Elevregister"] =
@"
    {
      "source": "Elevregister",
      "action": "Include",
      "rules": [
        {
          "name": "Enhet",
          "value": ""
        },
        {
          "name": "Roll",
          "value": "Personal"
        }
      ]
    }
"@
        $memberSources["OnPremAd - Query"] =
@"
    {
      "source": "OnPremAdQuery",
      "action": "Include",
      "rules": [
        {
          "name": "LdapFilter",
          "value": ""
        },
        {
          "name": "SearchBase",
          "value": ""
        }
      ]
    }
"@
        $memberSources["OnPremAd - Group"] =
@"
    {
      "source": "OnPremAdGroup",
      "action": "Include",
      "rules": [
        {
          "name": "Group",
          "value": ""
        }
      ]
    }
"@
        $memberSources["AzureAd - Group"] =
@"
    {
      "source": "AzureAdGroup",
      "action": "Include",
      "rules": [
        {
          "name": "Group",
          "value": ""
        }
      ]
    }
"@
        $memberSources["Exo - Group"] =
@"
    {
      "source": "ExoGroup",
      "action": "Include",
      "rules": [
        {
          "name": "Group",
          "value": ""
        }
      ]
    }
"@
        $memberSources["CustomView"] =
@"
    {
      "source": "CustomView",
      "action": "Include",
      "rules": [
        {
          "name": "View",
          "value": ""
        }
      ]
    }
"@
      $emptyRule =
@"
        {
          "name": "",
          "value": ""
        }
"@
        try {
            $reader = New-Object -TypeName 'System.Xml.XmlNodeReader' -ArgumentList @($xaml)
            $window = [Windows.Markup.XamlReader]::Load($reader)
        }
        finally {
            if ($reader) {
                $reader.Dispose()
            }
        }
        $control = $window.FindName('Validate')
        $control.Add_Click({
            $json = GetContent
            if (Validate $json) {
                Write-Host "Document is valid" -ForegroundColor Green
                $doc = ConvertTo-GrouperDocument -InputObject $json
                SetContent $doc
            }
        })
        $control = $window.FindName('Next')
        $control.Add_Click({
          $json = GetContent
          if (-not (Validate $json)) {
                Write-Host 'Unable to validate document. Correct all errors or click "Skip" to skip this document.' -ForegroundColor Red
            }
            else {
                $Script:nextClicked = $true
                $window.Hide()
            }
        })
        $control = $window.FindName('Diff')
        $control.Add_Click({
            $json = GetContent
            if (-not (Validate $json)) {
                Write-Host 'Unable to validate document. Correct all errors and try again.' -ForegroundColor Red
                return
            }
            $modpath = "$PSScriptRoot\..\Grouper.psd1"
            $job = Start-Job -ArgumentList @($modpath, $json) -ScriptBlock {
                Import-Module -Name $args[0]
                $diff = $args[1] | ConvertTo-GrouperDocument | Get-GrouperMemberDiff
                if ($diff.Count -gt 0) {
                    $diff | Format-Table -AutoSize | Out-String | Write-Host
                }
                else {
                    Write-Host 'No members will be added or removed'
                }
            }
            $job | Wait-Job
            $job | Receive-Job
            $job | Remove-Job
        })
        $control = $window.FindName('Skip')
        $control.Add_Click({
            $Script:skipClicked = $true
            $window.Hide()
        })
        $control = $window.FindName('Cancel')
        $control.Add_Click({
            $Script:cancelRequested = $true
            $window.Hide()
        })
        $control = $window.FindName('InsertGuid')
        $control.Add_Click({
            $content = $window.FindName('JsonContent')
            $content.Selection.Text = [Guid]::NewGuid().ToString()
            $content.Selection.Select($content.Selection.End, $content.Selection.End)
        })
        $control = $window.FindName('InsertMemberSource')
        $control.Add_Click({
            $memberSource = $window.FindName('MemberSource');
            if ($null -ne $memberSource.SelectedValue) {
                $source = $memberSources[$memberSource.SelectedValue.Content]
                $content = $window.FindName('JsonContent')
                $content.Selection.Text = $source
                $content.Selection.Select($content.Selection.End, $content.Selection.End)
            }
        })
        $control = $window.FindName('InsertRule')
        $control.Add_Click({
              $content = $window.FindName('JsonContent')
              $content.Selection.Text = $emptyRule
              $content.Selection.Select($content.Selection.End, $content.Selection.End)
        })
        $control = $window.FindName('JsonContent')
        $control.Add_PreviewKeyDown({
            param($sendr, $e)
            if ($e.Key -eq 'Tab') {
                $numSpace = 2 - (($sendr.CaretPosition.GetLineStartPosition(0).GetOffsetToPosition($sendr.Selection.Start) - 1) % 2)
                $sendr.Selection.Text = [string]::new(' ', $numSpace)
                $sendr.Selection.Select($sendr.Selection.End, $sendr.Selection.End)
                $e.Handled = $true
            }
        })
        $window.Add_Closing({
            $Script:cancelRequested = $true
        })

        function GetContent() {
            $control = $window.FindName('JsonContent')
            $range = New-Object -TypeName 'System.Windows.Documents.TextRange' -ArgumentList @($control.Document.ContentStart, $control.Document.ContentEnd)
            $range.Text
        }

        function SetContent($doc) {
            $control = $window.FindName('JsonContent')
            $range = New-Object -TypeName 'System.Windows.Documents.TextRange' -ArgumentList @($control.Document.ContentStart, $control.Document.ContentEnd)
            $range.Text = $doc.ToJson()
        }

        function Validate($json) {
            $result = Test-GrouperDocument -InputObject $json -OutputErrors
            if ($result) {
                for ($i = 0; $i -lt $result.Count; $i++) {
                    Write-Host "$($i + 1). $($result[$i])" -ForegroundColor Yellow
                }
                return $false
            }
            return $true
        }
        $Script:cancelRequested = $false
    }

    process {
        if ($Script:cancelRequested) {
            break
        }
        $document = GetDocumentFromInputObject $InputObject
        if ($null -eq $document) {
            return
        }
        $Script:skipClicked = $false
        $Script:nextClicked = $false
        SetContent $document
        $async = $window.Dispatcher.InvokeAsync({
          $null = $window.ShowDialog()
        })
        $null = $async.Wait()
        if ($Script:nextClicked) {
            ConvertTo-GrouperDocument -InputObject (GetContent)
        }
    }

    end {
        if ($window -and -not $window.Closed) {
            $window.Close()
        }
    }
}

Export-ModuleMember -Function 'Edit-GrouperDocument'
